import { useState } from "react";
import { ComboBox, InlineNotification, Pagination, Select, SelectItem, Tag } from "@carbon/react";
import { useAssetCategories, useDepartments } from "@/features/assets/hooks/useAssets";
import type { AssetCategory, Department } from "@/features/assets/types/asset";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { formatDate, hasDateRangeErrors, validateDateRange, type DateRange } from "@/shared/lib/dates";
import DateRangeFilter from "@/shared/components/DateRangeFilter";
import { useAuditReport, useExportAuditReport } from "../hooks/useAuditReport";
import { exportBlockReason } from "../lib/export";
import { ReportExportBar, ReportFilters, ReportStats } from "./ReportParts";

// Organisation-wide audit report. Unlike the other report panels this one
// is aggregated, paged and exported server-side (GET /api/audit-report and
// its /export), so it only sends the filters.
export default function AuditReportPanel() {
  const [range, setRange] = useState<DateRange>({});
  const [department, setDepartment] = useState<Department | null>(null);
  const [category, setCategory] = useState<AssetCategory | null>(null);
  const [status, setStatus] = useState("");
  const [paging, setPaging] = useState({ page: 1, pageSize: 25 });

  const { data: departments } = useDepartments();
  const { data: categories } = useAssetCategories();

  const rangeErrors = validateDateRange(range);
  const rangeValid = !hasDateRangeErrors(rangeErrors);

  const filters = {
    from: range.from,
    to: range.to,
    departmentId: department?.id,
    categoryId: category?.id,
    status: status || undefined,
  };
  const report = useAuditReport({ ...filters, ...paging }, rangeValid);
  const exportReport = useExportAuditReport();

  // Any filter change goes back to the first page.
  const applyFilter = (apply: () => void) => {
    apply();
    setPaging((p) => ({ ...p, page: 1 }));
  };

  const activeCount = [range.from, range.to, department, category, status].filter(Boolean).length;
  const data = report.data;
  const byClassification = data?.by_classification ?? [];
  const discrepancies = data?.discrepancies ?? [];
  const totalCount = data?.discrepancies_total_count ?? discrepancies.length;

  return (
    <div className="cg-report">
      <p className="cg-report__intro">
        Every campaign and discrepancy in your organisation: how many assets were in scope and verified, and
        discrepancies by classification and resolution status. Exports cover exactly what's filtered here.
      </p>

      <ReportFilters
        activeCount={activeCount}
        onClear={() =>
          applyFilter(() => {
            setRange({});
            setDepartment(null);
            setCategory(null);
            setStatus("");
          })
        }
        isRefreshing={report.isLoading && Boolean(data)}
      >
        <ComboBox<Department>
          id="audit-report-department"
          titleText="Department"
          placeholder="All departments"
          items={departments ?? []}
          itemToString={(d) => d?.name ?? ""}
          selectedItem={department}
          shouldFilterItem={comboBoxFilter(department)}
          onChange={({ selectedItem }) => applyFilter(() => setDepartment(selectedItem ?? null))}
        />
        <ComboBox<AssetCategory>
          id="audit-report-category"
          titleText="Category"
          placeholder="All categories"
          items={categories ?? []}
          itemToString={(c) => c?.name ?? ""}
          selectedItem={category}
          shouldFilterItem={comboBoxFilter(category)}
          onChange={({ selectedItem }) => applyFilter(() => setCategory(selectedItem ?? null))}
        />
        <Select
          id="audit-report-status"
          labelText="Discrepancy status"
          value={status}
          onChange={(e) => applyFilter(() => setStatus(e.target.value))}
        >
          <SelectItem value="" text="All statuses" />
          <SelectItem value="Open" text="Open" />
          <SelectItem value="Resolved" text="Resolved" />
        </Select>
        <DateRangeFilter idPrefix="audit-report" value={range} onChange={(r) => applyFilter(() => setRange(r))} errors={rangeErrors} />
      </ReportFilters>

      {report.isError && (
        <InlineNotification
          kind="error"
          title="Could not load the report"
          subtitle={getErrorMessage(report.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ maxWidth: "100%" }}
        />
      )}
      {exportReport.isError && (
        <InlineNotification
          kind="error"
          title="Could not export the report"
          subtitle={getErrorMessage(exportReport.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ maxWidth: "100%" }}
        />
      )}

      {report.isLoading && !data ? (
        <div className="cg-section cg-placeholder">
          <p>Loading…</p>
        </div>
      ) : data ? (
        <div className={report.isLoading || !rangeValid ? "cg-report__results is-stale" : "cg-report__results"}>
          <ReportStats
            stats={[
              { label: "Campaigns in period", value: data.campaigns_in_period },
              { label: "Assets in scope", value: data.assets_in_scope },
              { label: "Verified", value: data.assets_verified },
              { label: "Open discrepancies", value: data.open_discrepancies },
            ]}
          />

          <ReportExportBar
            count={totalCount}
            noun="discrepancies"
            isExporting={exportReport.isPending}
            onPdf={() => exportReport.mutate({ query: filters, format: "pdf" })}
            onCsv={() => exportReport.mutate({ query: filters, format: "csv" })}
            // The export also includes the campaign totals, so an empty
            // discrepancy list is still worth exporting.
            disabledReason={exportBlockReason(rangeValid, report.isLoading, 1)}
          />

          <section className="cg-section cg-report__table">
            <header className="cg-section__header">
              <p className="cg-section__title">By classification</p>
            </header>
            <table className="cg-table cg-table--no-hover">
              <thead>
                <tr>
                  <th>Classification</th>
                  <th>Raised</th>
                  <th>Resolved</th>
                </tr>
              </thead>
              <tbody>
                {byClassification.map((row) => (
                  <tr key={row.classification}>
                    <td>{row.classification}</td>
                    <td className="cg-table__muted">{row.raised}</td>
                    <td className="cg-table__muted">{row.resolved}</td>
                  </tr>
                ))}
                {byClassification.length === 0 && (
                  <tr>
                    <td colSpan={3} className="cg-table__muted">No discrepancies match these filters.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </section>

          <section className="cg-section cg-report__table">
            <header className="cg-section__header">
              <div>
                <p className="cg-section__title">Discrepancies</p>
                <p className="cg-report__table-subtitle">
                  Showing {discrepancies.length.toLocaleString()} of {totalCount.toLocaleString()} filtered discrepancies
                </p>
              </div>
            </header>
            <div style={{ overflowX: "auto" }}>
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Asset</th>
                    <th>Department</th>
                    <th>Classification</th>
                    <th>Status</th>
                    <th>Raised</th>
                    <th>Resolved</th>
                  </tr>
                </thead>
                <tbody>
                  {discrepancies.map((row, i) => (
                    <tr key={`${row.asset_code}-${row.raised_at}-${i}`}>
                      <td className="cg-table__mono">{row.asset_code}</td>
                      <td className="cg-table__muted">{row.department_name}</td>
                      <td>{row.classification}</td>
                      <td>
                        <Tag type={row.status === "Open" ? "red" : "green"}>{row.status}</Tag>
                      </td>
                      <td className="cg-table__muted">{formatDate(row.raised_at)}</td>
                      <td className="cg-table__muted">{formatDate(row.resolved_at)}</td>
                    </tr>
                  ))}
                  {discrepancies.length === 0 && (
                    <tr>
                      <td colSpan={6} className="cg-table__muted">No discrepancies match these filters.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            {totalCount > 0 && (
              <Pagination
                page={paging.page}
                pageSize={paging.pageSize}
                pageSizes={[10, 25, 50, 100]}
                totalItems={totalCount}
                onChange={({ page, pageSize }) => setPaging({ page, pageSize })}
              />
            )}
          </section>
        </div>
      ) : null}
    </div>
  );
}
