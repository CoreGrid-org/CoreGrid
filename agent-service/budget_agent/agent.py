from typing import Dict, Any, Optional
import os
import json
from dotenv import load_dotenv

from langchain.chat_models import init_chat_model
from langchain_core.messages import SystemMessage, HumanMessage

from .contracts import (
    FinancialAssessmentRequest,
    FinancialAssessment,
    MaintenanceAnalysis,
)
from .tools import AgentToolsClient

load_dotenv()


SYSTEM_PROMPT = """You are the Budget Analysis Agent in the CoreGrid Asset Lifecycle System (SRS §7.3).

Your responsibility is to perform an objective, evidence-based financial evaluation of physical assets based on:
1. Historical & projected maintenance behaviour (from Maintenance Analysis).
2. Authoritative backend financial records (acquisition cost, depreciation, residual book value, cumulative maintenance spend).
3. Owning department budget constraints.

You must evaluate and rank the 4 possible lifecycle actions:
- REPAIR: Continuing maintenance if projected repair costs are justified and within budget.
- REPLACE: Procuring a replacement if cumulative or projected maintenance exceeds economic value.
- TRANSFER: Reallocating the asset to another department if underutilized or if maintenance can be absorbed elsewhere.
- DISPOSE: Permanent decommissioning/scrapping if the asset is beyond economic repair/service life.

Rules:
1. Treat all provided maintenance metrics and financial numbers strictly as factual context.
2. If department budget data is missing or marked NOT_CONFIGURED, proceed with the financial comparison using available asset financials and note the budget constraint status in the rationale.
3. Compute the repair-to-replace ratio as (projected_annual_cost / max(residual_value, 1.0)) or against replacement cost if available.
4. Rank all 4 options with scores summing or proportional to suitability (0.0 to 1.0) and provide concrete, concise rationales citing the provided figures.
5. Select the highest-scoring option as `proposed_recommendation`.
"""


class BudgetAnalysisAgent:
    """
    Standalone LangGraph-ready node for the Budget Analysis Agent.
    """

    def __init__(
        self,
        tools_client: Optional[AgentToolsClient] = None,
        model_provider: Optional[str] = None,
        model_name: Optional[str] = None,
        api_key: Optional[str] = None,
    ):
        self.tools = tools_client or AgentToolsClient()
        self.provider = model_provider or os.getenv("MODEL_PROVIDER", "google_genai")
        self.model_name = model_name or os.getenv("MODEL_NAME", "gemini-2.0-flash")
        
        # Support Model__ApiKey (from SRS §14.2) as well as provider-specific keys
        self.api_key = (
            api_key
            or os.getenv("Model__ApiKey")
            or os.getenv("GEMINI_API_KEY")
            or os.getenv("GOOGLE_API_KEY")
            or os.getenv("OPENAI_API_KEY")
            or os.getenv("ANTHROPIC_API_KEY")
        )

        # Initialize provider-agnostic chat model with structured output
        # If API key is not configured, we allow lazy initialization during invoke
        self._llm = None

    def _get_llm(self):
        if self._llm is None:
            kwargs = {}
            if self.api_key:
                if self.provider == "google_genai":
                    kwargs["google_api_key"] = self.api_key
                elif self.provider == "openai":
                    kwargs["api_key"] = self.api_key
                elif self.provider == "anthropic":
                    kwargs["api_key"] = self.api_key

            base_llm = init_chat_model(
                model=self.model_name,
                model_provider=self.provider,
                temperature=0.1,
                **kwargs,
            )
            self._llm = base_llm.with_structured_output(FinancialAssessment)
        return self._llm

    async def run(self, request: FinancialAssessmentRequest) -> FinancialAssessment:
        """
        Executes the Budget Analysis Agent node:
        1. Fetch asset financial telemetry from backend
        2. Fetch owning department budget summary
        3. Assemble sanitized context strictly as data (AI-22 / NFR-49)
        4. Invoke structured model reasoning
        5. Return FinancialAssessment
        """
        # 1. Fetch Backend Financials
        financials = await self.tools.get_asset_financials(
            request.asset_id, request.organization_id
        )

        # 2. Fetch Department Budget if departmentId is available
        dept_id = financials.get("department_id") or financials.get("departmentId")
        budget_summary = {}
        if dept_id:
            try:
                from uuid import UUID
                budget_summary = await self.tools.get_department_budget_summary(
                    UUID(str(dept_id)), organization_id=request.organization_id
                )
            except Exception:
                budget_summary = {"status": "UNAVAILABLE", "note": "Could not resolve department ID"}
        else:
            budget_summary = {
                "status": "NOT_CONFIGURED",
                "note": "No department attached or budget tracking not configured in schema",
            }

        # 3. Defensive Data Structuring (AI-22: data not instructions; NFR-49: no personal/user data)
        # We explicitly package only numeric/factual domain items into a structured dictionary
        sanitized_context = {
            "asset_id": str(request.asset_id),
            "asset_code": financials.get("asset_code") or financials.get("assetCode", "N/A"),
            "acquisition_cost": financials.get("acquisition_cost") or financials.get("acquisitionCost", 0.0),
            "accumulated_depreciation": financials.get("accumulated_depreciation") or financials.get("accumulatedDepreciation", 0.0),
            "residual_book_value": financials.get("residual_book_value") or financials.get("residualBookValue", 0.0),
            "cumulative_maintenance_cost": financials.get("cumulative_maintenance_cost") or financials.get("cumulativeMaintenanceCost", 0.0),
            "replacement_estimate": financials.get("replacement_estimate") or financials.get("replacementEstimate"),
            "maintenance_analysis": {
                "repair_count": request.maintenance_analysis.repair_count,
                "cumulative_cost": request.maintenance_analysis.cumulative_cost,
                "mean_time_between_failures_days": request.maintenance_analysis.mean_time_between_failures_days,
                "cost_trend": request.maintenance_analysis.cost_trend,
                "projected_annual_cost": request.maintenance_analysis.projected_annual_cost,
                "data_quality": request.maintenance_analysis.data_quality,
                "confidence": request.maintenance_analysis.confidence,
            },
            "department_budget": budget_summary,
        }

        # Format input strictly within structured delimiters to prevent prompt injection
        user_message_content = (
            "--- BEGIN ASSET FINANCIAL TELEMETRY (FACTUAL DATA ONLY) ---\n"
            f"{json.dumps(sanitized_context, indent=2)}\n"
            "--- END ASSET FINANCIAL TELEMETRY ---\n\n"
            "Analyze the above financial facts and produce the required FinancialAssessment."
        )

        llm = self._get_llm()
        messages = [
            SystemMessage(content=SYSTEM_PROMPT),
            HumanMessage(content=user_message_content),
        ]

        result: FinancialAssessment = await llm.ainvoke(messages)
        return result
