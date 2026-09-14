from datetime import date
from typing import Literal, TypedDict
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class EvaluationObjective(BaseModel):
    model_config = ConfigDict(extra="forbid")

    asset_id: UUID
    objective_text: str = Field(min_length=1, max_length=2000)
    initiated_by: UUID
    organization_id: UUID


class AssetSummary(BaseModel):
    model_config = ConfigDict(extra="ignore", populate_by_name=True)

    asset_id: UUID
    asset_code: str
    name: str
    asset_type: str
    category: str
    status: str
    condition: str
    department: str
    location: str
    acquisition_date: date
    acquisition_cost: float


AgentName = Literal[
    "MaintenanceAnalysis",
    "BudgetAnalysis",
    "PolicyCompliance",
    "DeterministicGate",
]
ExpectedOutput = Literal[
    "MaintenanceAnalysis",
    "FinancialAssessment",
    "PolicyValidation",
    "GateResult",
]


class PlanStep(BaseModel):
    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    seq: int = Field(ge=1, le=6)
    agent: AgentName
    purpose: str = Field(min_length=1, max_length=500)
    expected_output: ExpectedOutput = Field(alias="expectedOutput")


class ExecutionPlan(BaseModel):
    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    in_scope: bool = Field(alias="inScope")
    rejection_reason: str | None = Field(default=None, alias="rejectionReason")
    steps: list[PlanStep] = Field(default_factory=list, max_length=6)


class PlannerState(TypedDict, total=False):
    objective: EvaluationObjective
    asset_summary: AssetSummary
    execution_plan: ExecutionPlan
    error: str
