import { Button, InlineNotification, Select, SelectItem, SkeletonText, Tag } from "@carbon/react";
import { DocumentExport, DocumentPdf } from "@carbon/icons-react";
import { jsPDF } from "jspdf";
import { useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { listMaintenanceRecords } from "@/features/maintenance/api/maintenance";
import type { MaintenanceRecord, MaintenanceStatus, MaintenanceType, MaintenancePriority } from "@/features/maintenance/types/maintenance";

const MAINTENANCE_STATUSES: MaintenanceStatus[] = ["REQUESTED", "APPROVED", "IN_PROGRESS", "COMPLETED", "CANCELLED"];
const MAINTENANCE_TYPES: MaintenanceType[] = ["CORRECTIVE", "PREVENTIVE"];
const MAINTENANCE_PRIORITIES: MaintenancePriority[] = ["LOW", "MEDIUM", "HIGH", "CRITICAL"];

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", maximumFractionDigits: 0 }).format(value);
}

function formatDate(value: string | undefined) {
  if (!value) return "-";
  return new Date(`${value}T00:00:00`).toLocaleDateString();
}

function csvEscape(value: string | number | undefined) {
  if (value === undefined || value === null) return "";
  const text = String(value);
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function downloadCsv(records: MaintenanceRecord[]) {
  const headers = ["Asset code", "Name", "Asset type", "Department", "Type", "Priority", "Status", "Actual cost", "Completion date"];
  const rows = records.map((record) => [
    record.asset_code,
    record.asset_name,
    record.asset_type_name,
    record.department_name,
    record.type,
    record.priority,
    formatStatusLabel(record.status),
    record.actual_cost ?? 0,
    record.completion_date ?? "-",
  ]);
  const csv = [headers, ...rows].map((row) => row.map(csvEscape).join(",")).join("\n");
  const blob = new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" });
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
  const columnWidths = [25, 35, 30, 30, 25, 25, 25, 25, 30];
  const headers = ["Asset code", "Name", "Asset type", "Department", "Type", "Priority", "Status", "Cost", "Completed"];
  let y = 15;

  const drawHeader = () => {
    document.setFillColor(224, 224, 224);
    document.rect(left, y - 5, columnWidths.reduce((sum, width) => sum + width, 0), 8, "F");
    document.setFont("helvetica", "bold");
    document.setFontSize(7);
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
  
  const completedRecords = records.filter(r => r.status === "COMPLETED").length;
  const avgCost = completedRecords > 0 ? totalCost / completedRecords : 0;
  
  document.text(`Records: ${records.length.toLocaleString()}`, left, y);
  document.text(`Total cost: ${formatCurrency(totalCost)}`, left + 60, y);
  document.text(`Average cost per repair: ${formatCurrency(avgCost)}`, left + 145, y);
  y += 9;
  drawHeader();

  records.forEach((record) => {
    const values = [
      record.asset_code,
      record.asset_name,
      record.asset_type_name,
      record.department_name,
      record.type,
      record.priority,
      formatStatusLabel(record.status),
      record.actual_cost ? formatCurrency(record.actual_cost) : "-",
      formatDate(record.completion_date),
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

export default function MaintenanceReportPanel() {
  const { getAccessToken } = useThunderID();
  const [records, setRecords] = useState<MaintenanceRecord[]>([]);
  const [status, setStatus] = useState<MaintenanceStatus | "">("");
  const [type, setType] = useState<MaintenanceType | "">("");
  const [priority, setPriority] = useState<MaintenancePriority | "">("");
  const [error, setError] = useState<unknown>();
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    const query: any = {};
    if (status) query.status = status;
    if (type) query.type = type;
    if (priority) query.priority = priority;

    getAccessToken()
      .then((token) => listMaintenanceRecords(query, token))
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
  }, [getAccessToken, status, type, priority]);

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

  const totalCost = records.reduce((total, r) => total + (r.actual_cost ?? 0), 0);
  const completedRecords = records.filter(r => r.status === "COMPLETED").length;
  const avgCost = completedRecords > 0 ? totalCost / completedRecords : 0;
  
  const byAssetType = Array.from(
    records.reduce((groups, record) => {
      const current = groups.get(record.asset_type_name) ?? { count: 0, cost: 0 };
      groups.set(record.asset_type_name, {
        count: current.count + 1,
        cost: current.cost + (record.actual_cost ?? 0),
      });
      return groups;
    }, new Map<string, { count: number; cost: number }>()),
  ).sort(([, left], [, right]) => right.count - left.count);

  return (
    <div className="cg-section">
      <div className="cg-toolbar" style={{ flexWrap: "wrap", gap: "0.75rem", alignItems: "end" }}>
        <Select id="maintenance-report-type" labelText="Type" value={type} onChange={(event) => setType(event.target.value as any)}>
          <SelectItem value="" text="All types" />
          {MAINTENANCE_TYPES.map((value) => <SelectItem key={value} value={value} text={value} />)}
        </Select>
        <Select id="maintenance-report-priority" labelText="Priority" value={priority} onChange={(event) => setPriority(event.target.value as any)}>
          <SelectItem value="" text="All priorities" />
          {MAINTENANCE_PRIORITIES.map((value) => <SelectItem key={value} value={value} text={value} />)}
        </Select>
        <Select id="maintenance-report-status" labelText="Status" value={status} onChange={(event) => setStatus(event.target.value as any)}>
          <SelectItem value="" text="All statuses" />
          {MAINTENANCE_STATUSES.map((value) => <SelectItem key={value} value={value} text={formatStatusLabel(value)} />)}
        </Select>
        <Button
          kind="ghost"
          size="md"
          onClick={() => {
            setType("");
            setPriority("");
            setStatus("");
          }}
        >
          Clear filters
        </Button>
      </div>
      
      <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: 0, gridTemplateColumns: "repeat(3, 1fr)" }}>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Records this period</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{records.length.toLocaleString()}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Total cost</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{formatCurrency(totalCost)}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Average cost per repair</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{formatCurrency(avgCost)}</p>
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
          {byAssetType.length === 0 && <tr><td colSpan={3} className="cg-table__muted">No records found.</td></tr>}
        </tbody>
      </table>

      <div style={{ marginTop: "2rem" }}>
        <div className="cg-section__header">
          <div>
            <h2 className="cg-section__title">Record details</h2>
            <p className="cg-section__subtitle">Showing {records.length.toLocaleString()} filtered records</p>
          </div>
        </div>
        <div style={{ overflowX: "auto" }}>
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Asset code</th>
                <th>Name</th>
                <th>Asset type</th>
                <th>Department</th>
                <th>Type</th>
                <th>Priority</th>
                <th>Status</th>
                <th>Cost</th>
                <th>Completed</th>
              </tr>
            </thead>
            <tbody>
              {records.map((record) => (
                <tr key={record.id}>
                  <td>{record.asset_code}</td>
                  <td>{record.asset_name}</td>
                  <td className="cg-table__muted">{record.asset_type_name}</td>
                  <td className="cg-table__muted">{record.department_name}</td>
                  <td className="cg-table__muted">{record.type}</td>
                  <td className="cg-table__muted">{record.priority}</td>
                  <td><Tag type={statusTagColor(record.status)}>{formatStatusLabel(record.status)}</Tag></td>
                  <td className="cg-table__muted">{record.actual_cost ? formatCurrency(record.actual_cost) : "-"}</td>
                  <td className="cg-table__muted">{formatDate(record.completion_date)}</td>
                </tr>
              ))}
              {records.length === 0 && <tr><td colSpan={9} className="cg-table__muted">No records found.</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
