import { Tag } from "@carbon/react";
import { Bot, ToolBox, Wallet, RuleLocked, Api, FlowConnection } from "@carbon/icons-react";
import type { ComponentType } from "react";

interface AgentTool {
  name: string;
  description: string;
}

interface AgentSpec {
  node: string;
  name: string;
  icon: ComponentType<{ size?: number; className?: string }>;
  callsModel: boolean;
  responsibility: string;
  input: string;
  output: string;
  tools: AgentTool[];
}

// Defines the agents and their allowed tools.
const AGENTS: AgentSpec[] = [
  {
    node: "Node 1",
    name: "Planner Agent",
    icon: Bot,
    callsModel: true,
    responsibility:
      "Interprets the objective, confirms it's in scope, and produces an ordered, typed plan naming which agent executes each step. Rejects out-of-scope objectives before any analysis runs.",
    input: "EvaluationObjective { assetId, objectiveText, initiatedBy, organizationId }",
    output: "ExecutionPlan { steps[], inScope, rejectionReason? }",
    tools: [
      {
        name: "get_asset_summary",
        description: "Code, name, type, category, status, condition, department, location, acquisition date and cost.",
      },
    ],
  },
  {
    node: "Node 2",
    name: "Maintenance Analysis Agent",
    icon: ToolBox,
    callsModel: false,
    responsibility:
      "Quantifies the asset's maintenance behaviour: how often it fails, what it has cost, whether the trend is worsening, and what the next twelve months are likely to cost.",
    input: "MaintenanceAnalysisRequest { assetId, windowMonths }",
    output: "MaintenanceAnalysis { repairCount, cumulativeCost, meanTimeBetweenFailuresDays, costTrend, projectedAnnualCost, dataQuality, confidence }",
    tools: [
      {
        name: "get_maintenance_history",
        description: "Completed maintenance records with dates, classification, actual cost and resulting condition.",
      },
      {
        name: "compute_failure_statistics",
        description: "Repair count, mean time between failures, cost trend coefficient, projected annual cost.",
      },
    ],
  },
  {
    node: "Node 3",
    name: "Budget Analysis Agent",
    icon: Wallet,
    callsModel: false,
    responsibility:
      "Converts the maintenance picture into a financial comparison: residual value against projected repair cost against replacement cost, within the department's budget reality, and ranks the options.",
    input: "FinancialAssessmentRequest { assetId, maintenanceAnalysis }",
    output: "FinancialAssessment { residualValue, replacementEstimate, repairToReplaceRatio, budgetHeadroom, rankedOptions[], proposedRecommendation }",
    tools: [
      {
        name: "get_asset_financials",
        description: "Acquisition cost, accumulated depreciation, residual value, cumulative maintenance cost, replacement estimate.",
      },
      {
        name: "get_department_budget_summary",
        description: "Allocated maintenance budget for the fiscal year, committed and remaining amounts.",
      },
      {
        name: "compute_depreciation",
        description: "Straight-line residual value as of the current date.",
      },
    ],
  },
  {
    node: "Node 4",
    name: "Policy Compliance Agent",
    icon: RuleLocked,
    callsModel: false,
    responsibility:
      "Establishes whether the proposed recommendation is permitted by the organisation's configured policy and the asset's compliance state. The PASS / FAIL / NEEDS_REVISION verdict is a deterministic rule engine, never the model.",
    input: "PolicyValidationRequest { assetId, proposedRecommendation, financialAssessment }",
    output: "PolicyValidation { verdict, ruleResults[], blockingReasons[], isHighImpact }",
    tools: [
      {
        name: "get_organization_policies",
        description: "Repair-to-replace ratio limit, minimum service life, maximum failure frequency, valuation requirement.",
      },
      {
        name: "get_asset_compliance_state",
        description: "Condemnation status, valuation presence and date, open maintenance and transfer counts, elapsed service life.",
      },
    ],
  },
];

export default function AgentsOverview() {
  return (
    <div className="cg-section">
      <div className="cg-section__header">
        <p className="cg-section__title">The Asset Lifecycle Decision workflow</p>
      </div>
      <div className="cg-section__body">
        <p className="cg-agent-intro">
          One evaluation runs through four specialised agents in a fixed order, each with its own disjoint,
          read-only tool allow-list. Every tool call is scoped to the initiating organisation, validated
          against a JSON schema, and recorded with its outcome — an agent can only read through its listed
          tools; it can never write, update or delete a business record.
        </p>

        <div className="cg-agent-grid">
          {AGENTS.map((agent) => (
            <div className="cg-agent-card" key={agent.name}>
              <div className="cg-agent-card__header">
                <div className="cg-agent-card__identity">
                  <agent.icon size={24} className="cg-agent-card__icon" />
                  <div>
                    <p className="cg-agent-card__node">{agent.node}</p>
                    <p className="cg-agent-card__name">{agent.name}</p>
                  </div>
                </div>
                <Tag type={agent.callsModel ? "purple" : "gray"} size="sm">
                  {agent.callsModel ? "Calls a model" : "Deterministic"}
                </Tag>
              </div>

              <p className="cg-agent-card__responsibility">{agent.responsibility}</p>

              <div className="cg-agent-card__contract">
                <FlowConnection size={14} className="cg-agent-card__contract-icon" />
                <span>
                  <code>{agent.input}</code>
                  <span className="cg-agent-card__contract-arrow"> → </span>
                  <code>{agent.output}</code>
                </span>
              </div>

              <p className="cg-agent-card__tools-label">
                Allow-listed tools ({agent.tools.length})
              </p>
              <ul className="cg-agent-card__tools">
                {agent.tools.map((tool) => (
                  <li className="cg-agent-card__tool" key={tool.name}>
                    <Api size={16} className="cg-agent-card__tool-icon" />
                    <div>
                      <p className="cg-agent-card__tool-name">{tool.name}</p>
                      <p className="cg-agent-card__tool-desc">{tool.description}</p>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <p className="cg-agent-footnote">
          A fifth step — the Orchestrator (<code>AgentWorkflowService</code>) — sequences these four agents,
          enforces per-tool timeouts and retries, and runs the deterministic gate before any high-impact
          action can proceed. It holds only its own control-plane tools (persist state, checkpoint/resume,
          enforce timeout, run the gate, request approval) and never calls a business tool directly.
        </p>
      </div>
    </div>
  );
}
