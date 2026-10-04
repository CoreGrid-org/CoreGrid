import { useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { Accordion, AccordionItem, Button, InlineNotification, SkeletonText, Tag } from "@carbon/react";
import { CheckmarkFilled, ErrorFilled, ChevronDown, ChevronUp, Time } from "@carbon/icons-react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { getExecutionSummary } from "../api/workflows";
import type { AgentExecutionStep, AgentWorkflow, WorkflowExecutionSummary } from "../api/workflows";
import { agentDisplay } from "./agentDisplay";
import { ActionMix, ActionTag, BudgetFacts, FleetTable, MaintenanceFacts, PlanSteps, PolicyRules } from "./AgentOutputs";

function formatDuration(ms: number | null) {
  if (ms === null) return null;
  return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(1)} s`;
}

// The structured output behind a step, read from the workflow itself.
// Returns null when there's nothing beyond the one-line summary.
function StepDetail({ agent, workflow }: { agent: string; workflow: AgentWorkflow }) {
  switch (agent) {
    case "Planner":
      return workflow.plan ? <PlanSteps plan={workflow.plan} /> : null;
    case "MaintenanceAnalysis":
      return workflow.maintenance_analysis ? <MaintenanceFacts stats={workflow.maintenance_analysis} /> : null;
    case "BudgetAnalysis":
      return workflow.budget_analysis ? <BudgetFacts assessment={workflow.budget_analysis} /> : null;
    case "PolicyCompliance":
      if (!workflow.validation_result) return null;
      return (
        <>
          <div className="cg-agent-output__headline">
            {workflow.recommendation && <ActionTag action={workflow.recommendation} />}
            <Tag size="sm" type={workflow.validation_result.verdict === "PASS" ? "green" : workflow.validation_result.verdict === "FAIL" ? "red" : "warm-gray"}>
              {workflow.validation_result.verdict}
            </Tag>
            {workflow.validation_result.is_high_impact && <Tag size="sm" type="magenta">High impact</Tag>}
          </div>
          {workflow.fleet && workflow.fleet.asset_count > 1 && <ActionMix fleet={workflow.fleet} />}
          <PolicyRules validation={workflow.validation_result} />
          {workflow.fleet && workflow.fleet.asset_count > 1 && <FleetTable fleet={workflow.fleet} />}
        </>
      );
    default:
      return null;
  }
}

function StepRow({ step, workflow, isLatestForAgent }: { step: AgentExecutionStep; workflow: AgentWorkflow; isLatestForAgent: boolean }) {
  const { label, icon: Icon } = agentDisplay(step.agent);
  const succeeded = step.status === "SUCCESS";
  const detail = succeeded && isLatestForAgent ? <StepDetail agent={step.agent} workflow={workflow} /> : null;
  const [open, setOpen] = useState(false);

  return (
    <li className={`cg-trace-step cg-trace-step--${succeeded ? "success" : "failed"}`}>
      <div className="cg-trace-step__rail" aria-hidden="true">
        <span className="cg-trace-step__node">
          <Icon size={16} />
        </span>
      </div>
      <div className="cg-trace-step__body">
        <div className="cg-trace-step__header">
          <span className="cg-trace-step__agent">
            <span className="cg-trace-step__seq">{step.sequence}</span> {label}
          </span>
          <Tag type={succeeded ? "green" : "red"} size="sm" renderIcon={succeeded ? CheckmarkFilled : ErrorFilled}>
            {succeeded ? "Success" : "Failed"}
          </Tag>
          {formatDuration(step.duration_ms) && (
            <span className="cg-trace-step__duration">
              <Time size={12} /> {formatDuration(step.duration_ms)}
            </span>
          )}
          {!isLatestForAgent && <Tag size="sm" type="cool-gray">Earlier run</Tag>}
        </div>
        <p className={`cg-trace-step__summary${succeeded ? "" : " cg-trace-step__summary--error"}`}>
          {step.error ?? step.output_summary ?? "No summary recorded."}
        </p>
        {detail && (
          <>
            <Button
              kind="ghost"
              size="sm"
              className="cg-trace-step__toggle"
              renderIcon={open ? ChevronUp : ChevronDown}
              onClick={() => setOpen((v) => !v)}
            >
              {open ? "Hide output" : "Show output"}
            </Button>
            {open && <div className="cg-agent-output">{detail}</div>}
          </>
        )}
      </div>
    </li>
  );
}

// SRS §9.6: "Full auditable trace: plan, agent outputs, tool calls,
// validation, decision": a timeline of nodes, each with its own one-line
// summary and, on demand, its structured output. Fetched lazily, only once
// the accordion is opened, so a page of cards doesn't fire a request per card.
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

  // Re-runs leave earlier rows in the trace; only the latest per agent maps to the stored output.
  const latestIdByAgent = new Map<string, string>();
  summary?.steps.forEach((s) => latestIdByAgent.set(s.agent, s.id));
  const totalMs = summary?.steps.reduce((sum, s) => sum + (s.duration_ms ?? 0), 0) ?? 0;

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
            {summary.steps.length > 0 ? (
              <>
                <p className="cg-trace-meta">
                  {summary.steps.length} step{summary.steps.length === 1 ? "" : "s"} · {formatDuration(totalMs)} total
                </p>
                <ol className="cg-trace-steps">
                  {summary.steps.map((step) => (
                    <StepRow
                      key={step.id}
                      step={step}
                      workflow={summary.workflow}
                      isLatestForAgent={latestIdByAgent.get(step.agent) === step.id}
                    />
                  ))}
                </ol>
              </>
            ) : (
              <p className="cg-table__muted cg-text-small">No agent has run for this workflow yet.</p>
            )}

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
