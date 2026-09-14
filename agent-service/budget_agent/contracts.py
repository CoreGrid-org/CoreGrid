from typing import List, Optional, Literal
from uuid import UUID
from datetime import date
from pydantic import BaseModel, Field


class MaintenanceAnalysis(BaseModel):
    """
    Contract from Maintenance Analysis Agent (Node 2, SRS §7.3).
    """
    repair_count: int = Field(description="Total count of historical repair interventions")
    cumulative_cost: float = Field(description="Sum of historical maintenance actual costs")
    mean_time_between_failures_days: Optional[float] = Field(
        default=None, description="Average days between corrective failures"
    )
    cost_trend: str = Field(
        default="STABLE",
        description="Cost progression classification: ESCALATING | STABLE | DECREASING",
    )
    projected_annual_cost: float = Field(
        description="Estimated maintenance expenditure required over the next 12 months"
    )
    data_quality: str = Field(
        default="ADEQUATE",
        description="Quality of historical telemetry: ADEQUATE | INSUFFICIENT",
    )
    confidence: float = Field(
        ge=0.0,
        le=1.0,
        description="Confidence score for the maintenance projection (0.0 to 1.0)",
    )


class FinancialAssessmentRequest(BaseModel):
    """
    Input contract for Budget Analysis Agent (Node 3, SRS §7.3).
    """
    asset_id: UUID = Field(description="Identifier of the asset under evaluation")
    organization_id: Optional[UUID] = Field(
        default=None,
        description="Tenant organization ID for multi-tenant / agent isolation (AI-05)",
    )
    maintenance_analysis: MaintenanceAnalysis = Field(
        description="Findings produced by the upstream Maintenance Analysis Agent"
    )


class RankedOption(BaseModel):
    """
    Single candidate action evaluated by the Budget Analysis Agent.
    """
    action: Literal["REPAIR", "REPLACE", "TRANSFER", "DISPOSE"] = Field(
        description="Lifecycle action evaluated"
    )
    score: float = Field(
        ge=0.0,
        le=1.0,
        description="Relative financial feasibility/suitability score (0.0 to 1.0)",
    )
    rationale: str = Field(
        description="Evidence-based reasoning comparing cost, residual value, and budget impact"
    )


class FinancialAssessment(BaseModel):
    """
    Output contract for Budget Analysis Agent (Node 3, SRS §7.3).
    """
    residual_value: float = Field(
        description="Current estimated/depreciated residual book value of the asset"
    )
    replacement_estimate: Optional[float] = Field(
        default=None,
        description="Estimated replacement procurement cost, if available/applicable",
    )
    repair_to_replace_ratio: Optional[float] = Field(
        default=None,
        description="Ratio of projected 12-month repair cost to replacement cost (or residual value)",
    )
    budget_headroom: Optional[float] = Field(
        default=None,
        description="Available remaining maintenance budget in the owning department",
    )
    ranked_options: List[RankedOption] = Field(
        description="Ranked list of lifecycle options with financial scores and rationale"
    )
    proposed_recommendation: Literal["REPAIR", "REPLACE", "TRANSFER", "DISPOSE"] = Field(
        description="Primary recommended lifecycle action passing to Policy Compliance Agent"
    )
