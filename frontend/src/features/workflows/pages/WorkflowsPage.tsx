import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, InlineNotification, Pagination } from "@carbon/react";
import { Add, CheckmarkFilled, WarningAltFilled, CloseFilled } from "@carbon/icons-react";
import { formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useClientPagination } from "@/shared/hooks/useClientPagination";
import { useMe } from "@/features/auth/hooks/useMe";
import { useRunMaintenanceAgent, useRunPolicyAgent, useWorkflowsList } from "../hooks/useWorkflows";
import CreateWorkflowModal from "../components/CreateWorkflowModal";
import EvaluatePolicyModal from "../components/EvaluatePolicyModal";
import DecideWorkflowModal from "../components/DecideWorkflowModal";
import AgentsOverview from "../components/AgentsOverview";
import WorkflowCard, { KeyFact } from "../components/WorkflowCard";
import type { AgentWorkflow } from "../api/workflows";

const IN_FLIGHT_STATUSES = ["PLANNING", "ANALYZING", "VALIDATING"];
const OUTCOME_ICON: Record<string, typeof CheckmarkFilled> = {
  PASS: CheckmarkFilled,
  FAIL: CloseFilled,
  NEEDS_REVISION: WarningAltFilled,
};
const OUTCOME_COLOR: Record<string, string> = { PASS: "#24a148", FAIL: "#da1e28", NEEDS_REVISION: "#f1c21b" };

function WorkflowCardPagination({ pagination }: { pagination: ReturnType<typeof useClientPagination<AgentWorkflow>> }) {
  if (pagination.total === 0) return null;
  return (
    <Pagination
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

export default function WorkflowsPage() {
  const { data: me } = useMe();
  const canInitiate = me?.role === "InventoryOfficer" || me?.role === "Administrator";
  const canDecide = me?.role === "Administrator";

  const workflows = useWorkflowsList();
  const runAgent = useRunPolicyAgent();
  const runMaintenanceAgent = useRunMaintenanceAgent();

  const [showCreate, setShowCreate] = useState(false);
  const [evaluating, setEvaluating] = useState<AgentWorkflow | null>(null);
  const [deciding, setDeciding] = useState<{ workflow: AgentWorkflow; decision: "APPROVE" | "REJECT" | "REVISE" } | null>(null);
  const [runningId, setRunningId] = useState<string | null>(null);
  const [runningMaintenanceId, setRunningMaintenanceId] = useState<string | null>(null);

  const handleRunAgent = (id: string) => {
    setRunningId(id);
    runAgent.mutate({ id }, { onSuccess: () => workflows.refetch() });
  };

  const handleRunMaintenanceAgent = (id: string) => {
    setRunningMaintenanceId(id);
    runMaintenanceAgent.mutate({ id }, { onSuccess: () => workflows.refetch() });
  };

  const active = workflows.data?.filter((w) => IN_FLIGHT_STATUSES.includes(w.status)) ?? [];
  const awaitingApproval = workflows.data?.filter((w) => w.status === "AWAITING_APPROVAL") ?? [];
  const completed = workflows.data?.filter((w) => !IN_FLIGHT_STATUSES.includes(w.status) && w.status !== "AWAITING_APPROVAL") ?? [];

  // Every card carries its own (lazily-fetched) execution trace accordion,
  // so a page of cards is naturally taller than a page of table rows — a
  // smaller default page size keeps each tab to a reasonable scroll.
  const activePage = useClientPagination(active, 5);
  const awaitingApprovalPage = useClientPagination(awaitingApproval, 5);
  const completedPage = useClientPagination(completed, 5);

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Agentic Workflows</h1>
          <p className="cg-page__subtitle">
            Review and approve agent-recommended actions.
          </p>
        </div>
        {canInitiate && (
          <Button renderIcon={Add} onClick={() => setShowCreate(true)}>
            New evaluation
          </Button>
        )}
      </div>

      <InlineNotification
        kind="info"
        lowContrast
        hideCloseButton
        title="Planner and Maintenance Analysis Agents are connected"
        subtitle="New evaluations now call the Planner Agent, persist its typed execution plan, move to analysis, and automatically run the Maintenance Analysis Agent (repair count, MTBF, cost trend, 12-month projection) for the asset. The Budget agent still needs to be connected; Policy Compliance remains available through the existing action."
        style={{ marginBottom: "1rem", maxWidth: "100%" }}
      />

      {runAgent.isError && (
        <InlineNotification
          kind="error"
          title="Could not run the Policy Compliance Agent"
          subtitle={getErrorMessage(runAgent.error, "Something went wrong. Please try again.")}
          lowContrast
          onCloseButtonClick={() => setRunningId(null)}
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {runMaintenanceAgent.isError && (
        <InlineNotification
          kind="error"
          title="Could not run the Maintenance Analysis Agent"
          subtitle={getErrorMessage(runMaintenanceAgent.error, "Something went wrong. Please try again.")}
          lowContrast
          onCloseButtonClick={() => setRunningMaintenanceId(null)}
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {workflows.isError && (
        <InlineNotification
          kind="error"
          title="Could not load workflows"
          subtitle={getErrorMessage(workflows.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <Tabs>
        <TabList aria-label="Workflow sections">
          <Tab>Active</Tab>
          <Tab>Awaiting Approval</Tab>
          <Tab>Completed</Tab>
          <Tab>Agents</Tab>
        </TabList>
        <TabPanels>
          {/* ── Active ──────────────────────────────────────────────────── */}
          <TabPanel>
            {workflows.isLoading ? (
              <div className="cg-placeholder">
                <p>Loading…</p>
              </div>
            ) : active.length > 0 ? (
              <>
                <div className="cg-workflow-list">
                  {activePage.pageItems.map((w) => (
                    <WorkflowCard key={w.id} workflow={w}>
                      <div className="cg-workflow-card__facts">
                        <KeyFact
                          label="Plan"
                          value={w.plan?.inScope ? `${w.plan.steps.length} steps` : w.plan?.rejectionReason ?? "—"}
                        />
                        <KeyFact
                          label="Maintenance analysis"
                          value={
                            w.maintenance_analysis
                              ? `${w.maintenance_analysis.repair_count} repair${w.maintenance_analysis.repair_count === 1 ? "" : "s"} · ${formatStatusLabel(w.maintenance_analysis.cost_trend)}`
                              : "—"
                          }
                        />
                        <KeyFact label="Started" value={w.started_at ? new Date(w.started_at).toLocaleString() : "—"} />
                      </div>

                      {canInitiate && (
                        <div className="cg-workflow-card__actions">
                          <Button
                            kind="tertiary"
                            size="sm"
                            disabled={runAgent.isPending && runningId === w.id}
                            onClick={() => handleRunAgent(w.id)}
                          >
                            {runAgent.isPending && runningId === w.id ? "Running…" : "Run Policy Compliance Agent"}
                          </Button>
                          <Button
                            kind="tertiary"
                            size="sm"
                            disabled={runMaintenanceAgent.isPending && runningMaintenanceId === w.id}
                            onClick={() => handleRunMaintenanceAgent(w.id)}
                          >
                            {runMaintenanceAgent.isPending && runningMaintenanceId === w.id
                              ? "Running…"
                              : "Re-run Maintenance Analysis"}
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
              <div className="cg-placeholder">
                <p>No evaluations in progress.</p>
              </div>
            )}
          </TabPanel>

          {/* ── Awaiting approval ───────────────────────────────────────── */}
          <TabPanel>
            {workflows.isLoading ? (
              <div className="cg-placeholder">
                <p>Loading…</p>
              </div>
            ) : awaitingApproval.length > 0 ? (
              <>
                <div className="cg-workflow-list">
                  {awaitingApprovalPage.pageItems.map((w) => (
                    <WorkflowCard key={w.id} workflow={w}>
                      <KeyFact label="Recommendation" value={formatStatusLabel(w.recommendation ?? "—")} />

                      {w.maintenance_analysis && (
                        <div className="cg-workflow-card__facts">
                          <KeyFact label="Repair count" value={w.maintenance_analysis.repair_count} />
                          <KeyFact
                            label="Mean time between failures"
                            value={
                              w.maintenance_analysis.mean_time_between_failures_days !== null
                                ? `${w.maintenance_analysis.mean_time_between_failures_days} days`
                                : "—"
                            }
                          />
                          <KeyFact label="Cost trend" value={formatStatusLabel(w.maintenance_analysis.cost_trend)} />
                          <KeyFact
                            label="Projected next 12 months"
                            value={`LKR ${w.maintenance_analysis.projected_next_twelve_months_cost.toLocaleString()}`}
                          />
                        </div>
                      )}

                      <p className="cg-workflow-card__section-label">
                        Policy validation: {w.validation_result?.verdict}
                      </p>
                      <div className="cg-workflow-card__rules">
                        {w.validation_result?.rule_results.map((r) => {
                          const Icon = OUTCOME_ICON[r.outcome];
                          return (
                            <div key={r.rule_id} className="cg-workflow-card__rule">
                              {Icon && <Icon size={16} style={{ fill: OUTCOME_COLOR[r.outcome], flexShrink: 0 }} />}
                              <span className="cg-workflow-card__rule-id">{r.rule_id}</span>
                              <span className="cg-workflow-card__rule-values">
                                {r.expected} → {r.actual}
                              </span>
                              <Tag type={r.outcome === "PASS" ? "green" : r.outcome === "FAIL" ? "red" : "gray"} size="sm">
                                {r.outcome}
                              </Tag>
                            </div>
                          );
                        })}
                      </div>

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
                        <p className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
                          Awaiting an Administrator's decision.
                        </p>
                      )}
                    </WorkflowCard>
                  ))}
                </div>
                <WorkflowCardPagination pagination={awaitingApprovalPage} />
              </>
            ) : (
              <div className="cg-placeholder">
                <p>Nothing is awaiting approval.</p>
              </div>
            )}
          </TabPanel>

          {/* ── Completed ───────────────────────────────────────────────── */}
          <TabPanel>
            {workflows.isLoading ? (
              <div className="cg-placeholder">
                <p>Loading…</p>
              </div>
            ) : completed.length > 0 ? (
              <>
                <div className="cg-workflow-list">
                  {completedPage.pageItems.map((w) => (
                    <WorkflowCard key={w.id} workflow={w}>
                      <div className="cg-workflow-card__facts">
                        <KeyFact label="Recommendation" value={w.recommendation ? formatStatusLabel(w.recommendation) : "—"} />
                        <KeyFact label="Completed" value={w.completed_at ? new Date(w.completed_at).toLocaleString() : "—"} />
                        {w.failure_reason && <KeyFact label="Failure reason" value={w.failure_reason} />}
                      </div>
                    </WorkflowCard>
                  ))}
                </div>
                <WorkflowCardPagination pagination={completedPage} />
              </>
            ) : (
              <div className="cg-placeholder">
                <p>No completed evaluations yet.</p>
              </div>
            )}
          </TabPanel>

          {/* ── Agents ──────────────────────────────────────────────────── */}
          <TabPanel>
            <AgentsOverview />
          </TabPanel>
        </TabPanels>
      </Tabs>

      {showCreate && (
        <CreateWorkflowModal
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            setShowCreate(false);
            workflows.refetch();
          }}
        />
      )}

      {evaluating && (
        <EvaluatePolicyModal
          workflow={evaluating}
          onClose={() => setEvaluating(null)}
          onEvaluated={() => {
            setEvaluating(null);
            workflows.refetch();
          }}
        />
      )}

      {deciding && (
        <DecideWorkflowModal
          workflow={deciding.workflow}
          decision={deciding.decision}
          onClose={() => setDeciding(null)}
          onDecided={() => {
            setDeciding(null);
            workflows.refetch();
          }}
        />
      )}
    </div>
  );
}
