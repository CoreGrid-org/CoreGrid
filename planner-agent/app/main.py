import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, Header, HTTPException
from openai import RateLimitError

from app.contracts import EvaluationObjective, ExecutionPlan
from app.planner import build_graph


@asynccontextmanager
async def lifespan(_: FastAPI):
    if not os.environ.get("OPENAI_API_KEY"):
        raise RuntimeError("OPENAI_API_KEY is required to start the Planner Agent.")
    yield


app = FastAPI(title="CoreGrid Planner Agent", version="0.1.0", lifespan=lifespan)
graph = build_graph()


def require_shared_secret(value: str | None) -> None:
    expected = os.environ.get("PLANNER_AGENT_SHARED_SECRET")
    if expected and value != expected:
        raise HTTPException(status_code=401, detail="Invalid planner service credentials.")


@app.get("/health")
async def health():
    return {"status": "ok", "agent": "Planner"}


@app.post("/plan", response_model=ExecutionPlan)
async def plan(
    objective: EvaluationObjective,
    x_planner_secret: str | None = Header(default=None),
):
    require_shared_secret(x_planner_secret)
    try:
        result = await graph.ainvoke({"objective": objective})
        return result["execution_plan"]
    except RateLimitError as error:
        print(f"planner_error type=OpenAIQuota message={error}")
        raise HTTPException(
            status_code=503,
            detail="OpenAI quota is exhausted. Add API credits or use an account with available quota, then restart the Planner.",
        ) from error
    except Exception as error:
        print(f"planner_error type={type(error).__name__} message={error}")
        raise HTTPException(status_code=502, detail="Planner Agent could not complete the plan.") from error
