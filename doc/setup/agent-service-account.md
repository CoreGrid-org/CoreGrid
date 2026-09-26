# Agent Service Account & Machine-to-Machine (M2M) Authentication

This document details the configuration required in **ThunderID** and **CoreGrid API** for an agent node still running as a **standalone external process** to securely communicate with the backend's `/api/agent-tools/*` endpoints via OAuth2 `client_credentials`. Under the target architecture (SRS §7.2.1, ADR-010) every node eventually runs in-process and needs none of this — it applies only until a given node is migrated.

---

## 1. Overview & Architecture

Per **SRS §4.6, §7.4, and SEC-ID-10**:
- This M2M setup applies to agents that run as standalone external processes — today that's only the Planner Agent (Python/LangGraph, `planner-agent/`), pending its own migration in-process. The Budget Analysis Agent's prior standalone implementation was removed 2026-09-15 once the target design (SRS §7.2.1, ADR-010) made it redundant before it was ever wired in; its replacement will be built in-process from the start. Maintenance Analysis and Policy Compliance run in-process and need none of this setup at all — they call their tool services directly, no token request required.
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

---

## 5. In-Process Agent LLM Configuration (.NET Core)

With the migration of agents into the ASP.NET Core process (PlannerAgentService and BudgetAgentService), external M2M tokens and standalone Python runtimes are no longer needed for these nodes. Their LLM outbound endpoints are configured via standard .NET `IConfiguration` (via `appsettings.json`, `appsettings.Development.json`, User Secrets, or environment variables).

All in-process agents that call an LLM share one **`Llm`** section. The team standard is **Google Gemini 3.5 Flash** through Gemini's OpenAI-compatible endpoint:

| Key | Env var | Default | Notes |
|---|---|---|---|
| `Llm:Endpoint` | `Llm__Endpoint` | `https://generativelanguage.googleapis.com/v1beta/openai/chat/completions` | Any OpenAI-compatible chat-completions URL |
| `Llm:Model` | `Llm__Model` | `gemini-3.5-flash` | |
| `Llm:ApiKey` | `Llm__ApiKey` | *(none)* | A Google AI Studio (Gemini) API key. **Never put it in appsettings**; use user-secrets or an env var |

Set the key once for local development:

```bash
cd backend
dotnet user-secrets set "Llm:ApiKey" "<your Gemini API key>"
```

An agent's own section overrides any single shared value, e.g. `Budget:Model` to try a different model for the Budget agent only. Blank values count as unset, so an empty placeholder never hides a real key.

Without a key, each agent logs a warning and uses its deterministic fallback (`PlannerScopeGuard.FallbackPlan()`, `BudgetScopeGuard.FallbackAssessment()`), so workflows still complete. Older key names (`Planner:OpenAiApiKey`, `Budget:ApiKey`) are still read for backwards compatibility, but prefer `Llm:ApiKey`.

Implementation: `backend/Features/Agents/LlmSettings.cs`; both agents use the shared `"Llm"` named `HttpClient` (60 s timeout).
