import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Button, InlineNotification, Pagination } from "@carbon/react";
import { Add, Renew, InProgress, Pending, CheckmarkOutline, Misuse } from "@carbon/icons-react";
import type { ComponentType } from "react";
import { formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useClientPagination } from "@/shared/hooks/useClientPagination";
import { usePermissions } from "@/features/auth/hooks/usePermissions";
import { useResumeWorkflow, useWorkflowsList } from "../hooks/useWorkflows";
import CreateWorkflowModal from "../components/CreateWorkflowModal";
import EvaluatePolicyModal from "../components/EvaluatePolicyModal";
import DecideWorkflowModal from "../components/DecideWorkflowModal";
import AgentsOverview from "../components/AgentsOverview";
import WorkflowCard, { KeyFact } from "../components/WorkflowCard";
import { ActionMix, ActionTag, FleetTable, formatLkr, PolicyRules } from "../components/AgentOutputs";
import { workflowTitle } from "../api/workflows";
import type { AgentWorkflow, WorkflowStatus } from "../api/workflows";

const IN_FLIGHT_STATUSES: WorkflowStatus[] = ["PLANNING", "ANALYZING", "VALIDATING"];
const POLL_MS = 4000;

const TAB = { active: 0, awaiting: 1, completed: 2, agents: 3 } as const;

function tabFor(status: WorkflowStatus) {
  if (IN_FLIGHT_STATUSES.includes(status)) return TAB.active;
  return status === "AWAITING_APPROVAL" ? TAB.awaiting : TAB.completed;
}

// One sentence on where a finished evaluation ended up and why.
const OUTCOME_COPY: Partial<Record<WorkflowStatus, string>> = {
  COMPLETED_ADVISORY: "Completed as advisory: policy-compliant and low impact, no approval needed.",
  APPROVED: "Approved by an Administrator.",
  REJECTED: "Rejected by an Administrator.",
  REVISION_REQUESTED: "No policy-permitted action yet, so revision was requested.",
  FAILED_SAFE: "Stopped safely. No business record was changed.",
};

// Clickable count tiles above the tabs; each jumps to the tab that holds them.
function SummaryTile({
  label,
  count,
  hint,
  icon: Icon,
  tone,
  onClick,
}: {
  label: string;
  count: number;
  hint: string;
  icon: ComponentType<{ size?: number; className?: string }>;
  tone: "progress" | "attention" | "done" | "stopped";
  onClick: () => void;
}) {
  return (
    <button type="button" className={`cg-workflow-summary__tile cg-workflow-summary__tile--${tone}`} onClick={onClick}>
      <span className="cg-workflow-summary__icon">
        <Icon size={20} />
      </span>
      <span className="cg-workflow-summary__text">
        <span className="cg-workflow-summary__count">{count}</span>
        <span className="cg-workflow-summary__label">{label}</span>
        <span className="cg-workflow-summary__hint">{hint}</span>
      </span>
    </button>
  );
}

function WorkflowCardPagination({ pagination }: { pagination: ReturnType<typeof useClientPagination<AgentWorkflow>> }) {
  if (pagination.total <= 5) return null;
  return (
    <Pagination
      className="cg-workflow-pagination"
      page={pagination.page}
      pageSize={pagination.pageSize}
      pageSizes={[5, 10, 20, 50]}
      totalItems={pagination.total}
      onChange={({ page: nextPage, pageSize: nextPageSize }) => {
        pagination.setPage(nextPage);
        pagination.setPageSize(nextPageSize);
      }}
    />
  );
}

// The headline every card leads with: the recommended action, and for a fleet
// how the assets split across actions.
function RecommendationSummary({ workflow }: { workflow: AgentWorkflow }) {
  const single = workflow.scope === "ASSET" ? workflow.fleet?.assets[0] : undefined;
  return (
    <div className="cg-workflow-card__recommendation">
      <div className="cg-workflow-card__recommendation-head">
        <span className="cg-workflow-card__section-label">Recommendation</span>
        {workflow.recommendation ? <ActionTag action={workflow.recommendation} /> : <span className="cg-table__muted">N/A</span>}
      </div>
      {single && <p className="cg-workflow-card__reason">{single.reason}</p>}
      {workflow.fleet && workflow.fleet.asset_count > 1 && <ActionMix fleet={workflow.fleet} />}
    </div>
  );
}

function AnalysisFacts({ workflow }: { workflow: AgentWorkflow }) {
  const m = workflow.maintenance_analysis;
  const b = workflow.budget_analysis;
  if (!m && !b) return null;
  return (
    <div className="cg-workflow-card__facts">
      {m && <KeyFact label="Repairs" value={m.repair_count} />}
      {m && <KeyFact label="Cost trend" value={formatStatusLabel(m.cost_trend)} />}
      {m && <KeyFact label="Projected next 12 months" value={formatLkr(m.projected_next_twelve_months_cost)} />}
      {b && <KeyFact label="Residual value" value={formatLkr(b.residual_value)} />}
      {b?.repair_to_replace_ratio != null && <KeyFact label="Repair-to-residual" value={b.repair_to_replace_ratio.toFixed(2)} />}
    </div>
  );
}

export default function WorkflowsPage() {
  const { can } = usePermissions();
  const canInitiate = can("workflow:initiate");
  const canDecide = can("workflow:approve");

  const workflows = useWorkflowsList(undefined, {
    everyMs: POLL_MS,
    while: (items) => items.some((w) => IN_FLIGHT_STATUSES.includes(w.status)),
  });
  const resume = useResumeWorkflow();

  const [selectedTab, setSelectedTab] = useState<number>(TAB.active);
  const [showCreate, setShowCreate] = useState(false);
  const [justCreated, setJustCreated] = useState<AgentWorkflow | null>(null);
  const [evaluating, setEvaluating] = useState<AgentWorkflow | null>(null);
  const [deciding, setDeciding] = useState<{ workflow: AgentWorkflow; decision: "APPROVE" | "REJECT" | "REVISE" } | null>(null);
  const [resumingId, setResumingId] = useState<string | null>(null);

  const all = workflows.data ?? [];
  const active = all.filter((w) => IN_FLIGHT_STATUSES.includes(w.status));
  const awaitingApproval = all.filter((w) => w.status === "AWAITING_APPROVAL");
  const completed = all.filter((w) => tabFor(w.status) === TAB.completed);

  // Every card carries its own (lazily-fetched) execution trace accordion,
  // so a page of cards is naturally taller than a page of table rows — a
  // smaller default page size keeps each tab to a reasonable scroll.
  const activePage = useClientPagination(active, 5);
  const awaitingApprovalPage = useClientPagination(awaitingApproval, 5);
  const completedPage = useClientPagination(completed, 5);

  const handleResume = (id: string) => {
    setResumingId(id);
    resume.mutate(
      { id },
      {
        onSuccess: (updated) => {
          workflows.refetch();
          setSelectedTab(tabFor(updated.status));
        },
      },
    );
  };

  const failedSafe = completed.filter((w) => w.status === "FAILED_SAFE").length;

  const placeholder = (text: string, hint: string, Icon: ComponentType<{ size?: number }>) => (
    <div className="cg-placeholder cg-workflow-empty">
      <Icon size={32} />
      <p className="cg-workflow-empty__title">{workflows.isLoading ? "Loading…" : text}</p>
      {!workflows.isLoading && <p className="cg-workflow-empty__hint">{hint}</p>}
    </div>
  );

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Agentic Workflows</h1>
          <p className="cg-page__subtitle">Review and approve agent-recommended actions.</p>
        </div>
        {canInitiate && (
          <Button renderIcon={Add} onClick={() => setShowCreate(true)}>
            New evaluation
          </Button>
        )}
      </div>

      {justCreated && (
        <InlineNotification
          kind={justCreated.status === "FAILED_SAFE" ? "warning" : "success"}
          title={`Evaluation of ${workflowTitle(justCreated)} finished`}
          subtitle={`${formatStatusLabel(justCreated.status)}${justCreated.recommendation ? ` · recommends ${formatStatusLabel(justCreated.recommendation)}` : ""}.`}
          lowContrast
          onClose={() => setJustCreated(null)}
          className="cg-page-notification"
        />
      )}

      {resume.isError && (
        <InlineNotification
          kind="error"
          title="Could not resume the evaluation"
          subtitle={getErrorMessage(resume.error, "Something went wrong. Please try again.")}
          lowContrast
          onCloseButtonClick={() => setResumingId(null)}
          className="cg-page-notification"
        />
      )}

      {workflows.isError && (
        <InlineNotification
          kind="error"
          title="Could not load workflows"
          subtitle={getErrorMessage(workflows.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          className="cg-page-notification"
        />
      )}

      <div className="cg-workflow-summary">
        <SummaryTile
          label="In progress"
          count={active.length}
          hint="Agents still running"
          icon={InProgress}
          tone="progress"
          onClick={() => setSelectedTab(TAB.active)}
        />
        <SummaryTile
          label="Needs a decision"
          count={awaitingApproval.length}
          hint="High-impact, paused for approval"
          icon={Pending}
          tone="attention"
          onClick={() => setSelectedTab(TAB.awaiting)}
        />
        <SummaryTile
          label="Finished"
          count={completed.length - failedSafe}
          hint="Advisory, approved, rejected or revised"
          icon={CheckmarkOutline}
          tone="done"
          onClick={() => setSelectedTab(TAB.completed)}
        />
        <SummaryTile
          label="Stopped safely"
          count={failedSafe}
          hint="No business record was changed"
          icon={Misuse}
          tone="stopped"
          onClick={() => setSelectedTab(TAB.completed)}
        />
      </div>

      <Tabs selectedIndex={selectedTab} onChange={({ selectedIndex }) => setSelectedTab(selectedIndex)}>
        <TabList aria-label="Workflow sections">
          <Tab>Active{active.length > 0 ? ` (${active.length})` : ""}</Tab>
          <Tab>Awaiting Approval{awaitingApproval.length > 0 ? ` (${awaitingApproval.length})` : ""}</Tab>
          <Tab>Completed{completed.length > 0 ? ` (${completed.length})` : ""}</Tab>
          <Tab>Agents</Tab>
        </TabList>
        <TabPanels>
          {/* ── Active ──────────────────────────────────────────────────── */}
          <TabPanel className="cg-workflow-panel">
            {active.length > 0 ? (
              <>
                <div className="cg-workflow-list">
                  {activePage.pageItems.map((w) => (
                    <WorkflowCard key={w.id} workflow={w}>
                      <div className="cg-workflow-card__facts">
                        <KeyFact
                          label="Plan"
                          value={w.plan?.inScope ? `${w.plan.steps.length} steps` : w.plan?.rejectionReason ?? "Planning…"}
                        />
                        <KeyFact label="Stage" value={formatStatusLabel(w.status)} />
                        <KeyFact label="Started" value={w.started_at ? new Date(w.started_at).toLocaleString() : "—"} />
                      </div>
                      <AnalysisFacts workflow={w} />

                      {canInitiate && (
                        <div className="cg-workflow-card__actions">
                          <Button
                            kind="primary"
                            size="sm"
                            renderIcon={Renew}
                            disabled={resume.isPending && resumingId === w.id}
                            onClick={() => handleResume(w.id)}
                          >
                            {resume.isPending && resumingId === w.id ? "Running agents…" : "Resume evaluation"}
                          </Button>
                          <Button kind="ghost" size="sm" onClick={() => setEvaluating(w)}>
                            Evaluate manually
                          </Button>
                        </div>
                      )}
                    </WorkflowCard>
                  ))}
                </div>
                <WorkflowCardPagination pagination={activePage} />
              </>
            ) : (
              placeholder(
                "No evaluations in progress.",
                canInitiate ? "Start one with “New evaluation” — it usually finishes in a few seconds." : "Evaluations appear here while their agents are running.",
                InProgress,
              )
            )}
          </TabPanel>

          {/* ── Awaiting approval ───────────────────────────────────────── */}
          <TabPanel className="cg-workflow-panel">
            {awaitingApproval.length > 0 ? (
              <>
                <div className="cg-workflow-list">
                  {awaitingApprovalPage.pageItems.map((w) => (
                    <WorkflowCard key={w.id} workflow={w}>
                      <RecommendationSummary workflow={w} />
                      <AnalysisFacts workflow={w} />

                      {w.validation_result && (
                        <div className="cg-workflow-card__section">
                          <p className="cg-workflow-card__section-label">Policy validation: {w.validation_result.verdict}</p>
                          <PolicyRules validation={w.validation_result} />
                        </div>
                      )}

                      {w.fleet && w.fleet.asset_count > 1 && (
                        <div className="cg-workflow-card__section">
                          <p className="cg-workflow-card__section-label">Per-asset results</p>
                          <FleetTable fleet={w.fleet} />
                        </div>
                      )}

                      {canDecide ? (
                        <div className="cg-workflow-card__actions">
                          <Button kind="primary" size="sm" onClick={() => setDeciding({ workflow: w, decision: "APPROVE" })}>
                            Approve
                          </Button>
                          <Button kind="danger--tertiary" size="sm" onClick={() => setDeciding({ workflow: w, decision: "REJECT" })}>
                            Reject
                          </Button>
                          <Button kind="tertiary" size="sm" onClick={() => setDeciding({ workflow: w, decision: "REVISE" })}>
                            Request revision
                          </Button>
                        </div>
                      ) : (
                        <p className="cg-workflow-card__waiting">Awaiting an Administrator's decision.</p>
                      )}
                    </WorkflowCard>
                  ))}
                </div>
                <WorkflowCardPagination pagination={awaitingApprovalPage} />
              </>
            ) : (
              placeholder("Nothing is awaiting approval.", "High-impact recommendations pause here until an Administrator decides.", Pending)
            )}
          </TabPanel>

          {/* ── Completed ───────────────────────────────────────────────── */}
          <TabPanel className="cg-workflow-panel">
            {completed.length > 0 ? (
              <>
                <div className="cg-workflow-list">
                  {completedPage.pageItems.map((w) => (
                    <WorkflowCard key={w.id} workflow={w}>
                      <p className={`cg-workflow-card__outcome cg-workflow-card__outcome--${w.status.toLowerCase()}`}>
                        {OUTCOME_COPY[w.status] ?? formatStatusLabel(w.status)}
                      </p>
                      {w.recommendation && <RecommendationSummary workflow={w} />}
                      <div className="cg-workflow-card__facts">
                        <KeyFact label="Completed" value={w.completed_at ? new Date(w.completed_at).toLocaleString() : "—"} />
                        {w.revision_count > 0 && <KeyFact label="Revisions" value={w.revision_count} />}
                        {w.failure_reason && <KeyFact label="Reason" value={w.failure_reason} />}
                      </div>
                    </WorkflowCard>
                  ))}
                </div>
                <WorkflowCardPagination pagination={completedPage} />
              </>
            ) : (
              placeholder("No completed evaluations yet.", "Finished, rejected and safely-stopped evaluations are kept here for audit.", CheckmarkOutline)
            )}
          </TabPanel>

          {/* ── Agents ──────────────────────────────────────────────────── */}
          <TabPanel className="cg-workflow-panel">
            <AgentsOverview />
          </TabPanel>
        </TabPanels>
      </Tabs>

      {showCreate && (
        <CreateWorkflowModal
          onClose={() => setShowCreate(false)}
          onCreated={(created) => {
            setShowCreate(false);
            setJustCreated(created);
            setSelectedTab(tabFor(created.status));
            workflows.refetch();
          }}
        />
      )}

      {evaluating && (
        <EvaluatePolicyModal
          workflow={evaluating}
          onClose={() => setEvaluating(null)}
          onEvaluated={(updated) => {
            setEvaluating(null);
            setSelectedTab(tabFor(updated.status));
            workflows.refetch();
          }}
        />
      )}

      {deciding && (
        <DecideWorkflowModal
          workflow={deciding.workflow}
          decision={deciding.decision}
          onClose={() => setDeciding(null)}
          onDecided={(updated) => {
            setDeciding(null);
            setSelectedTab(tabFor(updated.status));
            workflows.refetch();
          }}
        />
      )}
    </div>
  );
}
