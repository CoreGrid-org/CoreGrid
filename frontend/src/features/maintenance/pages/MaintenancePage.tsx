import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, InlineNotification, Select, SelectItem } from "@carbon/react";
import { Add, NotificationNew } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { MOCK_PREVENTIVE_SCHEDULE, MOCK_NOTIFICATIONS } from "../data/mockMaintenance";
import { useMaintenanceList } from "../hooks/useMaintenance";

export default function MaintenancePage() {
  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState("");
  const { data: records, isLoading, isError, error } = useMaintenanceList({
    status: statusFilter ? (statusFilter as any) : undefined,
  });

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Maintenance</h1>
          <p className="cg-page__subtitle">Faults, repairs and preventive schedules (FR-033 to FR-042).</p>
        </div>
        <Button renderIcon={Add} onClick={() => navigate("new")}>New maintenance record</Button>
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
            {isError && (
              <InlineNotification
                kind="error"
                title="Could not load maintenance records"
                subtitle={getErrorMessage(error, "Something went wrong.")}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}
            <div className="cg-section">
              <div className="cg-toolbar" style={{ marginBottom: "1rem" }}>
                <div style={{ width: "12rem" }}>
                  <Select
                    id="maintenance-status-filter"
                    labelText="Filter by Status"
                    hideLabel
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                  >
                    <SelectItem value="" text="All Statuses" />
                    <SelectItem value="REQUESTED" text="Requested" />
                    <SelectItem value="APPROVED" text="Approved" />
                    <SelectItem value="IN_PROGRESS" text="In Progress" />
                    <SelectItem value="COMPLETED" text="Completed" />
                    <SelectItem value="CANCELLED" text="Cancelled" />
                  </Select>
                </div>
              </div>
              {isLoading ? (
                <div className="cg-placeholder"><p>Loading records…</p></div>
              ) : records && records.length > 0 ? (
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
                    {records.map((rec) => (
                      <tr key={rec.id} onClick={() => navigate(rec.id)} style={{ cursor: "pointer" }}>
                        <td>
                          <span className="cg-table__mono">{rec.asset_code}</span>
                          <br />
                          <span className="cg-table__muted">{rec.asset_name}</span>
                        </td>
                        <td className="cg-table__muted">{rec.type === "CORRECTIVE" ? "Corrective" : "Preventive"}</td>
                        <td>
                          <Tag type={statusTagColor(rec.priority)}>{formatStatusLabel(rec.priority)}</Tag>
                        </td>
                        <td>
                          <Tag type={statusTagColor(rec.status)}>{formatStatusLabel(rec.status)}</Tag>
                        </td>
                        <td className="cg-table__muted">{rec.assignee_email ?? "Unassigned"}</td>
                        <td className="cg-table__muted">{rec.estimated_cost ? `LKR ${rec.estimated_cost.toLocaleString()}` : "—"}</td>
                        <td className="cg-table__muted">{rec.actual_cost ? `LKR ${rec.actual_cost.toLocaleString()}` : "—"}</td>
                        <td className="cg-table__muted">{new Date(rec.created_at).toLocaleDateString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder"><p>No maintenance records found.</p></div>
              )}
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
