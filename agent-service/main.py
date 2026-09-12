import asyncio
import sys
import os
import json
from uuid import UUID
from dotenv import load_dotenv

from budget_agent.contracts import (
    FinancialAssessmentRequest,
    MaintenanceAnalysis,
)
from budget_agent.agent import BudgetAnalysisAgent

load_dotenv()


async def main():
    """
    Manual testing CLI runner for the Budget Analysis Agent.
    Usage: python main.py [optional_asset_uuid]
    """
    asset_id_str = sys.argv[1] if len(sys.argv) > 1 else "01900000-0000-7000-8000-000000000001"
    try:
        asset_id = UUID(asset_id_str)
    except ValueError:
        print(f"Invalid UUID provided: {asset_id_str}")
        sys.exit(1)

    print(f"=== CoreGrid Budget Analysis Agent Standalone Test ===")
    print(f"Target Asset ID: {asset_id}")

    # Sample input from upstream Maintenance Analysis Agent (Node 2)
    sample_maintenance_analysis = MaintenanceAnalysis(
        repair_count=4,
        cumulative_cost=3200.00,
        mean_time_between_failures_days=45.0,
        cost_trend="ESCALATING",
        projected_annual_cost=2800.00,
        data_quality="ADEQUATE",
        confidence=0.88,
    )

    request = FinancialAssessmentRequest(
        asset_id=asset_id,
        maintenance_analysis=sample_maintenance_analysis,
    )

    print("\n[1] Initializing BudgetAnalysisAgent...")
    agent = BudgetAnalysisAgent()

    print("[2] Executing Agent (fetching tool data & running model reasoning)...")
    try:
        assessment = await agent.run(request)
        print("\n=== Financial Assessment Result ===")
        print(json.dumps(assessment.model_dump(), indent=2))
    except Exception as ex:
        print(f"\nExecution encountered an issue: {ex}")
        print("Note: To run with live LLM calls, ensure Model__ApiKey (or GEMINI_API_KEY) is set in your environment.")


if __name__ == "__main__":
    asyncio.run(main())
