import os

from langgraph.graph import END, START, StateGraph
from openai import AsyncOpenAI

from app.contracts import ExecutionPlan, PlannerState
from app.scope import rejected_plan, rejection_reason, validate_plan
from app.tools import get_asset_summary


_SYSTEM_PROMPT = """You are CoreGrid's Planner Agent. Create only an ordered execution plan for an asset lifecycle evaluation.
The available downstream steps are MaintenanceAnalysis, BudgetAnalysis, PolicyCompliance, and DeterministicGate.
Never invent asset facts; use the supplied asset summary. Never approve, execute, or mutate a business action.
Return only the typed ExecutionPlan structure. Use inScope=false and no steps if the objective is outside scope."""


async def planner_node(state: PlannerState) -> PlannerState:
    objective = state["objective"]
    reason = rejection_reason(objective.objective_text)
    if reason:
        return {**state, "execution_plan": rejected_plan(reason)}

    summary = await get_asset_summary(objective.asset_id, objective.organization_id)
    client = AsyncOpenAI(api_key=os.environ["OPENAI_API_KEY"])
    model = os.environ.get("OPENAI_MODEL", "gpt-4o-mini")
    completion = await client.chat.completions.parse(
        model=model,
        temperature=0,
        messages=[
            {"role": "system", "content": _SYSTEM_PROMPT},
            {
                "role": "user",
                "content": (
                    f"Objective: {objective.objective_text}\n"
                    f"Asset summary: {summary.model_dump_json()}\n"
                    "Produce the typed plan."
                ),
            },
        ],
        response_format=ExecutionPlan,
    )
    parsed = completion.choices[0].message.parsed
    if parsed is None:
        raise ValueError("OpenAI returned no structured execution plan.")
    plan = validate_plan(parsed)
    return {**state, "asset_summary": summary, "execution_plan": plan}


def build_graph():
    graph = StateGraph(PlannerState)
    graph.add_node("planner", planner_node)
    graph.add_edge(START, "planner")
    graph.add_edge("planner", END)
    return graph.compile()
