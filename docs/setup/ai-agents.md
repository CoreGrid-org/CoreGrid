# Agent Service Account & Machine-to-Machine (M2M) Authentication

This document is retained as historical guidance for any future standalone agent process. The current CoreGrid
implementation runs the Planner, Maintenance Analysis, Budget Analysis, and Policy Compliance nodes in-process
inside the API, so a normal current deployment does not need this ThunderID M2M setup. Under the target
architecture (SRS §7.2.1, ADR-010), `/api/agent-tools/*` is compatibility surface for an explicitly external
agent only.

---

## 1. Overview & Architecture

Per **SRS §4.6, §7.4, and SEC-ID-10**:
- This M2M setup applies only to a future or legacy standalone external agent. The current Planner and Budget
  implementations are in-process services under `backend/Features/Agents/`; Maintenance Analysis and Policy
  Compliance are also in-process and need no token request or external-agent registration.
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

With the migration of agents into the ASP.NET Core process (PlannerAgentService and BudgetAgentService), external M2M tokens and standalone Python runtimes are no longer needed for these nodes. Their LLM outbound endpoints are configured via standard .NET `IConfiguration`: `backend/.env` locally, or environment variables in Docker and the cloud. `appsettings*.json` hold no values here.

All in-process agents that call an LLM share one **`Llm`** section. The team standard is **Google Gemini 3.5 Flash** through Gemini's OpenAI-compatible endpoint:

| Key | Env var | Default | Notes |
|---|---|---|---|
| `Llm:Endpoint` | `Llm__Endpoint` | `https://generativelanguage.googleapis.com/v1beta/openai/chat/completions` | Any OpenAI-compatible chat-completions URL |
| `Llm:Model` | `Llm__Model` | `gemini-3.5-flash` | |
| `Llm:ApiKey` | `Llm__ApiKey` | *(none)* | A Google AI Studio (Gemini) API key. Set it in `backend/.env` or as an environment variable, never in a committed file |

Set the key once for local development, in `backend/.env`:

```dotenv
Llm__ApiKey=<your Gemini API key>
```

**Optional fallback provider.** If the primary call fails (HTTP error, `429` rate limit, timeout, or output that doesn't validate), each agent retries once against a second OpenAI-compatible provider before using its deterministic fallback. Groq hosts `gpt-oss-120b` and is the default:

| Key | Env var | Default | Notes |
|---|---|---|---|
| `LlmFallback:Endpoint` | `LlmFallback__Endpoint` | `https://api.groq.com/openai/v1/chat/completions` | Any OpenAI-compatible chat-completions URL |
| `LlmFallback:Model` | `LlmFallback__Model` | `openai/gpt-oss-120b` | |
| `LlmFallback:ApiKey` | `LlmFallback__ApiKey` | *(none, so disabled)* | A Groq API key (console.groq.com) |

The order is: primary, then fallback, then deterministic result. A provider without a key is skipped. Logs name the model that produced each plan or assessment.

An agent's own section overrides any single shared value, e.g. `Budget:Model` to try a different model for the Budget agent only. Blank values count as unset, so an empty placeholder never hides a real key.

Without any key, each agent logs a warning and uses its deterministic fallback (`PlannerScopeGuard.FallbackPlan()`, `BudgetScopeGuard.FallbackAssessment()`), so workflows still complete. Older key names (`Planner:OpenAiApiKey`, `Budget:ApiKey`) are still read for backwards compatibility, but prefer `Llm:ApiKey`.

Implementation: `backend/Features/Agents/LlmSettings.cs` (`Chain`) and `Services/LlmChat.cs`; both agents use the shared `"Llm"` named `HttpClient` (60 s timeout per call).
