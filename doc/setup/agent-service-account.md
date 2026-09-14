# Agent Service Account & Machine-to-Machine (M2M) Authentication

This document details the configuration required in **ThunderID** and **CoreGrid API** for the **Budget Analysis Agent** (and subsequent AI agents) to securely communicate with the backend's `/api/agent-tools/*` endpoints via OAuth2 `client_credentials`.

---

## 1. Overview & Architecture

Per **SRS §4.6, §7.4, and SEC-ID-10**:
- This M2M setup applies to agents that run as standalone external processes — today that's only the Budget Analysis Agent (Python/LangGraph, `agent-service/`). Team direction as of 2026-09-14 is to build the remaining agent nodes .NET-native inside the API (matching the Policy Compliance Agent, which already runs in-process and doesn't need this setup at all — it calls its tool endpoints directly, no token request required).
- Agents act as advisory and read-only services.
- Agents authenticate as an **"Agent Service Principal"** using the standard OAuth2 `client_credentials` grant against ThunderID.
- The issued JWT token is presented as a `Bearer` token to the CoreGrid backend.
- The CoreGrid backend validates token signature, issuer, and expiration via JWKS.
- `RoleEnrichmentMiddleware` recognizes service principal tokens (where `sub == client_id` or `gty == client-credentials`) and permits them without requiring a human record in the `Users` table.

---

## 2. One-Time Manual ThunderID Console Setup

These steps must be performed in the ThunderID Admin Console (`https://localhost:8090/console`):

### Step 2.1 — Create the Agent Service Application
1. Navigate to **Applications** → **New Application** → **Backend Service**.
2. **Name**: `CoreGrid Budget Agent`.
3. **Grant Type**: `client_credentials`.
4. **Token Endpoint Auth Method**: `client_secret_post` (save explicitly).
5. **Note the credentials**:
   - **Client ID**: e.g., `coregrid-agent-service`
   - **Client Secret**: (generate & store securely in the agent's own `.env` / key vault — only relevant for a standalone external agent; a .NET-native in-process agent has no separate `.env` to manage)

### Step 2.2 — Assign Resource Server & Scopes
1. **Resource Server**: Re-use the default `System` resource server (`https://localhost:8090/mcp`) or a custom CoreGrid API resource server identifier.
2. **Role / Permissions (SRS §4.6)**:
   - `tool:read-asset-history`
   - `tool:read-budget-summary`
   - `tool:read-policy-set`

---

## 3. Token Request — Standalone External Agent Only

A standalone external agent process requests an M2M access token before invoking tool endpoints. The existing Budget Analysis Agent does this in Python:

```python
import httpx

async def get_agent_token(issuer: str, client_id: str, client_secret: str, resource: str) -> str:
    async with httpx.AsyncClient(verify=False) as client:
        response = await client.post(
            f"{issuer}/oauth2/token",
            data={
                "grant_type": "client_credentials",
                "client_id": client_id,
                "client_secret": client_secret,
                "scope": "agent:tools",
                "resource": resource
            },
            headers={"Content-Type": "application/x-www-form-urlencoded"}
        )
        response.raise_for_status()
        return response.json()["access_token"]
```

Then includes the token in HTTP headers:
```python
headers = {
    "Authorization": f"Bearer {access_token}"
}
```

A future standalone .NET agent would do the equivalent with `HttpClient`:

```csharp
var response = await httpClient.PostAsync($"{issuer}/oauth2/token", new FormUrlEncodedContent(new Dictionary<string, string>
{
    ["grant_type"] = "client_credentials",
    ["client_id"] = clientId,
    ["client_secret"] = clientSecret,
    ["scope"] = "agent:tools",
    ["resource"] = resource
}));
response.EnsureSuccessStatusCode();
var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
```

**None of this section applies to a .NET-native in-process agent** (the direction for Planner and Maintenance Analysis) — it runs inside the same API process as `AgentToolsController` and calls its tool methods directly, with no token request, no separate deployment, and no `.env` of its own.

---

## 4. Environment Variables Required for a Standalone External Agent

Only needed for an agent running as its own process (today: the Budget Analysis Agent). Add to that agent's `.env` file:
```dotenv
COREGRID_API_URL=http://localhost:5000
THUNDERID_ISSUER=https://localhost:8090
THUNDERID_RESOURCE=https://localhost:8090/mcp
THUNDERID_AGENT_CLIENT_ID=coregrid-agent-service
THUNDERID_AGENT_CLIENT_SECRET=<secret_from_thunderid_console>
```
