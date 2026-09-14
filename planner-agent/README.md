# CoreGrid Planner Agent

Standalone Python/LangGraph service for Student 1's Planner Agent.

The service:

- Rejects clearly out-of-scope objectives before calling OpenAI.
- Calls only the allow-listed `get_asset_summary` tool.
- Uses organisation and asset IDs from the request/workflow context.
- Uses OpenAI structured output to produce a typed `ExecutionPlan`.
- Does not connect to PostgreSQL or ThunderID.
- Keeps the OpenAI key in `OPENAI_API_KEY`; never commit `.env`.

## Prerequisites

- Python 3.11+
- The CoreGrid backend running with an internal endpoint:
  `GET /api/agent-tools/assets/{assetId}/summary`
- An OpenAI API key in the environment.

The current repository does not yet expose the `get_asset_summary` backend endpoint. Add that API endpoint using the contract in `doc/setup/Planner-Agent-Implementation.md` before running an in-scope plan.

## Run

```bash
cd planner-agent
python3 -m venv .venv
source .venv/bin/activate
pip install -e ".[test]"
cp .env.example .env
```

Set `OPENAI_API_KEY` in `.env` or export it in the shell. Do not commit the file.

For an in-scope plan, `AGENT_SERVICE_TOKEN` is also required. It must be a valid
CoreGrid bearer token accepted by the protected `/api/agent-tools/*` endpoints.
An OpenAI key alone is not enough: OpenAI creates the plan, while CoreGrid
authorises and supplies the asset data.

```bash
set -a
source .env
set +a
uvicorn app.main:app --host "$PLANNER_AGENT_HOST" --port "$PLANNER_AGENT_PORT"
```

Health check:

```bash
curl http://127.0.0.1:8091/health
```

Plan request:

```bash
curl -X POST http://127.0.0.1:8091/plan \
  -H 'Content-Type: application/json' \
  -H 'X-Planner-Secret: your-internal-secret' \
  -d '{
    "asset_id": "00000000-0000-0000-0000-000000000001",
    "objective_text": "Evaluate whether this asset should be repaired or replaced",
    "initiated_by": "00000000-0000-0000-0000-000000000002",
    "organization_id": "00000000-0000-0000-0000-000000000003"
  }'
```

Use real UUIDs in the request. Values such as `REAL_ASSET_UUID` or `000...001`
are examples only and will fail validation or asset lookup. The required UUIDs
are the asset ID, initiating CoreGrid user ID, and organisation ID.

For an out-of-scope objective, the service returns a rejected typed plan without making an OpenAI request.

## Test

```bash
pytest
```

## Security notes

- `OPENAI_API_KEY` is loaded only from the environment.
- The Planner service is intended for private/internal networking, not public ingress.
- The CoreGrid API must enforce organisation scoping for `get_asset_summary`.
- Do not send ThunderID credentials to OpenAI.
- Do not include prompts, tokens, or chain-of-thought in workflow persistence.
- The current endpoint returns the plan only. Wiring the plan into `AgentWorkflow.Plan` belongs to the API orchestration integration.
