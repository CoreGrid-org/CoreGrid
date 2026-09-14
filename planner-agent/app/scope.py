from app.contracts import ExecutionPlan, PlanStep


_ALLOWED_TERMS = (
    "repair",
    "replace",
    "transfer",
    "dispose",
    "retain",
    "lifecycle",
    "maintenance",
    "condition",
    "evaluate",
    "evaluation",
)
_REJECTED_PHRASES = (
    "create user",
    "delete database",
    "change password",
    "modify policy",
    "change policy",
    "approve disposal",
    "execute disposal",
    "delete asset",
)


def rejection_reason(objective_text: str) -> str | None:
    text = " ".join(objective_text.casefold().split())
    if not text:
        return "An evaluation objective is required."
    if any(phrase in text for phrase in _REJECTED_PHRASES):
        return "The Planner only plans asset lifecycle evaluation; it cannot administer users, policies, databases, or approve actions."
    if not any(term in text for term in _ALLOWED_TERMS):
        return "The objective is outside asset lifecycle evaluation scope."
    return None


def rejected_plan(reason: str) -> ExecutionPlan:
    return ExecutionPlan(inScope=False, rejectionReason=reason, steps=[])


def validate_plan(plan: ExecutionPlan) -> ExecutionPlan:
    if not plan.in_scope:
        if plan.steps:
            raise ValueError("An out-of-scope plan must not contain executable steps.")
        if not plan.rejection_reason:
            raise ValueError("An out-of-scope plan must contain a rejection reason.")
        return plan

    if plan.rejection_reason:
        raise ValueError("An in-scope plan cannot contain a rejection reason.")
    if len(plan.steps) < 3 or len(plan.steps) > 6:
        raise ValueError("An in-scope plan must contain 3 to 6 steps.")
    if [step.seq for step in plan.steps] != list(range(1, len(plan.steps) + 1)):
        raise ValueError("Plan steps must have consecutive sequence numbers.")
    return plan


def fallback_plan() -> ExecutionPlan:
    return ExecutionPlan(
        inScope=True,
        steps=[
            PlanStep(seq=1, agent="MaintenanceAnalysis", purpose="Analyse repair history and projected maintenance cost.", expectedOutput="MaintenanceAnalysis"),
            PlanStep(seq=2, agent="BudgetAnalysis", purpose="Compare repair, replacement, residual value, and budget facts.", expectedOutput="FinancialAssessment"),
            PlanStep(seq=3, agent="PolicyCompliance", purpose="Evaluate the proposed recommendation against organisation policy.", expectedOutput="PolicyValidation"),
            PlanStep(seq=4, agent="DeterministicGate", purpose="Validate schemas, business rules, and authorisation before action.", expectedOutput="GateResult"),
        ],
    )
