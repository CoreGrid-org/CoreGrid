import pytest
from uuid import UUID, uuid4
from unittest.mock import AsyncMock, patch

from budget_agent.contracts import (
    FinancialAssessmentRequest,
    FinancialAssessment,
    MaintenanceAnalysis,
    RankedOption,
)
from budget_agent.tools import AgentToolsClient
from budget_agent.agent import BudgetAnalysisAgent


@pytest.fixture
def sample_maintenance_analysis():
    return MaintenanceAnalysis(
        repair_count=3,
        cumulative_cost=1500.00,
        mean_time_between_failures_days=60.0,
        cost_trend="ESCALATING",
        projected_annual_cost=2000.00,
        data_quality="ADEQUATE",
        confidence=0.85,
    )


@pytest.fixture
def sample_request(sample_maintenance_analysis):
    return FinancialAssessmentRequest(
        asset_id=uuid4(),
        maintenance_analysis=sample_maintenance_analysis,
    )


@pytest.mark.asyncio
async def test_tools_client_graceful_not_configured_budget():
    """
    Verifies that tools client handles NOT_CONFIGURED budget responses without error (AI-10).
    """
    client = AgentToolsClient(base_url="http://mock-api")
    
    with patch("httpx.AsyncClient.get") as mock_get:
        mock_response = AsyncMock()
        mock_response.status_code = 200
        mock_response.json = lambda: {
            "department_id": str(uuid4()),
            "status": "NOT_CONFIGURED",
            "note": "Department budget tracking tables do not exist in the database schema.",
            "allocated_budget": None,
            "remaining_amount": None,
        }
        mock_response.raise_for_status = lambda: None
        mock_get.return_value = mock_response
        
        # Mock token acquisition
        client.get_access_token = AsyncMock(return_value="mock_token")

        result = await client.get_department_budget_summary(uuid4())
        assert result["status"] == "NOT_CONFIGURED"
        assert result["allocated_budget"] is None


@pytest.mark.asyncio
async def test_budget_agent_structured_execution_with_mocked_llm(sample_request):
    """
    Verifies the complete BudgetAnalysisAgent flow:
    1. Calls backend tools
    2. Packages data defensively (AI-22 / NFR-49)
    3. Invokes model and parses structured FinancialAssessment response
    """
    mock_tools = AsyncMock(spec=AgentToolsClient)
    mock_tools.get_asset_financials.return_value = {
        "asset_id": str(sample_request.asset_id),
        "asset_code": "AST-SRV-01",
        "acquisition_cost": 10000.0,
        "accumulated_depreciation": 6000.0,
        "residual_book_value": 4000.0,
        "cumulative_maintenance_cost": 1500.0,
        "replacement_estimate": None,
    }
    mock_tools.get_department_budget_summary.return_value = {
        "status": "NOT_CONFIGURED",
        "note": "Schema gap",
    }

    agent = BudgetAnalysisAgent(tools_client=mock_tools)

    expected_assessment = FinancialAssessment(
        residual_value=4000.0,
        replacement_estimate=12000.0,
        repair_to_replace_ratio=0.5,
        budget_headroom=None,
        ranked_options=[
            RankedOption(
                action="REPAIR",
                score=0.75,
                rationale="Projected annual repair cost of $2,000 is manageable relative to $4,000 residual value.",
            ),
            RankedOption(
                action="REPLACE",
                score=0.60,
                rationale="Replacement is feasible but high upfront procurement cost.",
            ),
            RankedOption(
                action="TRANSFER",
                score=0.30,
                rationale="Asset is needed in current department.",
            ),
            RankedOption(
                action="DISPOSE",
                score=0.10,
                rationale="Asset still holds $4,000 residual book value with 2+ years useful life remaining.",
            ),
        ],
        proposed_recommendation="REPAIR",
    )

    # Mock the LLM structured output call
    mock_llm = AsyncMock()
    mock_llm.ainvoke.return_value = expected_assessment
    agent._llm = mock_llm

    result = await agent.run(sample_request)

    assert result.proposed_recommendation == "REPAIR"
    assert result.residual_value == 4000.0
    assert len(result.ranked_options) == 4
    assert result.ranked_options[0].action == "REPAIR"
    assert result.ranked_options[0].score == 0.75
    
    # Assert tools were called
    mock_tools.get_asset_financials.assert_called_once_with(sample_request.asset_id, None)
