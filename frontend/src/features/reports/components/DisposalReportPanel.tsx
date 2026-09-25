import { Button, DatePicker, DatePickerInput, InlineNotification, Pagination, Select, SelectItem, SkeletonText, Tag } from "@carbon/react";
import { DocumentExport, DocumentPdf } from "@carbon/icons-react";
import { jsPDF } from "jspdf";
import { useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { listDisposals } from "@/features/transfers/services/disposals";
import type { DisposalMethod, DisposalResponse, DisposalStatus, PagedResult } from "@/features/transfers/types";
import { getDisposalReportRecords } from "../api/disposalReport";

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", maximumFractionDigits: 0 }).format(value);
}

function csvEscape(value: string | number) {
  const text = String(value);
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function downloadCsv(records: DisposalResponse[]) {
  const headers = ["Asset code", "Asset name", "Method", "Status", "Estimated residual value", "Requested", "Approved", "Disposed"];
  const rows = records.map((d) => [
    d.asset_code,
    d.asset_name,
    formatStatusLabel(d.disposal_method),
    formatStatusLabel(d.status),
    d.estimated_residual_value,
    d.requested_at,
    d.approved_at ?? "",
    d.disposed_at ?? "",
  ]);
  const csv = [headers, ...rows].map((row) => row.map(csvEscape).join(",")).join("\n");
  const blob = new Blob([`﻿${csv}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `disposal-report-${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function printPdf(records: DisposalResponse[], totalProceeds: number) {
  const document = new jsPDF({ orientation: "landscape", unit: "mm", format: "a4" });
  const pageHeight = document.internal.pageSize.getHeight();
  const left = 10;
  const columnWidths = [28, 38, 26, 24, 32, 24, 24];
  const headers = ["Asset code", "Asset name", "Method", "Status", "Residual value", "Requested", "Disposed"];
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
  document.text("Disposal Report", left, y);
  y += 6;
  document.setFont("helvetica", "normal");
  document.setFontSize(8);
  document.text(`Generated ${new Date().toLocaleString()}`, left, y);
  y += 8;
  document.setFontSize(9);
  document.text(`Disposals in scope: ${records.length.toLocaleString()}`, left, y);
  document.text(`Total proceeds: ${formatCurrency(totalProceeds)}`, left + 70, y);
  y += 9;
  drawHeader();

  records.forEach((d) => {
    const values = [
      d.asset_code,
      d.asset_name,
      formatStatusLabel(d.disposal_method),
      formatStatusLabel(d.status),
      formatCurrency(d.estimated_residual_value),
      new Date(d.requested_at).toLocaleDateString(),
      d.disposed_at ? new Date(d.disposed_at).toLocaleDateString() : "—",
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

  document.save(`disposal-report-${new Date().toISOString().slice(0, 10)}.pdf`);
}

// FR-084/085/086 (Component C): method/status/date filters, PDF/CSV export.
// Scoping follows whatever GET /api/disposals itself already applies to the
// caller's role (same posture as Inventory/Maintenance's own report panels).
export default function DisposalReportPanel() {
  const { getAccessToken } = useThunderID();
  const [records, setRecords] = useState<DisposalResponse[]>([]);
  const [status, setStatus] = useState("");
  const [method, setMethod] = useState("");
  const [dateFrom, setDateFrom] = useState<string | undefined>();
  const [dateTo, setDateTo] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [error, setError] = useState<unknown>();
  const [isLoading, setIsLoading] = useState(true);

  const dateRangeInvalid = !!dateFrom && !!dateTo && new Date(dateFrom).getTime() > new Date(dateTo).getTime();

  const query: Omit<Parameters<typeof getDisposalReportRecords>[0], never> = {
    status: status ? (status as DisposalStatus) : undefined,
    method: method ? (method as DisposalMethod) : undefined,
  };

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getDisposalReportRecords(query, token))
      .then((result) => {
        if (!cancelled) {
          // requested_at date-range filtering happens client-side — the
          // backend endpoint has no date filter of its own (same reasoning
          // as Inventory's own report panel: the underlying list endpoint
          // wasn't built with a report's filters in mind).
          const filtered = result.filter((d) => {
            const requested = new Date(d.requested_at).getTime();
            if (dateFrom && requested < new Date(dateFrom).getTime()) return false;
            if (dateTo && requested > new Date(dateTo).getTime()) return false;
            return true;
          });
          setRecords(filtered);
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
  }, [getAccessToken, status, method, dateFrom, dateTo]);

  useEffect(() => {
    setPage(1);
  }, [status, method, dateFrom, dateTo]);

  // Real server-side pagination for the detail table, same split as
  // MaintenanceReportPanel: `records` above stays the full filtered set
  // (for stats/by-method breakdown/export), this is a separate paged
  // request purely to drive the detail table.
  const [detailPage, setDetailPage] = useState<PagedResult<DisposalResponse>>({
    items: [], total_count: 0, page: 1, page_size: pageSize, total_pages: 0,
  });

  useEffect(() => {
    let cancelled = false;

    getAccessToken()
      .then((token) =>
        listDisposals(
          { status: status ? (status as DisposalStatus) : undefined, method: method ? (method as DisposalMethod) : undefined, page, pageSize },
          token,
        ),
      )
      .then((result) => {
        if (!cancelled) setDetailPage(result);
      })
      .catch(() => {
        // Errors here surface through the aggregate fetch's own error
        // state above (same filters, same failure mode).
      });

    return () => {
      cancelled = true;
    };
  }, [getAccessToken, status, method, page, pageSize]);

  if (isLoading) {
    return <div className="cg-section"><SkeletonText paragraph lineCount={4} /></div>;
  }

  if (error) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load the disposal report"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        hideCloseButton
      />
    );
  }

  const disposed = records.filter((d) => d.status === "DISPOSED");
  const totalProceeds = disposed.reduce((total, d) => total + d.estimated_residual_value, 0);
  const approvalDurationsInDays = disposed
    .filter((d) => d.disposed_at)
    .map((d) => (new Date(d.disposed_at!).getTime() - new Date(d.requested_at).getTime()) / (24 * 60 * 60 * 1000));
  const averageApprovalDays =
    approvalDurationsInDays.length > 0
      ? approvalDurationsInDays.reduce((total, days) => total + days, 0) / approvalDurationsInDays.length
      : 0;

  const byMethod = Array.from(
    records.reduce((groups, d) => {
      const current = groups.get(d.disposal_method) ?? { count: 0, proceeds: 0 };
      const proceeds = d.status === "DISPOSED" ? d.estimated_residual_value : 0;
      groups.set(d.disposal_method, { count: current.count + 1, proceeds: current.proceeds + proceeds });
      return groups;
    }, new Map<DisposalMethod, { count: number; proceeds: number }>()),
  ).sort(([, left], [, right]) => right.count - left.count);

  return (
    <div className="cg-section">
      <div className="cg-toolbar" style={{ flexWrap: "wrap", gap: "0.75rem", alignItems: "end" }}>
        <Select id="disposal-report-status" labelText="Status" value={status} onChange={(e) => setStatus(e.target.value)}>
          <SelectItem value="" text="All statuses" />
          <SelectItem value="PENDING" text="Pending" />
          <SelectItem value="APPROVED" text="Approved" />
          <SelectItem value="REJECTED" text="Rejected" />
          <SelectItem value="REVISION_REQUESTED" text="Revision requested" />
          <SelectItem value="DISPOSED" text="Disposed" />
        </Select>
        <Select id="disposal-report-method" labelText="Method" value={method} onChange={(e) => setMethod(e.target.value)}>
          <SelectItem value="" text="All methods" />
          <SelectItem value="SCRAP" text="Scrap" />
          <SelectItem value="AUCTION" text="Auction" />
          <SelectItem value="DONATION" text="Donation" />
          <SelectItem value="DESTROY" text="Destroy" />
        </Select>
        <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setDateFrom(date ? date.toISOString() : undefined)}>
          <DatePickerInput id="disposal-report-date-from" labelText="Requested from" placeholder="yyyy-mm-dd" />
        </DatePicker>
        <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setDateTo(date ? date.toISOString() : undefined)}>
          <DatePickerInput
            id="disposal-report-date-to"
            labelText="Requested to"
            placeholder="yyyy-mm-dd"
            invalid={dateRangeInvalid}
            invalidText="Must be on or after the “Requested from” date."
          />
        </DatePicker>
        <Button
          kind="ghost"
          size="md"
          onClick={() => {
            setStatus("");
            setMethod("");
            setDateFrom(undefined);
            setDateTo(undefined);
          }}
        >
          Clear filters
        </Button>
      </div>

      <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: 0, gridTemplateColumns: "repeat(3, 1fr)" }}>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Disposals in scope</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{records.length.toLocaleString()}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Total proceeds</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{formatCurrency(totalProceeds)}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Average approval time</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{averageApprovalDays.toFixed(1)} days</p>
        </div>
      </div>

      <div className="cg-toolbar" style={{ justifyContent: "flex-end", marginTop: "1rem" }}>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button kind="tertiary" size="sm" renderIcon={DocumentPdf} onClick={() => printPdf(records, totalProceeds)}>
            Export PDF
          </Button>
          <Button kind="tertiary" size="sm" renderIcon={DocumentExport} onClick={() => downloadCsv(records)}>
            Export CSV
          </Button>
        </div>
      </div>

      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr><th>Method</th><th>Disposals</th><th>Proceeds</th></tr>
        </thead>
        <tbody>
          {byMethod.map(([disposalMethod, summary]) => (
            <tr key={disposalMethod}>
              <td>{formatStatusLabel(disposalMethod)}</td>
              <td className="cg-table__muted">{summary.count}</td>
              <td className="cg-table__muted">{formatCurrency(summary.proceeds)}</td>
            </tr>
          ))}
          {byMethod.length === 0 && <tr><td colSpan={3} className="cg-table__muted">No disposal requests found.</td></tr>}
        </tbody>
      </table>

      <div style={{ marginTop: "2rem" }}>
        <div className="cg-section__header">
          <div>
            <h2 className="cg-section__title">Disposal details</h2>
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
                <th>Method</th>
                <th>Status</th>
                <th>Residual value</th>
                <th>Requested</th>
                <th>Disposed</th>
              </tr>
            </thead>
            <tbody>
              {detailPage.items.map((d) => (
                <tr key={d.id}>
                  <td>
                    <span className="cg-table__mono">{d.asset_code}</span>
                    <br />
                    <span className="cg-table__muted">{d.asset_name}</span>
                  </td>
                  <td className="cg-table__muted">{formatStatusLabel(d.disposal_method)}</td>
                  <td><Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag></td>
                  <td className="cg-table__muted">{formatCurrency(d.estimated_residual_value)}</td>
                  <td className="cg-table__muted">{new Date(d.requested_at).toLocaleDateString()}</td>
                  <td className="cg-table__muted">{d.disposed_at ? new Date(d.disposed_at).toLocaleDateString() : "—"}</td>
                </tr>
              ))}
              {detailPage.items.length === 0 && <tr><td colSpan={6} className="cg-table__muted">No disposal requests found.</td></tr>}
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
