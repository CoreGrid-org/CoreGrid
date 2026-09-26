import { useMemo, useState } from "react";
import { Select, SelectItem, Tag } from "@carbon/react";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { formatDate, hasDateRangeErrors, isWithinDayRange, validateDateRange, type DateRange } from "@/shared/lib/dates";
import DateRangeFilter from "@/shared/components/DateRangeFilter";
import type { DisposalMethod, DisposalResponse, DisposalStatus } from "@/features/transfers/types";
import { getDisposalReportRecords } from "../api/disposalReport";
import { useReportData } from "../hooks/useReportData";
import { downloadCsv, downloadPdf, exportBlockReason, formatCurrency, type ExportColumn, type ReportExport } from "../lib/export";
import { ReportExportBar, ReportFilters, ReportStats, ReportStatus, ReportTable, type ReportColumn } from "./ReportParts";

const STATUSES: DisposalStatus[] = ["PENDING", "APPROVED", "REJECTED", "REVISION_REQUESTED", "DISPOSED"];
const METHODS: DisposalMethod[] = ["SCRAP", "AUCTION", "DONATION", "DESTROY"];
const DAY_MS = 24 * 60 * 60 * 1000;

const EXPORT_COLUMNS: ExportColumn<DisposalResponse>[] = [
  { header: "Asset code", width: 28, value: (d) => d.asset_code },
  { header: "Asset name", width: 40, value: (d) => d.asset_name },
  { header: "Method", width: 26, value: (d) => formatStatusLabel(d.disposal_method) },
  { header: "Status", width: 28, value: (d) => formatStatusLabel(d.status) },
  { header: "Residual value", width: 32, value: (d) => formatCurrency(d.estimated_residual_value) },
  { header: "Requested", width: 26, value: (d) => formatDate(d.requested_at) },
  { header: "Approved", width: 26, value: (d) => formatDate(d.approved_at) },
  { header: "Disposed", width: 26, value: (d) => formatDate(d.disposed_at) },
];

const DETAIL_COLUMNS: ReportColumn<DisposalResponse>[] = [
  {
    header: "Asset",
    render: (d) => (
      <>
        <span className="cg-table__mono">{d.asset_code}</span>
        <br />
        <span className="cg-table__muted">{d.asset_name}</span>
      </>
    ),
  },
  { header: "Method", muted: true, render: (d) => formatStatusLabel(d.disposal_method) },
  { header: "Status", render: (d) => <Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag> },
  { header: "Residual value", muted: true, render: (d) => formatCurrency(d.estimated_residual_value) },
  { header: "Requested", muted: true, render: (d) => formatDate(d.requested_at) },
  { header: "Disposed", muted: true, render: (d) => formatDate(d.disposed_at) },
];

type Breakdown = { key: DisposalMethod; count: number; proceeds: number };
const BREAKDOWN_COLUMNS: ReportColumn<Breakdown>[] = [
  { header: "Method", render: (b) => formatStatusLabel(b.key) },
  { header: "Disposals", muted: true, render: (b) => b.count },
  { header: "Proceeds", muted: true, render: (b) => formatCurrency(b.proceeds) },
];

// Status/method/requested-date filters over GET /api/disposals (scoped to
// the caller's role by the backend), with PDF/CSV export.
export default function DisposalReportPanel() {
  const [status, setStatus] = useState("");
  const [method, setMethod] = useState("");
  const [range, setRange] = useState<DateRange>({});

  const rangeErrors = validateDateRange(range);
  const rangeValid = !hasDateRangeErrors(rangeErrors);

  // The endpoint has no date filter, so only status/method go to the server;
  // the requested-date range is applied to the loaded records below, which
  // means changing dates never needs a new request.
  const query = { status: (status || undefined) as DisposalStatus | undefined, method: (method || undefined) as DisposalMethod | undefined };
  const queryKey = JSON.stringify(query);
  const report = useReportData(queryKey, true, (token) => getDisposalReportRecords(query, token));

  const records = useMemo(
    () => (report.data ?? []).filter((d) => !rangeValid || isWithinDayRange(d.requested_at, range.from, range.to)),
    [report.data, rangeValid, range.from, range.to],
  );

  const { totalProceeds, averageApprovalDays, byMethod } = useMemo(() => {
    const disposed = records.filter((d) => d.status === "DISPOSED");
    const durations = disposed
      .filter((d) => d.disposed_at)
      .map((d) => (new Date(d.disposed_at!).getTime() - new Date(d.requested_at).getTime()) / DAY_MS);
    const groups = new Map<DisposalMethod, Breakdown>();
    for (const d of records) {
      const current = groups.get(d.disposal_method) ?? { key: d.disposal_method, count: 0, proceeds: 0 };
      groups.set(d.disposal_method, {
        key: d.disposal_method,
        count: current.count + 1,
        proceeds: current.proceeds + (d.status === "DISPOSED" ? d.estimated_residual_value : 0),
      });
    }
    return {
      totalProceeds: disposed.reduce((sum, d) => sum + d.estimated_residual_value, 0),
      averageApprovalDays: durations.length > 0 ? durations.reduce((sum, days) => sum + days, 0) / durations.length : 0,
      byMethod: [...groups.values()].sort((a, b) => b.count - a.count),
    };
  }, [records]);

  const filterLabels = [
    status && `Status: ${formatStatusLabel(status)}`,
    method && `Method: ${formatStatusLabel(method)}`,
    range.from && `Requested from ${range.from}`,
    range.to && `Requested to ${range.to}`,
  ].filter((label): label is string => Boolean(label));

  const exportData: ReportExport<DisposalResponse> = {
    title: "Disposal Report",
    fileName: "disposal-report",
    columns: EXPORT_COLUMNS,
    rows: records,
    summary: [`Disposals in scope: ${records.length.toLocaleString()}`, `Total proceeds: ${formatCurrency(totalProceeds)}`],
    filters: filterLabels,
  };

  return (
    <div className="cg-report">
      <ReportFilters
        activeCount={filterLabels.length}
        onClear={() => {
          setStatus("");
          setMethod("");
          setRange({});
        }}
        isRefreshing={report.isRefreshing}
      >
        <Select id="disposal-report-status" labelText="Status" value={status} onChange={(e) => setStatus(e.target.value)}>
          <SelectItem value="" text="All statuses" />
          {STATUSES.map((s) => (
            <SelectItem key={s} value={s} text={formatStatusLabel(s)} />
          ))}
        </Select>
        <Select id="disposal-report-method" labelText="Method" value={method} onChange={(e) => setMethod(e.target.value)}>
          <SelectItem value="" text="All methods" />
          {METHODS.map((m) => (
            <SelectItem key={m} value={m} text={formatStatusLabel(m)} />
          ))}
        </Select>
        <DateRangeFilter
          idPrefix="disposal-report-date"
          fromLabel="Requested from"
          toLabel="Requested to"
          value={range}
          onChange={setRange}
          errors={rangeErrors}
        />
      </ReportFilters>

      <ReportStatus title="Could not load the disposal report" error={report.error} isInitialLoading={report.isInitialLoading} />

      {report.data && (
        <div className={report.isRefreshing || !rangeValid ? "cg-report__results is-stale" : "cg-report__results"}>
          <ReportStats
            stats={[
              { label: "Disposals in scope", value: records.length.toLocaleString() },
              { label: "Total proceeds", value: formatCurrency(totalProceeds) },
              { label: "Average approval time", value: `${averageApprovalDays.toFixed(1)} days` },
            ]}
          />
          <ReportExportBar
            count={records.length}
            noun="disposal requests"
            onPdf={() => downloadPdf(exportData)}
            onCsv={() => downloadCsv(exportData)}
            disabledReason={exportBlockReason(rangeValid, report.isRefreshing, records.length)}
          />
          <ReportTable
            title="By method"
            columns={BREAKDOWN_COLUMNS}
            rows={byMethod}
            rowKey={(b) => b.key}
            emptyText="No disposal requests match these filters."
            pageable={false}
          />
          <ReportTable
            key={`${queryKey}|${range.from}|${range.to}`}
            title="Disposal details"
            columns={DETAIL_COLUMNS}
            rows={records}
            rowKey={(d) => d.id}
            emptyText="No disposal requests match these filters."
          />
        </div>
      )}
    </div>
  );
}
