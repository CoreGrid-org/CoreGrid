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
    input: "EvaluationScope { assetType, asset? } + objective",
    output: "ExecutionPlan { steps[], inScope, rejectionReason? }",
    tools: [
      {
        name: "get_asset_type_summary",
        description: "Asset type, category, useful life, active asset count and condition mix of the fleet.",
      },
    ],
  },
  {
    node: "Node 2",
    name: "Maintenance Analysis Agent",
    icon: ToolBox,
    callsModel: false,
    responsibility:
      "Quantifies each asset's maintenance behaviour: how often it fails, whether repair costs are rising, and what the next twelve months are likely to cost. A fleet is rolled up into one figure.",
    input: "EvaluationScope { assetType, asset? }",
    output: "FailureStatistics { repairCount, meanTimeBetweenFailuresDays, costTrend, projectedNextTwelveMonthsCost }",
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
    callsModel: true,
    responsibility:
      "Triages each asset by comparing projected repair cost with residual value against the policy threshold, then ranks the lifecycle options. Every figure is deterministic; a model, when configured, only re-scores the options and falls back to the deterministic ranking.",
    input: "EvaluationScope + per-asset FailureStatistics",
    output: "FinancialAssessment { residualValue, repairToReplaceRatio, budgetHeadroom, rankedOptions[], proposedRecommendation, source }",
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
      "Tries candidate actions for each asset in order and keeps the first one the organisation's policy permits. The PASS / FAIL / NEEDS_REVISION verdict comes from a deterministic rule engine, never the model.",
    input: "EvaluationScope + maintenance and financial assessments",
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
          One evaluation runs through four specialised agents in a fixed order, each with its own read-only
          tool interface. Every tool call is scoped to the initiating organisation taken from the persisted
          workflow — an agent can only read through its own tools; it can never write, update or delete a
          business record.
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
          The Orchestrator (<code>AgentWorkflowService</code> with <code>WorkflowPipeline</code>) runs these
          four agents in plan order, records a trace step for each, and finishes with the deterministic gate:
          DISPOSE or low-confidence recommendations pause for Administrator approval, other policy-compliant
          ones complete as advisory, and a policy FAIL stops safely. It never calls a business tool itself.
        </p>
      </div>
    </div>
  );
}
