import { useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { Accordion, AccordionItem, InlineNotification, SkeletonText, Tag } from "@carbon/react";
import { Bot, ToolBox, Wallet, RuleLocked, FlowConnection, CheckmarkFilled, ErrorFilled } from "@carbon/icons-react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { getExecutionSummary } from "../api/workflows";
import type { AgentExecutionStep, WorkflowExecutionSummary } from "../api/workflows";

// SRS §9.6/§7.8: the same agent → icon/display-name mapping AgentsOverview
// uses, so a step in this trace and that tab's reference card always read
// as the same agent. Falls back to a generic icon/its raw identifier for
// anything not in the four-agent allow-list (DeterministicGate, and
// PolicyComplianceRecommendation — the node-4 recommendation sub-step).
const AGENT_DISPLAY: Record<string, { label: string; icon: typeof Bot }> = {
  Planner: { label: "Planner Agent", icon: Bot },
  MaintenanceAnalysis: { label: "Maintenance Analysis Agent", icon: ToolBox },
  BudgetAnalysis: { label: "Budget Analysis Agent", icon: Wallet },
  PolicyCompliance: { label: "Policy Compliance Agent", icon: RuleLocked },
  PolicyComplianceRecommendation: { label: "Policy Compliance Agent — recommendation", icon: RuleLocked },
  DeterministicGate: { label: "Deterministic Gate", icon: FlowConnection },
};

function agentDisplay(agent: string) {
  return AGENT_DISPLAY[agent] ?? { label: agent, icon: FlowConnection };
}

function formatDuration(ms: number | null) {
  if (ms === null) return null;
  return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(1)} s`;
}

function StepRow({ step }: { step: AgentExecutionStep }) {
  const { label, icon: Icon } = agentDisplay(step.agent);
  const succeeded = step.status === "SUCCESS";

  return (
    <div className="cg-trace-step">
      <Icon size={20} className="cg-trace-step__icon" />
      <div className="cg-trace-step__body">
        <div className="cg-trace-step__header">
          <span className="cg-trace-step__agent">
            <span className="cg-trace-step__seq">{step.sequence}.</span> {label}
          </span>
          <Tag type={succeeded ? "green" : "red"} size="sm" renderIcon={succeeded ? CheckmarkFilled : ErrorFilled}>
            {succeeded ? "Success" : "Failed"}
          </Tag>
          {formatDuration(step.duration_ms) && (
            <span className="cg-trace-step__duration">{formatDuration(step.duration_ms)}</span>
          )}
        </div>
        <p className="cg-trace-step__summary">{step.error ?? step.output_summary ?? "No summary recorded."}</p>
      </div>
    </div>
  );
}

// SRS §9.6: "Full auditable trace: plan, agent outputs, tool calls,
// validation, decision" — rendered as a readable step-by-step list (an
// icon, the agent's name, a pass/fail tag, its own summary sentence),
// never as a raw JSON dump of the underlying AgentExecutionStep rows.
// Fetched lazily, only once the accordion is actually opened, since a
// page of workflow cards would otherwise fire one extra request per card
// whether or not anyone looks at the trace.
export default function ExecutionTrace({ workflowId }: { workflowId: string }) {
  const { getAccessToken } = useThunderID();
  const [summary, setSummary] = useState<WorkflowExecutionSummary | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<unknown>(undefined);
  const [hasOpened, setHasOpened] = useState(false);

  useEffect(() => {
    if (!hasOpened || summary || isLoading) return;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getExecutionSummary(workflowId, token))
      .then(setSummary)
      .catch(setError)
      .finally(() => setIsLoading(false));
  }, [hasOpened, summary, isLoading, workflowId, getAccessToken]);

  return (
    <Accordion align="start" className="cg-trace-accordion">
      <AccordionItem title="Agent execution trace" onHeadingClick={({ isOpen }) => isOpen && setHasOpened(true)}>
        {isLoading && <SkeletonText paragraph lineCount={3} />}

        {error !== undefined && (
          <InlineNotification
            kind="error"
            lowContrast
            hideCloseButton
            title="Could not load the execution trace"
            subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
          />
        )}

        {summary && (
          <>
            <div className="cg-trace-steps">
              {summary.steps.length > 0 ? (
                summary.steps.map((step) => <StepRow key={step.id} step={step} />)
              ) : (
                <p className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
                  No agent has run for this workflow yet.
                </p>
              )}
            </div>

            {summary.approvals.length > 0 && (
              <div className="cg-trace-approvals">
                <p className="cg-trace-approvals__label">Decisions</p>
                {summary.approvals.map((approval) => (
                  <div key={approval.id} className="cg-trace-approval">
                    <Tag
                      type={approval.decision === "APPROVE" ? "green" : approval.decision === "REJECT" ? "red" : "gray"}
                      size="sm"
                    >
                      {approval.decision}
                    </Tag>
                    <span className="cg-trace-approval__by">
                      {approval.decided_by_email ?? "Unknown"} · {new Date(approval.decided_at).toLocaleString()}
                    </span>
                    <p className="cg-trace-approval__reason">{approval.reason}</p>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </AccordionItem>
    </Accordion>
  );
}
