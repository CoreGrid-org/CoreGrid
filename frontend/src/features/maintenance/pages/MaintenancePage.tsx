import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
  Tag,
  Button,
  ActionableNotification,
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
import { formatDate } from "@/shared/lib/dates";
import PhotoThumbnail from "@/shared/components/PhotoThumbnail";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { usePermissions } from "@/features/auth/hooks/usePermissions";
import { useDepartments } from "@/features/assets/hooks/useAssets";
import { useUsersList } from "@/features/users/hooks/useUsers";
import { MOCK_PREVENTIVE_SCHEDULE } from "../data/mockMaintenance";
import { useMaintenanceList } from "../hooks/useMaintenance";
import ReportFaultModal from "../components/ReportFaultModal";
import CreateMaintenanceModal from "../components/CreateMaintenanceModal";
import type { MaintenanceStatus } from "../types/maintenance";

export default function MaintenancePage() {
  const navigate = useNavigate();
  // Direct record creation is Officer/Administrator; reporting a fault
  // is broader. Auditor gets neither — Maintenance is read-only for them.
  const { can } = usePermissions();
  const canCreateDirectly = can("maintenance:create-direct");
  const canReportFault = can("maintenance:report-fault");
  const [isReportFaultOpen, setReportFaultOpen] = useState(false);
  const [reportedRecord, setReportedRecord] = useState<{ id: string; asset_code: string } | null>(null);
  const reportFaultButtonRef = useRef<HTMLButtonElement>(null);
  const [isCreateOpen, setCreateOpen] = useState(false);
  const [createdRecord, setCreatedRecord] = useState<{ id: string; asset_code: string } | null>(null);
  const createButtonRef = useRef<HTMLButtonElement>(null);
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

  const { data: pageResult, isLoading, isError, error, refetch } = useMaintenanceList({
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
          <p className="cg-page__subtitle">
            Log asset faults, track work orders, manage repairs, and oversee preventive maintenance schedules across the organization.
          </p>
        </div>
        <div className="cg-actions">
          {canReportFault && (
            <Button
              ref={reportFaultButtonRef}
              kind={canCreateDirectly ? "tertiary" : "primary"}
              onClick={() => setReportFaultOpen(true)}
            >
              Report fault
            </Button>
          )}
          {canCreateDirectly && (
            <Button ref={createButtonRef} renderIcon={Add} onClick={() => setCreateOpen(true)}>
              New maintenance record
            </Button>
          )}
        </div>
      </div>

      {reportedRecord && (
        <ActionableNotification
          inline
          kind="success"
          title="Fault reported"
          subtitle={`A maintenance request was created for ${reportedRecord.asset_code}.`}
          lowContrast
          actionButtonLabel="View request"
          onActionButtonClick={() => navigate(reportedRecord.id)}
          onClose={() => setReportedRecord(null)}
          className="cg-page-notification"
        />
      )}

      {createdRecord && (
        <ActionableNotification
          inline
          kind="success"
          title="Maintenance record created"
          subtitle={`A work order was created for ${createdRecord.asset_code}.`}
          lowContrast
          actionButtonLabel="View record"
          onActionButtonClick={() => navigate(createdRecord.id)}
          onClose={() => setCreatedRecord(null)}
          className="cg-page-notification"
        />
      )}

      {isCreateOpen && (
        <CreateMaintenanceModal
          launcherButtonRef={createButtonRef}
          onClose={() => setCreateOpen(false)}
          onCreated={(record) => {
            setCreateOpen(false);
            setCreatedRecord({ id: record.id, asset_code: record.asset_code });
            refetch();
          }}
        />
      )}

      {isReportFaultOpen && (
        <ReportFaultModal
          launcherButtonRef={reportFaultButtonRef}
          onClose={() => setReportFaultOpen(false)}
          onReported={(record) => {
            setReportFaultOpen(false);
            setReportedRecord({ id: record.id, asset_code: record.asset_code });
            refetch();
          }}
        />
      )}

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
                className="cg-page-notification"
              />
            )}
            <div className="cg-section">
              <div className="cg-toolbar cg-toolbar--spaced">
                <div className="cg-toolbar__filter--fixed">
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
                  className="cg-toolbar__filter"
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
                  className="cg-toolbar__filter"
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
                <table className="cg-table">
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
                      <th>Photo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {records.map((rec) => (
                      <tr key={rec.id} onClick={() => navigate(rec.id)}>
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
                        <td className="cg-table__muted">{rec.estimated_cost ? `LKR ${rec.estimated_cost.toLocaleString()}` : "-"}</td>
                        <td className="cg-table__muted">{rec.actual_cost ? `LKR ${rec.actual_cost.toLocaleString()}` : "-"}</td>
                        <td className="cg-table__muted">{formatDate(rec.created_at)}</td>
                        {/* The thumbnail opens its own viewer; don't also open the record. */}
                        <td onClick={(e) => e.stopPropagation()}>
                          {rec.photo_url ? (
                            <PhotoThumbnail url={rec.photo_url} alt={rec.description} title={`Photo: ${rec.asset_code}`} />
                          ) : (
                            <span className="cg-table__muted">-</span>
                          )}
                        </td>
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
