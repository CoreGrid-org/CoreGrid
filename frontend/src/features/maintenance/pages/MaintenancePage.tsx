import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button } from "@carbon/react";
import { Add, NotificationNew } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { MOCK_MAINTENANCE_RECORDS, MOCK_PREVENTIVE_SCHEDULE, MOCK_NOTIFICATIONS } from "../data/mockMaintenance";

export default function MaintenancePage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Maintenance</h1>
          <p className="cg-page__subtitle">Faults, repairs and preventive schedules (FR-033 to FR-042).</p>
        </div>
        <Button renderIcon={Add}>New maintenance record</Button>
      </div>

      <Tabs>
        <TabList aria-label="Maintenance sections">
          <Tab>Maintenance Records</Tab>
          <Tab>Preventive Schedule</Tab>
          <Tab>Notifications</Tab>
        </TabList>
        <TabPanels>
          {/* ── Records ─────────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-035", "FR-037", "FR-038", "FR-042"]}>
              The real tab lists records via GET /api/maintenance with status/priority/assignee/date filters;
              the assigned officer progresses each record REQUESTED → APPROVED → IN_PROGRESS → COMPLETED, and
              Complete records actual cost, work performed and resulting condition in one transaction (the
              business-specific operation for this component).
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Asset</th>
                    <th>Type</th>
                    <th>Priority</th>
                    <th>Status</th>
                    <th>Assigned to</th>
                    <th>Estimated cost</th>
                    <th>Actual cost</th>
                    <th>Requested</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_MAINTENANCE_RECORDS.map((rec, i) => (
                    <tr key={i}>
                      <td>
                        <span className="cg-table__mono">{rec.assetCode}</span>
                        <br />
                        <span className="cg-table__muted">{rec.assetName}</span>
                      </td>
                      <td className="cg-table__muted">{rec.type === "CORRECTIVE" ? "Corrective" : "Preventive"}</td>
                      <td>
                        <Tag type={statusTagColor(rec.priority)}>{formatStatusLabel(rec.priority)}</Tag>
                      </td>
                      <td>
                        <Tag type={statusTagColor(rec.status)}>{formatStatusLabel(rec.status)}</Tag>
                      </td>
                      <td className="cg-table__muted">{rec.assignedTo ?? "Unassigned"}</td>
                      <td className="cg-table__muted">{rec.estimatedCost ? `$${rec.estimatedCost.toLocaleString()}` : "—"}</td>
                      <td className="cg-table__muted">{rec.actualCost ? `$${rec.actualCost.toLocaleString()}` : "—"}</td>
                      <td className="cg-table__muted">{rec.requestedAt}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Preventive schedule ─────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-041"]}>
              A scheduled job creates a preventive maintenance record automatically once an asset type's
              configured maintenance interval has elapsed since the last completed maintenance on that asset.
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Asset type</th>
                    <th>Interval</th>
                    <th>Last completed</th>
                    <th>Next due</th>
                    <th>Days until due</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_PREVENTIVE_SCHEDULE.map((s) => (
                    <tr key={s.assetType}>
                      <td>{s.assetType}</td>
                      <td className="cg-table__muted">{s.intervalDays} days</td>
                      <td className="cg-table__muted">{s.lastCompleted}</td>
                      <td className="cg-table__muted">{s.nextDue}</td>
                      <td>
                        <Tag type={s.daysUntilDue <= 7 ? "magenta" : "gray"}>{s.daysUntilDue} days</Tag>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Notifications ───────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-077", "FR-078", "FR-079", "FR-080"]}>
              Real notifications are queued server-side on maintenance assignment, transfer/disposal/workflow
              approval requirements and approval decisions; dispatch never blocks or rolls back the business
              operation that triggered it.
            </MockNotice>

            <div className="cg-section">
              {MOCK_NOTIFICATIONS.map((n, i) => (
                <div
                  key={i}
                  style={{
                    display: "flex",
                    gap: "0.75rem",
                    alignItems: "flex-start",
                    padding: "0.875rem 1.5rem",
                    borderBottom: i < MOCK_NOTIFICATIONS.length - 1 ? "1px solid #e0e0e0" : "none",
                  }}
                >
                  {!n.isRead && <NotificationNew size={16} style={{ marginTop: "2px", flexShrink: 0, fill: "#406AAF" }} />}
                  <div style={{ flex: 1, marginLeft: n.isRead ? "1.5rem" : 0 }}>
                    <p style={{ margin: 0, fontWeight: n.isRead ? 400 : 600, fontSize: "0.875rem" }}>{n.title}</p>
                    <p className="cg-table__muted" style={{ margin: "0.15rem 0 0", fontSize: "0.8125rem" }}>
                      {n.body}
                    </p>
                  </div>
                  <span className="cg-table__muted" style={{ fontSize: "0.75rem", whiteSpace: "nowrap" }}>
                    {n.sentAt}
                  </span>
                </div>
              ))}
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
