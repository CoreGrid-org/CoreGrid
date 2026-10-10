import { Bot, ToolBox, Wallet, RuleLocked, FlowConnection } from "@carbon/icons-react";

// Maps agent identifiers to their display names and icons.
const AGENT_DISPLAY: Record<string, { label: string; icon: typeof Bot }> = {
  Planner: { label: "Planner Agent", icon: Bot },
  MaintenanceAnalysis: { label: "Maintenance Analysis Agent", icon: ToolBox },
  BudgetAnalysis: { label: "Budget Analysis Agent", icon: Wallet },
  PolicyCompliance: { label: "Policy Compliance Agent", icon: RuleLocked },
  PolicyComplianceRecommendation: { label: "Policy Compliance Agent (recommendation)", icon: RuleLocked },
  DeterministicGate: { label: "Deterministic Gate", icon: FlowConnection },
};

export function agentDisplay(agent: string) {
  return AGENT_DISPLAY[agent] ?? { label: agent, icon: FlowConnection };
}
