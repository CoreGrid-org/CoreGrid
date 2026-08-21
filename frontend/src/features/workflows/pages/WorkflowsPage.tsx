import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, InlineNotification } from "@carbon/react";
import { Add, CheckmarkFilled, WarningAltFilled, CloseFilled } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useMe } from "@/features/auth/hooks/useMe";
import { useWorkflowsList } from "../hooks/useWorkflows";
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

  const [showCreate, setShowCreate] = useState(false);
  const [evaluating, setEvaluating] = useState<AgentWorkflow | null>(null);
  const [deciding, setDeciding] = useState<{ workflow: AgentWorkflow; decision: "APPROVE" | "REJECT" | "REVISE" } | null>(null);

  const active = workflows.data?.filter((w) => IN_FLIGHT_STATUSES.includes(w.status)) ?? [];
  const awaitingApproval = workflows.data?.filter((w) => w.status === "AWAITING_APPROVAL") ?? [];
  const completed = workflows.data?.filter((w) => !IN_FLIGHT_STATUSES.includes(w.status) && w.status !== "AWAITING_APPROVAL") ?? [];

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Agentic Workflows</h1>
          <p className="cg-page__subtitle">
            Review and approve agent-recommended actions (FR-067 to FR-076; §7 of the SRS).
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
        title="The Planner, Maintenance Analysis and Budget Analysis agents aren't built yet"
        subtitle="Policy Compliance — this workflow's deterministic gate and the human-approval checkpoint — is real. Use “Evaluate policy compliance” on an active workflow to supply the recommendation those three agents would otherwise produce, and run it through the gate yourself."
        style={{ marginBottom: "1rem", maxWidth: "100%" }}
      />

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
                        <td>
                          <Tag type={statusTagColor(w.status)}>{formatStatusLabel(w.status)}</Tag>
                        </td>
                        <td className="cg-table__muted">{w.started_at ? new Date(w.started_at).toLocaleString() : "—"}</td>
                        <td>
                          {canInitiate && (
                            <Button kind="ghost" size="sm" onClick={() => setEvaluating(w)}>
                              Evaluate policy compliance
                            </Button>
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
                      {w.asset_code} — recommends {formatStatusLabel(w.recommendation ?? "")}
                    </p>
                    <p className="cg-table__muted" style={{ margin: "0.25rem 0 0", fontSize: "0.8125rem" }}>
                      {w.objective}
                    </p>
                  </div>
                  {w.is_high_impact && <Tag type="magenta">High impact</Tag>}
                </div>
                <div className="cg-section__body">
                  <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.5rem" }}>
                    Policy validation — {w.validation_result?.verdict}
                  </p>
                  <div style={{ display: "flex", flexDirection: "column", gap: "0.5rem", marginBottom: "1.5rem" }}>
                    {w.validation_result?.rule_results.map((r) => {
                      const Icon = OUTCOME_ICON[r.outcome];
                      return (
                        <div key={r.rule_id} style={{ display: "flex", alignItems: "center", gap: "0.625rem" }}>
                          {Icon && <Icon size={16} style={{ fill: OUTCOME_COLOR[r.outcome], flexShrink: 0 }} />}
                          <span style={{ fontSize: "0.8125rem", color: "#525252", fontWeight: 600, minWidth: "3.5rem" }}>{r.rule_id}</span>
                          <span style={{ fontSize: "0.8125rem", color: "#525252" }}>
                            {r.expected} — {r.actual}
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
