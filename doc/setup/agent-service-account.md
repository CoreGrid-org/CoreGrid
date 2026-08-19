# Agent Service Account & Machine-to-Machine (M2M) Authentication

This document details the configuration required in **ThunderID** and **CoreGrid API** for the **Budget Analysis Agent** (and subsequent AI agents) to securely communicate with the backend's `/api/agent-tools/*` endpoints via OAuth2 `client_credentials`.

---

## 1. Overview & Architecture

Per **SRS §4.6, §7.4, and SEC-ID-10**:
- Agents are external autonomous processes (Python/LangGraph).
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
   - **Client Secret**: (generate & store securely in Python agent `.env` / key vault)

### Step 2.2 — Assign Resource Server & Scopes
1. **Resource Server**: Re-use the default `System` resource server (`https://localhost:8090/mcp`) or a custom CoreGrid API resource server identifier.
2. **Role / Permissions (SRS §4.6)**:
   - `tool:read-asset-history`
   - `tool:read-budget-summary`
   - `tool:read-policy-set`

---

## 3. Python Agent Service Usage (Token Request)

The Python service requests an M2M access token before invoking tool endpoints:

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

---

## 4. Environment Variables Required for Python Agent

Add to the Python Agent `.env` file:
```dotenv
COREGRID_API_URL=http://localhost:5000
THUNDERID_ISSUER=https://localhost:8090
THUNDERID_RESOURCE=https://localhost:8090/mcp
THUNDERID_AGENT_CLIENT_ID=coregrid-agent-service
THUNDERID_AGENT_CLIENT_SECRET=<secret_from_thunderid_console>
```
