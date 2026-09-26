import { useMemo, useState } from "react";
import { ComboBox, Select, SelectItem, Tag } from "@carbon/react";
import { useDepartments } from "@/features/assets/hooks/useAssets";
import type { Department } from "@/features/assets/types/asset";
import { useUsersList } from "@/features/users/hooks/useUsers";
import type { MaintenanceQueryParameters, MaintenanceRecord, MaintenanceStatus } from "@/features/maintenance/types/maintenance";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { formatDate, hasDateRangeErrors, validateDateRange, type DateRange } from "@/shared/lib/dates";
import DateRangeFilter from "@/shared/components/DateRangeFilter";
import { getMaintenanceReportRecords } from "../api/maintenanceReport";
import { useReportData } from "../hooks/useReportData";
import { downloadCsv, downloadPdf, exportBlockReason, formatCurrency, type ExportColumn, type ReportExport } from "../lib/export";
import { ReportExportBar, ReportFilters, ReportStats, ReportStatus, ReportTable, type ReportColumn } from "./ReportParts";

type User = NonNullable<ReturnType<typeof useUsersList>["data"]>[number];
const userName = (u: User | null) => (u ? `${u.given_name} ${u.family_name}` : "");

const EXPORT_COLUMNS: ExportColumn<MaintenanceRecord>[] = [
  { header: "Asset code", width: 26, value: (r) => r.asset_code },
  { header: "Asset name", width: 34, value: (r) => r.asset_name },
  { header: "Asset type", width: 28, value: (r) => r.asset_type_name },
  { header: "Type", width: 22, value: (r) => formatStatusLabel(r.type) },
  { header: "Priority", width: 20, value: (r) => formatStatusLabel(r.priority) },
  { header: "Status", width: 24, value: (r) => formatStatusLabel(r.status) },
  { header: "Assignee", width: 34, value: (r) => r.assignee_email ?? "Unassigned" },
  { header: "Actual cost", width: 24, value: (r) => (r.actual_cost != null ? formatCurrency(r.actual_cost) : "-") },
  { header: "Requested", width: 24, value: (r) => formatDate(r.created_at) },
];

const DETAIL_COLUMNS: ReportColumn<MaintenanceRecord>[] = [
  {
    header: "Asset",
    render: (r) => (
      <>
        <span className="cg-table__mono">{r.asset_code}</span>
        <br />
        <span className="cg-table__muted">{r.asset_name}</span>
      </>
    ),
  },
  { header: "Type", muted: true, render: (r) => formatStatusLabel(r.type) },
  { header: "Priority", render: (r) => <Tag type={statusTagColor(r.priority)}>{formatStatusLabel(r.priority)}</Tag> },
  { header: "Status", render: (r) => <Tag type={statusTagColor(r.status)}>{formatStatusLabel(r.status)}</Tag> },
  { header: "Assignee", muted: true, render: (r) => r.assignee_email ?? "Unassigned" },
  { header: "Estimated cost", muted: true, render: (r) => (r.estimated_cost != null ? formatCurrency(r.estimated_cost) : "-") },
  { header: "Actual cost", muted: true, render: (r) => (r.actual_cost != null ? formatCurrency(r.actual_cost) : "-") },
  { header: "Requested", muted: true, render: (r) => formatDate(r.created_at) },
];

type Breakdown = { key: string; count: number; cost: number };
const BREAKDOWN_COLUMNS: ReportColumn<Breakdown>[] = [
  { header: "Asset type", render: (b) => b.key },
  { header: "Repairs", muted: true, render: (b) => b.count },
  { header: "Total cost", muted: true, render: (b) => formatCurrency(b.cost) },
];

// Date/department/assignee/status/priority/type filters over GET
// /api/maintenance (scoped to the caller's role by the backend), with PDF/CSV
// export of exactly what's filtered.
export default function MaintenanceReportPanel() {
  const [department, setDepartment] = useState<Department | null>(null);
  const [assignee, setAssignee] = useState<User | null>(null);
  const [status, setStatus] = useState("");
  const [priority, setPriority] = useState("");
  const [type, setType] = useState("");
  const [range, setRange] = useState<DateRange>({});

  const { data: departments } = useDepartments();
  const { data: users } = useUsersList();

  const rangeErrors = validateDateRange(range);
  const rangeValid = !hasDateRangeErrors(rangeErrors);

  const query: Omit<MaintenanceQueryParameters, "page" | "pageSize"> = {
    departmentId: department?.id,
    assigneeId: assignee?.id,
    status: (status || undefined) as MaintenanceStatus | undefined,
    priority: (priority || undefined) as MaintenanceQueryParameters["priority"],
    type: (type || undefined) as MaintenanceQueryParameters["type"],
    // The backend takes plain calendar dates (DateOnly), inclusive of both ends.
    dateFrom: range.from,
    dateTo: range.to,
  };
  const queryKey = JSON.stringify(query);
  const report = useReportData(queryKey, rangeValid, (token) => getMaintenanceReportRecords(query, token));
  const records = useMemo(() => report.data ?? [], [report.data]);

  const { totalCost, averageCost, byAssetType } = useMemo(() => {
    const withCost = records.filter((r) => r.actual_cost != null);
    const total = withCost.reduce((sum, r) => sum + (r.actual_cost ?? 0), 0);
    const groups = new Map<string, Breakdown>();
    for (const r of records) {
      const key = r.asset_type_name || "Unspecified";
      const current = groups.get(key) ?? { key, count: 0, cost: 0 };
      groups.set(key, { key, count: current.count + 1, cost: current.cost + (r.actual_cost ?? 0) });
    }
    return {
      totalCost: total,
      averageCost: withCost.length > 0 ? total / withCost.length : 0,
      byAssetType: [...groups.values()].sort((a, b) => b.count - a.count),
    };
  }, [records]);

  const filterLabels = [
    department && `Department: ${department.name}`,
    assignee && `Assignee: ${userName(assignee)}`,
    status && `Status: ${formatStatusLabel(status)}`,
    priority && `Priority: ${formatStatusLabel(priority)}`,
    type && `Type: ${formatStatusLabel(type)}`,
    range.from && `Requested from ${range.from}`,
    range.to && `Requested to ${range.to}`,
  ].filter((label): label is string => Boolean(label));

  const clearFilters = () => {
    setDepartment(null);
    setAssignee(null);
    setStatus("");
    setPriority("");
    setType("");
    setRange({});
  };

  const exportData: ReportExport<MaintenanceRecord> = {
    title: "Maintenance Report",
    fileName: "maintenance-report",
    columns: EXPORT_COLUMNS,
    rows: records,
    summary: [`Records in scope: ${records.length.toLocaleString()}`, `Total cost: ${formatCurrency(totalCost)}`],
    filters: filterLabels,
  };

  return (
    <div className="cg-report">
      <ReportFilters activeCount={filterLabels.length} onClear={clearFilters} isRefreshing={report.isRefreshing}>
        <ComboBox<Department>
          id="maintenance-report-department"
          titleText="Department"
          placeholder="All departments"
          items={departments ?? []}
          itemToString={(d) => d?.name ?? ""}
          selectedItem={department}
          shouldFilterItem={comboBoxFilter(department)}
          onChange={({ selectedItem }) => setDepartment(selectedItem ?? null)}
        />
        <ComboBox<User>
          id="maintenance-report-assignee"
          titleText="Assignee"
          placeholder="All assignees"
          items={users ?? []}
          itemToString={userName}
          selectedItem={assignee}
          shouldFilterItem={comboBoxFilter(assignee)}
          onChange={({ selectedItem }) => setAssignee(selectedItem ?? null)}
        />
        <Select id="maintenance-report-status" labelText="Status" value={status} onChange={(e) => setStatus(e.target.value)}>
          <SelectItem value="" text="All statuses" />
          {["REQUESTED", "APPROVED", "IN_PROGRESS", "COMPLETED", "CANCELLED"].map((s) => (
            <SelectItem key={s} value={s} text={formatStatusLabel(s)} />
          ))}
        </Select>
        <Select id="maintenance-report-priority" labelText="Priority" value={priority} onChange={(e) => setPriority(e.target.value)}>
          <SelectItem value="" text="All priorities" />
          {["LOW", "MEDIUM", "HIGH", "CRITICAL"].map((p) => (
            <SelectItem key={p} value={p} text={formatStatusLabel(p)} />
          ))}
        </Select>
        <Select id="maintenance-report-type" labelText="Type" value={type} onChange={(e) => setType(e.target.value)}>
          <SelectItem value="" text="All types" />
          <SelectItem value="CORRECTIVE" text="Corrective" />
          <SelectItem value="PREVENTIVE" text="Preventive" />
        </Select>
        <DateRangeFilter
          idPrefix="maintenance-report-date"
          fromLabel="Requested from"
          toLabel="Requested to"
          value={range}
          onChange={setRange}
          errors={rangeErrors}
        />
      </ReportFilters>

      <ReportStatus title="Could not load the maintenance report" error={report.error} isInitialLoading={report.isInitialLoading} />

      {report.data && (
        <div className={report.isRefreshing || !rangeValid ? "cg-report__results is-stale" : "cg-report__results"}>
          <ReportStats
            stats={[
              { label: "Records in scope", value: records.length.toLocaleString() },
              { label: "Total cost", value: formatCurrency(totalCost) },
              { label: "Average cost per repair", value: formatCurrency(averageCost) },
            ]}
          />
          <ReportExportBar
            count={records.length}
            noun="maintenance records"
            onPdf={() => downloadPdf(exportData)}
            onCsv={() => downloadCsv(exportData)}
            disabledReason={exportBlockReason(rangeValid, report.isRefreshing, records.length)}
          />
          <ReportTable
            title="By asset type"
            columns={BREAKDOWN_COLUMNS}
            rows={byAssetType}
            rowKey={(b) => b.key}
            emptyText="No maintenance records match these filters."
            pageable={false}
          />
          <ReportTable
            key={queryKey}
            title="Maintenance details"
            columns={DETAIL_COLUMNS}
            rows={records}
            rowKey={(r) => r.id}
            emptyText="No maintenance records match these filters."
          />
        </div>
      )}
    </div>
  );
}
