import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, InlineNotification } from "@carbon/react";
import { Add, CheckmarkFilled, WarningAltFilled, CloseFilled } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useMe } from "@/features/auth/hooks/useMe";
import { useRunMaintenanceAgent, useRunPolicyAgent, useWorkflowsList } from "../hooks/useWorkflows";
import CreateWorkflowModal from "../components/CreateWorkflowModal";
import EvaluatePolicyModal from "../components/EvaluatePolicyModal";
import DecideWorkflowModal from "../components/DecideWorkflowModal";
import type { AgentWorkflow } from "../api/workflows";

const IN_FLIGHT_STATUSES = ["PLANNING", "ANALYZING", "VALIDATING"];
const OUTCOME_ICON: Record<string, typeof CheckmarkFilled> = {
  PASS: CheckmarkFilled,
  FAIL: CloseFilled,
  NEEDS_REVISION: WarningAltFilled,
};
const OUTCOME_COLOR: Record<string, string> = { PASS: "#24a148", FAIL: "#da1e28", NEEDS_REVISION: "#f1c21b" };

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
        </TabList>
        <TabPanels>
          {/* ── Active ──────────────────────────────────────────────────── */}
          <TabPanel>
            <div className="cg-section">
              {workflows.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading…</p>
                </div>
              ) : active.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Asset</th>
                      <th>Objective</th>
                      <th>Plan</th>
                      <th>Maintenance analysis</th>
                      <th>Status</th>
                      <th>Started</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {active.map((w) => (
                      <tr key={w.id}>
                        <td className="cg-table__mono">{w.asset_code}</td>
                        <td className="cg-table__muted">{w.objective}</td>
                        <td className="cg-table__muted">
                          {w.plan?.inScope ? `${w.plan.steps.length} steps` : w.plan?.rejectionReason ?? "—"}
                        </td>
                        <td className="cg-table__muted">
                          {w.maintenance_analysis ? (
                            <>
                              {w.maintenance_analysis.repair_count} repair
                              {w.maintenance_analysis.repair_count === 1 ? "" : "s"}
                              {" · "}
                              {formatStatusLabel(w.maintenance_analysis.cost_trend)}
                            </>
                          ) : (
                            "—"
                          )}
                        </td>
                        <td>
                          <Tag type={statusTagColor(w.status)}>{formatStatusLabel(w.status)}</Tag>
                        </td>
                        <td className="cg-table__muted">{w.started_at ? new Date(w.started_at).toLocaleString() : "—"}</td>
                        <td>
                          {canInitiate && (
                            <div style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end", flexWrap: "wrap" }}>
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
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No evaluations in progress.</p>
                </div>
              )}
            </div>
          </TabPanel>

          {/* ── Awaiting approval ───────────────────────────────────────── */}
          <TabPanel>
            {!workflows.isLoading && awaitingApproval.length === 0 && (
              <div className="cg-placeholder">
                <p>Nothing is awaiting approval.</p>
              </div>
            )}

            {awaitingApproval.map((w) => (
              <div className="cg-section" key={w.id}>
                <div className="cg-section__header">
                  <div>
                    <p className="cg-section__title">
                      {w.asset_code}: recommends {formatStatusLabel(w.recommendation ?? "")}
                    </p>
                    <p className="cg-table__muted" style={{ margin: "0.25rem 0 0", fontSize: "0.8125rem" }}>
                      {w.objective}
                    </p>
                  </div>
                  {w.is_high_impact && <Tag type="magenta">High impact</Tag>}
                </div>
                <div className="cg-section__body">
                  {w.maintenance_analysis && (
                    <div style={{ display: "flex", gap: "1.5rem", flexWrap: "wrap", marginBottom: "1rem" }}>
                      <div>
                        <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.25rem" }}>
                          Repair count
                        </p>
                        <p style={{ margin: 0, fontSize: "0.875rem" }}>{w.maintenance_analysis.repair_count}</p>
                      </div>
                      <div>
                        <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.25rem" }}>
                          Mean time between failures
                        </p>
                        <p style={{ margin: 0, fontSize: "0.875rem" }}>
                          {w.maintenance_analysis.mean_time_between_failures_days !== null
                            ? `${w.maintenance_analysis.mean_time_between_failures_days} days`
                            : "—"}
                        </p>
                      </div>
                      <div>
                        <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.25rem" }}>
                          Cost trend
                        </p>
                        <p style={{ margin: 0, fontSize: "0.875rem" }}>{formatStatusLabel(w.maintenance_analysis.cost_trend)}</p>
                      </div>
                      <div>
                        <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.25rem" }}>
                          Projected next 12 months
                        </p>
                        <p style={{ margin: 0, fontSize: "0.875rem" }}>
                          LKR {w.maintenance_analysis.projected_next_twelve_months_cost.toLocaleString()}
                        </p>
                      </div>
                    </div>
                  )}
                  <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.5rem" }}>
                    Policy validation: {w.validation_result?.verdict}
                  </p>
                  <div style={{ display: "flex", flexDirection: "column", gap: "0.5rem", marginBottom: "1.5rem" }}>
                    {w.validation_result?.rule_results.map((r) => {
                      const Icon = OUTCOME_ICON[r.outcome];
                      return (
                        <div key={r.rule_id} style={{ display: "flex", alignItems: "center", gap: "0.625rem" }}>
                          {Icon && <Icon size={16} style={{ fill: OUTCOME_COLOR[r.outcome], flexShrink: 0 }} />}
                          <span style={{ fontSize: "0.8125rem", color: "#525252", fontWeight: 600, minWidth: "3.5rem" }}>{r.rule_id}</span>
                          <span style={{ fontSize: "0.8125rem", color: "#525252" }}>
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
                    <div className="cg-form__actions">
                      <Button kind="primary" onClick={() => setDeciding({ workflow: w, decision: "APPROVE" })}>
                        Approve
                      </Button>
                      <Button kind="danger--tertiary" onClick={() => setDeciding({ workflow: w, decision: "REJECT" })}>
                        Reject
                      </Button>
                      <Button kind="tertiary" onClick={() => setDeciding({ workflow: w, decision: "REVISE" })}>
                        Request revision
                      </Button>
                    </div>
                  ) : (
                    <p className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
                      Awaiting an Administrator's decision.
                    </p>
                  )}
                </div>
              </div>
            ))}
          </TabPanel>

          {/* ── Completed ───────────────────────────────────────────────── */}
          <TabPanel>
            <div className="cg-section">
              {workflows.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading…</p>
                </div>
              ) : completed.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Asset</th>
                      <th>Recommendation</th>
                      <th>Outcome</th>
                      <th>Completed</th>
                    </tr>
                  </thead>
                  <tbody>
                    {completed.map((w) => (
                      <tr key={w.id}>
                        <td className="cg-table__mono">{w.asset_code}</td>
                        <td>{w.recommendation ? formatStatusLabel(w.recommendation) : "—"}</td>
                        <td>
                          <Tag type={statusTagColor(w.status)}>{formatStatusLabel(w.status)}</Tag>
                        </td>
                        <td className="cg-table__muted">{w.completed_at ? new Date(w.completed_at).toLocaleString() : "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No completed evaluations yet.</p>
                </div>
              )}
            </div>
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
