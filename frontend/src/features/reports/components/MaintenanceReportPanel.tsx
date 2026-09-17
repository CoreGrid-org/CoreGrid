import { Button, Dropdown, DatePicker, DatePickerInput, InlineNotification, Pagination, Select, SelectItem, SkeletonText, Tag } from "@carbon/react";
import { DocumentExport, DocumentPdf } from "@carbon/icons-react";
import { jsPDF } from "jspdf";
import { useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useDepartments } from "@/features/assets/hooks/useAssets";
import { useUsersList } from "@/features/users/hooks/useUsers";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { listMaintenanceRecords } from "@/features/maintenance/api/maintenance";
import { getMaintenanceReportRecords } from "../api/maintenanceReport";
import type {
  MaintenanceQueryParameters,
  MaintenanceRecord,
  MaintenanceStatus,
  PagedMaintenanceRecords,
} from "@/features/maintenance/types/maintenance";

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", maximumFractionDigits: 0 }).format(value);
}

function csvEscape(value: string | number) {
  const text = String(value);
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function downloadCsv(records: MaintenanceRecord[]) {
  const headers = ["Asset code", "Asset name", "Asset type", "Type", "Priority", "Status", "Assignee", "Estimated cost", "Actual cost", "Completion date", "Requested"];
  const rows = records.map((r) => [
    r.asset_code,
    r.asset_name,
    r.asset_type_name,
    formatStatusLabel(r.type),
    formatStatusLabel(r.priority),
    formatStatusLabel(r.status),
    r.assignee_email ?? "Unassigned",
    r.estimated_cost ?? "",
    r.actual_cost ?? "",
    r.completion_date ?? "",
    r.created_at,
  ]);
  const csv = [headers, ...rows].map((row) => row.map(csvEscape).join(",")).join("\n");
  const blob = new Blob([`﻿${csv}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `maintenance-report-${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function printPdf(records: MaintenanceRecord[], totalCost: number) {
  const document = new jsPDF({ orientation: "landscape", unit: "mm", format: "a4" });
  const pageHeight = document.internal.pageSize.getHeight();
  const left = 10;
  const columnWidths = [26, 34, 28, 22, 20, 24, 34, 24, 24];
  const headers = ["Asset code", "Asset name", "Asset type", "Type", "Priority", "Status", "Assignee", "Actual cost", "Completed"];
  let y = 15;

  const drawHeader = () => {
    document.setFillColor(224, 224, 224);
    document.rect(left, y - 5, columnWidths.reduce((sum, width) => sum + width, 0), 8, "F");
    document.setFont("helvetica", "bold");
    document.setFontSize(6.5);
    let x = left + 1;
    headers.forEach((header, index) => {
      document.text(header, x, y);
      x += columnWidths[index];
    });
    y += 7;
    document.setFont("helvetica", "normal");
  };

  document.setFont("helvetica", "bold");
  document.setFontSize(16);
  document.text("Maintenance Report", left, y);
  y += 6;
  document.setFont("helvetica", "normal");
  document.setFontSize(8);
  document.text(`Generated ${new Date().toLocaleString()}`, left, y);
  y += 8;
  document.setFontSize(9);
  document.text(`Records in scope: ${records.length.toLocaleString()}`, left, y);
  document.text(`Total cost: ${formatCurrency(totalCost)}`, left + 70, y);
  y += 9;
  drawHeader();

  records.forEach((r) => {
    const values = [
      r.asset_code,
      r.asset_name,
      r.asset_type_name,
      formatStatusLabel(r.type),
      formatStatusLabel(r.priority),
      formatStatusLabel(r.status),
      r.assignee_email ?? "Unassigned",
      r.actual_cost ? formatCurrency(r.actual_cost) : "—",
      r.completion_date ?? "—",
    ];
    const lines = values.map((value, index) => document.splitTextToSize(String(value), columnWidths[index] - 2));
    const rowHeight = Math.max(...lines.map((value) => value.length)) * 3.2 + 3;
    if (y + rowHeight > pageHeight - 10) {
      document.addPage();
      y = 15;
      drawHeader();
    }
    let x = left + 1;
    lines.forEach((value, index) => {
      document.text(value, x, y, { baseline: "top" });
      x += columnWidths[index];
    });
    document.setDrawColor(210, 210, 210);
    document.line(left, y + rowHeight - 1, left + columnWidths.reduce((sum, width) => sum + width, 0), y + rowHeight - 1);
    y += rowHeight;
  });

  document.save(`maintenance-report-${new Date().toISOString().slice(0, 10)}.pdf`);
}

// FR-084/085/086: date/department/category/status/condition filters, PDF/CSV
// export, org/department scoping. "Category" here is asset type (the closest
// equivalent Maintenance has — MaintenanceRecord has no asset category of its
// own); scoping follows whatever GET /api/maintenance itself already applies
// to the caller's role (same posture as Asset Inventory's own panel).
export default function MaintenanceReportPanel() {
  const { getAccessToken } = useThunderID();
  const [records, setRecords] = useState<MaintenanceRecord[]>([]);
  const [departmentId, setDepartmentId] = useState<string | undefined>();
  const [assigneeId, setAssigneeId] = useState<string | undefined>();
  const [status, setStatus] = useState("");
  const [priority, setPriority] = useState("");
  const [type, setType] = useState("");
  const [dateFrom, setDateFrom] = useState<string | undefined>();
  const [dateTo, setDateTo] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [error, setError] = useState<unknown>();
  const [isLoading, setIsLoading] = useState(true);

  const { data: departments } = useDepartments();
  const { data: users } = useUsersList();

  const query: Omit<MaintenanceQueryParameters, "page" | "pageSize"> = {
    departmentId,
    assigneeId,
    status: status ? (status as MaintenanceStatus) : undefined,
    priority: priority ? (priority as MaintenanceQueryParameters["priority"]) : undefined,
    type: type ? (type as MaintenanceQueryParameters["type"]) : undefined,
    dateFrom,
    dateTo,
  };

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getMaintenanceReportRecords(query, token))
      .then((result) => {
        if (!cancelled) {
          setRecords(result);
          setIsLoading(false);
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setError(reason);
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- query is rebuilt every render from the same state below
  }, [getAccessToken, departmentId, assigneeId, status, priority, type, dateFrom, dateTo]);

  useEffect(() => {
    setPage(1);
  }, [departmentId, assigneeId, status, priority, type, dateFrom, dateTo]);

  // Real server-side pagination for the detail table — a separate request
  // per page against GET /api/maintenance directly, not a client-side slice
  // of `records` above (which stays as the full filtered set, fetched page
  // by page in the background, purely to drive the stats/by-asset-type
  // breakdown and PDF/CSV export — those need every matching record).
  const [detailPage, setDetailPage] = useState<PagedMaintenanceRecords>({
    items: [], total_count: 0, page: 1, page_size: pageSize, total_pages: 0,
  });

  useEffect(() => {
    let cancelled = false;

    getAccessToken()
      .then((token) => listMaintenanceRecords({ ...query, page, pageSize, sortBy: "createdat", sortDirection: "desc" }, token))
      .then((result) => {
        if (!cancelled) setDetailPage(result);
      })
      .catch(() => {
        // Errors here surface through the aggregate fetch's own error
        // state above (same filters, same failure mode) — no need for a
        // second error banner for the same underlying request.
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- query is rebuilt every render from the same state below
  }, [getAccessToken, departmentId, assigneeId, status, priority, type, dateFrom, dateTo, page, pageSize]);

  if (isLoading) {
    return <div className="cg-section"><SkeletonText paragraph lineCount={4} /></div>;
  }

  if (error) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load the maintenance report"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        hideCloseButton
      />
    );
  }

  const completedWithCost = records.filter((r) => r.actual_cost != null);
  const totalCost = completedWithCost.reduce((total, r) => total + (r.actual_cost ?? 0), 0);
  const averageCostPerRepair = completedWithCost.length > 0 ? totalCost / completedWithCost.length : 0;

  const byAssetType = Array.from(
    records.reduce((groups, r) => {
      const key = r.asset_type_name || "Unspecified";
      const current = groups.get(key) ?? { count: 0, cost: 0 };
      groups.set(key, { count: current.count + 1, cost: current.cost + (r.actual_cost ?? 0) });
      return groups;
    }, new Map<string, { count: number; cost: number }>()),
  ).sort(([, left], [, right]) => right.count - left.count);

  return (
    <div className="cg-section">
      <div className="cg-toolbar" style={{ flexWrap: "wrap", gap: "0.75rem", alignItems: "end" }}>
        <Dropdown
          id="maintenance-report-department"
          titleText="Department"
          label="All departments"
          items={["", ...(departments ?? []).map((d) => d.id)]}
          itemToString={(id) => (!id ? "All departments" : (departments ?? []).find((d) => d.id === id)?.name ?? id)}
          selectedItem={departmentId ?? ""}
          onChange={({ selectedItem }) => setDepartmentId(selectedItem || undefined)}
          style={{ minWidth: "12rem" }}
        />
        <Dropdown
          id="maintenance-report-assignee"
          titleText="Assignee"
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
        <Select id="maintenance-report-status" labelText="Status" value={status} onChange={(e) => setStatus(e.target.value)}>
          <SelectItem value="" text="All statuses" />
          <SelectItem value="REQUESTED" text="Requested" />
          <SelectItem value="APPROVED" text="Approved" />
          <SelectItem value="IN_PROGRESS" text="In Progress" />
          <SelectItem value="COMPLETED" text="Completed" />
          <SelectItem value="CANCELLED" text="Cancelled" />
        </Select>
        <Select id="maintenance-report-priority" labelText="Priority" value={priority} onChange={(e) => setPriority(e.target.value)}>
          <SelectItem value="" text="All priorities" />
          <SelectItem value="LOW" text="Low" />
          <SelectItem value="MEDIUM" text="Medium" />
          <SelectItem value="HIGH" text="High" />
          <SelectItem value="CRITICAL" text="Critical" />
        </Select>
        <Select id="maintenance-report-type" labelText="Type" value={type} onChange={(e) => setType(e.target.value)}>
          <SelectItem value="" text="All types" />
          <SelectItem value="CORRECTIVE" text="Corrective" />
          <SelectItem value="PREVENTIVE" text="Preventive" />
        </Select>
        <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setDateFrom(date ? date.toISOString() : undefined)}>
          <DatePickerInput id="maintenance-report-date-from" labelText="Requested from" placeholder="yyyy-mm-dd" />
        </DatePicker>
        <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setDateTo(date ? date.toISOString() : undefined)}>
          <DatePickerInput id="maintenance-report-date-to" labelText="Requested to" placeholder="yyyy-mm-dd" />
        </DatePicker>
        <Button
          kind="ghost"
          size="md"
          onClick={() => {
            setDepartmentId(undefined);
            setAssigneeId(undefined);
            setStatus("");
            setPriority("");
            setType("");
            setDateFrom(undefined);
            setDateTo(undefined);
          }}
        >
          Clear filters
        </Button>
      </div>

      <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: 0, gridTemplateColumns: "repeat(3, 1fr)" }}>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Records in scope</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{records.length.toLocaleString()}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Total cost</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{formatCurrency(totalCost)}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Average cost per repair</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{formatCurrency(averageCostPerRepair)}</p>
        </div>
      </div>

      <div className="cg-toolbar" style={{ justifyContent: "flex-end", marginTop: "1rem" }}>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button kind="tertiary" size="sm" renderIcon={DocumentPdf} onClick={() => printPdf(records, totalCost)}>
            Export PDF
          </Button>
          <Button kind="tertiary" size="sm" renderIcon={DocumentExport} onClick={() => downloadCsv(records)}>
            Export CSV
          </Button>
        </div>
      </div>

      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr><th>Asset type</th><th>Repairs</th><th>Total cost</th></tr>
        </thead>
        <tbody>
          {byAssetType.map(([assetType, summary]) => (
            <tr key={assetType}>
              <td>{assetType}</td>
              <td className="cg-table__muted">{summary.count}</td>
              <td className="cg-table__muted">{formatCurrency(summary.cost)}</td>
            </tr>
          ))}
          {byAssetType.length === 0 && <tr><td colSpan={3} className="cg-table__muted">No maintenance records found.</td></tr>}
        </tbody>
      </table>

      <div style={{ marginTop: "2rem" }}>
        <div className="cg-section__header">
          <div>
            <h2 className="cg-section__title">Maintenance details</h2>
            <p className="cg-section__subtitle">
              Showing {detailPage.items.length.toLocaleString()} of {detailPage.total_count.toLocaleString()} filtered records
            </p>
          </div>
        </div>
        <div style={{ overflowX: "auto" }}>
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Asset</th>
                <th>Type</th>
                <th>Priority</th>
                <th>Status</th>
                <th>Assignee</th>
                <th>Estimated cost</th>
                <th>Actual cost</th>
                <th>Requested</th>
              </tr>
            </thead>
            <tbody>
              {detailPage.items.map((r) => (
                <tr key={r.id}>
                  <td>
                    <span className="cg-table__mono">{r.asset_code}</span>
                    <br />
                    <span className="cg-table__muted">{r.asset_name}</span>
                  </td>
                  <td className="cg-table__muted">{formatStatusLabel(r.type)}</td>
                  <td><Tag type={statusTagColor(r.priority)}>{formatStatusLabel(r.priority)}</Tag></td>
                  <td><Tag type={statusTagColor(r.status)}>{formatStatusLabel(r.status)}</Tag></td>
                  <td className="cg-table__muted">{r.assignee_email ?? "Unassigned"}</td>
                  <td className="cg-table__muted">{r.estimated_cost != null ? formatCurrency(r.estimated_cost) : "—"}</td>
                  <td className="cg-table__muted">{r.actual_cost != null ? formatCurrency(r.actual_cost) : "—"}</td>
                  <td className="cg-table__muted">{new Date(r.created_at).toLocaleDateString()}</td>
                </tr>
              ))}
              {detailPage.items.length === 0 && <tr><td colSpan={8} className="cg-table__muted">No maintenance records found.</td></tr>}
            </tbody>
          </table>
        </div>
        {detailPage.total_count > 0 && (
          <Pagination
            page={page}
            pageSize={pageSize}
            pageSizes={[10, 20, 50, 100]}
            totalItems={detailPage.total_count}
            onChange={({ page: nextPage, pageSize: nextPageSize }) => {
              setPage(nextPage);
              setPageSize(nextPageSize);
            }}
          />
        )}
      </div>
    </div>
  );
}
