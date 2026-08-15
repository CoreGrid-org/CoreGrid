import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button } from "@carbon/react";
import { CheckmarkFilled } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { MOCK_ACTIVE_WORKFLOWS, MOCK_AWAITING_APPROVAL, MOCK_COMPLETED_WORKFLOWS } from "../data/mockWorkflows";

export default function WorkflowsPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Agentic Workflows</h1>
          <p className="cg-page__subtitle">
            Review and approve agent-recommended actions (FR-067 to FR-076; §7 of the SRS).
          </p>
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Workflow sections">
          <Tab>Active</Tab>
          <Tab>Awaiting Approval</Tab>
          <Tab>Completed</Tab>
        </TabList>
        <TabPanels>
          {/* ── Active ──────────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-067", "FR-069", "FR-070"]}>
              Planner → Maintenance Analysis → Budget Analysis → Policy Compliance is a fixed pipeline (§7.2);
              this tab tracks which node each running workflow is currently on, live.
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Asset</th>
                    <th>Objective</th>
                    <th>Status</th>
                    <th>Current step</th>
                    <th>Started</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_ACTIVE_WORKFLOWS.map((w, i) => (
                    <tr key={i}>
                      <td className="cg-table__mono">{w.assetCode}</td>
                      <td className="cg-table__muted">{w.objective}</td>
                      <td>
                        <Tag type={statusTagColor(w.status)}>{formatStatusLabel(w.status)}</Tag>
                      </td>
                      <td>{w.currentStep}</td>
                      <td className="cg-table__muted">{w.startedAt}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Awaiting approval ───────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-071", "FR-072", "AI-13", "AI-15", "AI-16"]}>
              A high-impact recommendation interrupts the workflow before any consequence and waits here for
              an Administrator. AI-15 requires the objective, plan, findings, recommendation and the full
              rule-by-rule validation result to be shown before the decision controls — that is what the card
              below reproduces.
            </MockNotice>

            {MOCK_AWAITING_APPROVAL.map((w, i) => (
              <div className="cg-section" key={i}>
                <div className="cg-section__header">
                  <div>
                    <p className="cg-section__title">
                      {w.assetCode} — recommends {formatStatusLabel(w.recommendation)}
                    </p>
                    <p className="cg-table__muted" style={{ margin: "0.25rem 0 0", fontSize: "0.8125rem" }}>
                      {w.objective}
                    </p>
                  </div>
                  {w.isHighImpact && <Tag type="magenta">High impact</Tag>}
                </div>
                <div className="cg-section__body">
                  <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.5rem" }}>
                    Supporting factors
                  </p>
                  <ul style={{ margin: "0 0 1.25rem", paddingLeft: "1.25rem", fontSize: "0.875rem", color: "#161616" }}>
                    {w.supportingFactors.map((f, j) => (
                      <li key={j} style={{ marginBottom: "0.25rem" }}>
                        {f}
                      </li>
                    ))}
                  </ul>

                  <p style={{ fontSize: "0.75rem", fontWeight: 600, letterSpacing: "0.05em", textTransform: "uppercase", color: "#8d8d8d", margin: "0 0 0.5rem" }}>
                    Policy validation
                  </p>
                  <div style={{ display: "flex", flexDirection: "column", gap: "0.5rem", marginBottom: "1.5rem" }}>
                    {w.ruleResults.map((r) => (
                      <div key={r.ruleId} style={{ display: "flex", alignItems: "center", gap: "0.625rem" }}>
                        <CheckmarkFilled size={16} style={{ fill: "#24a148", flexShrink: 0 }} />
                        <span style={{ fontSize: "0.8125rem", color: "#525252", fontWeight: 600 }}>{r.ruleId}</span>
                        <Tag type="green" size="sm">
                          {r.outcome}
                        </Tag>
                      </div>
                    ))}
                  </div>

                  <div className="cg-form__actions">
                    <Button kind="primary">Approve</Button>
                    <Button kind="danger--tertiary">Reject</Button>
                    <Button kind="tertiary">Request revision</Button>
                  </div>
                </div>
              </div>
            ))}
          </TabPanel>

          {/* ── Completed ───────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-073", "FR-074", "FR-075", "FR-076"]}>
              On approval the API executes the recommended action through the ordinary business service; on
              rejection nothing changes; a revision re-enters analysis (capped at two cycles, AI-20).
            </MockNotice>

            <div className="cg-section">
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
                  {MOCK_COMPLETED_WORKFLOWS.map((w, i) => (
                    <tr key={i}>
                      <td className="cg-table__mono">{w.assetCode}</td>
                      <td>{formatStatusLabel(w.recommendation)}</td>
                      <td>
                        <Tag type={statusTagColor(w.outcome)}>{formatStatusLabel(w.outcome)}</Tag>
                      </td>
                      <td className="cg-table__muted">{w.completedAt}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
