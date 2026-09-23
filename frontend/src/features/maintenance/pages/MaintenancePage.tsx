import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
  Tag,
  Button,
  InlineNotification,
  Select,
  SelectItem,
  Dropdown,
  DatePicker,
  DatePickerInput,
  Pagination,
} from "@carbon/react";
import { Add } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useMe } from "@/features/auth/hooks/useMe";
import { useDepartments } from "@/features/assets/hooks/useAssets";
import { useUsersList } from "@/features/users/hooks/useUsers";
import { MOCK_PREVENTIVE_SCHEDULE } from "../data/mockMaintenance";
import { useMaintenanceList } from "../hooks/useMaintenance";
import type { MaintenanceStatus } from "../types/maintenance";

export default function MaintenancePage() {
  const navigate = useNavigate();
  const { data: me } = useMe();
  // POST /api/maintenance (direct record creation) is InventoryOfficer-only
  // on the backend — stricter than the general maintenance:request policy
  // on purpose (plan §5.4), so Administrator doesn't get this button.
  // Reporting a fault (POST /api/maintenance/faults) is broader
  // (Staff/Officer/Administrator) and both roles get it here.
  const canCreateDirectly = me?.role === "InventoryOfficer";
  const canReportFault = me?.role === "InventoryOfficer" || me?.role === "Administrator";
  const [statusFilter, setStatusFilter] = useState("");
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const [assigneeId, setAssigneeId] = useState<string | undefined>();
  const [dateFrom, setDateFrom] = useState<string | undefined>();
  const [dateTo, setDateTo] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data: departments } = useDepartments();
  const { data: users } = useUsersList();

  useEffect(() => {
    setPage(1);
  }, [statusFilter, departmentId, assigneeId, dateFrom, dateTo]);

  const { data: pageResult, isLoading, isError, error } = useMaintenanceList({
    status: statusFilter ? (statusFilter as MaintenanceStatus) : undefined,
    departmentId,
    assigneeId,
    dateFrom,
    dateTo,
    page,
    pageSize,
  });
  const records = pageResult.items;

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Maintenance</h1>
          <p className="cg-page__subtitle">Faults, repairs and preventive schedules</p>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          {canReportFault && (
            <Button kind={canCreateDirectly ? "tertiary" : "primary"} onClick={() => navigate("report")}>
              Report fault
            </Button>
          )}
          {canCreateDirectly && (
            <Button renderIcon={Add} onClick={() => navigate("new")}>
              New maintenance record
            </Button>
          )}
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Maintenance sections">
          <Tab>Maintenance Records</Tab>
          <Tab>Preventive Schedule</Tab>
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
              <div className="cg-toolbar" style={{ marginBottom: "1rem", flexWrap: "wrap", gap: "0.75rem" }}>
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
                <Dropdown
                  id="maintenance-department-filter"
                  titleText="Department"
                  hideLabel
                  label="All departments"
                  items={["", ...(departments ?? []).map((d) => d.id)]}
                  itemToString={(id) =>
                    !id ? "All departments" : (departments ?? []).find((d) => d.id === id)?.name ?? id
                  }
                  selectedItem={departmentId ?? ""}
                  onChange={({ selectedItem }) => setDepartmentId(selectedItem || undefined)}
                  style={{ minWidth: "12rem" }}
                />
                <Dropdown
                  id="maintenance-assignee-filter"
                  titleText="Assignee"
                  hideLabel
                  label="All assignees"
                  items={["", ...(users ?? []).map((u) => u.id)]}
                  itemToString={(id) => {
                    if (!id) return "All assignees";
                    const u = (users ?? []).find((u) => u.id === id);
                    return u ? `${u.given_name} ${u.family_name}` : id;
                  }}
                  selectedItem={assigneeId ?? ""}
                  onChange={({ selectedItem }) => setAssigneeId(selectedItem || undefined)}
                  style={{ minWidth: "12rem" }}
                />
                <DatePicker
                  datePickerType="single"
                  dateFormat="Y-m-d"
                  onChange={([date]) => setDateFrom(date ? date.toISOString() : undefined)}
                >
                  <DatePickerInput id="maintenance-date-from" labelText="Requested from" placeholder="yyyy-mm-dd" />
                </DatePicker>
                <DatePicker
                  datePickerType="single"
                  dateFormat="Y-m-d"
                  onChange={([date]) => setDateTo(date ? date.toISOString() : undefined)}
                >
                  <DatePickerInput id="maintenance-date-to" labelText="Requested to" placeholder="yyyy-mm-dd" />
                </DatePicker>
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

            {pageResult.total_count > 0 && (
              <Pagination
                page={page}
                pageSize={pageSize}
                pageSizes={[10, 20, 50, 100]}
                totalItems={pageResult.total_count}
                onChange={({ page: nextPage, pageSize: nextPageSize }) => {
                  setPage(nextPage);
                  setPageSize(nextPageSize);
                }}
              />
            )}
          </TabPanel>

          {/* ── Preventive schedule ─────────────────────────────────────── */}
          <TabPanel>
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
        </TabPanels>
      </Tabs>
    </div>
  );
}
