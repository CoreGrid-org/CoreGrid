import { useState } from "react";
import { Button, Dropdown, DatePicker, DatePickerInput, InlineNotification } from "@carbon/react";
import { DocumentPdf, DocumentExport } from "@carbon/icons-react";
import { useAuditReport, useExportAuditReport } from "../hooks/useAuditReport";
import { useDepartments, useAssetCategories } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";

const STATUS_FILTERS = ["All statuses", "Open", "Resolved"];

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

// FR-065/FR-084/FR-085/FR-086 — the one report tab on this page that's
// Component D's: an aggregate across every campaign and discrepancy in the
// caller's organisation, filterable by date/department/category/status, and
// exportable as exactly what's on screen. The other three tabs on this page
// (inventory/maintenance/disposal) belong to Components A/B/C.
export default function AuditReportPanel() {
  const [from, setFrom] = useState<string>();
  const [to, setTo] = useState<string>();
  const [departmentId, setDepartmentId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [status, setStatus] = useState(STATUS_FILTERS[0]);

  const query = {
    from,
    to,
    departmentId: departmentId || undefined,
    categoryId: categoryId || undefined,
    status: status === "All statuses" ? undefined : status,
  };

  const report = useAuditReport(query);
  const exportReport = useExportAuditReport();
  const { data: departments } = useDepartments();
  const { data: categories } = useAssetCategories();

  return (
    <>
      <p className="cg-table__muted" style={{ margin: "0 0 1rem", fontSize: "0.8125rem" }}>
        Every campaign and discrepancy in your organisation, aggregated across the filters below — how many assets
        were in scope and verified, and discrepancies broken down by classification and resolution status (FR-065).
        Export reflects exactly what's filtered on screen (FR-084, FR-085), restricted to your organisation
        (FR-086).
      </p>

      <div className="cg-section" style={{ marginBottom: "1rem" }}>
        <div style={{ display: "flex", flexWrap: "wrap", gap: "1rem", padding: "1rem 1.5rem", alignItems: "flex-end" }}>
          <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setFrom(date ? toDateOnly(date) : undefined)}>
            <DatePickerInput id="audit-report-from" labelText="From" placeholder="yyyy-mm-dd" />
          </DatePicker>
          <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setTo(date ? toDateOnly(date) : undefined)}>
            <DatePickerInput id="audit-report-to" labelText="To" placeholder="yyyy-mm-dd" />
          </DatePicker>
          <Dropdown
            id="audit-report-department"
            titleText="Department"
            label="All departments"
            items={["", ...(departments?.map((d) => d.id) ?? [])]}
            itemToString={(id) => (id ? departments?.find((d) => d.id === id)?.name ?? id : "All departments")}
            selectedItem={departmentId}
            onChange={({ selectedItem }) => setDepartmentId(selectedItem || "")}
            style={{ minWidth: "12rem" }}
          />
          <Dropdown
            id="audit-report-category"
            titleText="Category"
            label="All categories"
            items={["", ...(categories?.map((c) => c.id) ?? [])]}
            itemToString={(id) => (id ? categories?.find((c) => c.id === id)?.name ?? id : "All categories")}
            selectedItem={categoryId}
            onChange={({ selectedItem }) => setCategoryId(selectedItem || "")}
            style={{ minWidth: "12rem" }}
          />
          <Dropdown
            id="audit-report-status"
            titleText="Discrepancy status"
            label={status}
            items={STATUS_FILTERS}
            selectedItem={status}
            onChange={({ selectedItem }) => setStatus(selectedItem ?? STATUS_FILTERS[0])}
            style={{ minWidth: "10rem" }}
          />
        </div>
      </div>

      {report.isError && (
        <InlineNotification
          kind="error"
          title="Could not load the report"
          subtitle={getErrorMessage(report.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      {exportReport.isError && (
        <InlineNotification
          kind="error"
          title="Could not export the report"
          subtitle={getErrorMessage(exportReport.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div className="cg-section">
        <div className="cg-toolbar" style={{ justifyContent: "flex-end" }}>
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <Button
              kind="tertiary"
              size="sm"
              renderIcon={DocumentPdf}
              disabled={exportReport.isPending}
              onClick={() => exportReport.mutate({ query, format: "pdf" })}
            >
              Export PDF
            </Button>
            <Button
              kind="tertiary"
              size="sm"
              renderIcon={DocumentExport}
              disabled={exportReport.isPending}
              onClick={() => exportReport.mutate({ query, format: "csv" })}
            >
              Export CSV
            </Button>
          </div>
        </div>

        {report.isLoading ? (
          <div className="cg-placeholder">
            <p>Loading…</p>
          </div>
        ) : report.data ? (
          <>
            <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: 0, gridTemplateColumns: "repeat(4, 1fr)" }}>
              <div className="cg-stat-card">
                <p className="cg-stat-card__label">Campaigns in period</p>
                <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                  {report.data.campaigns_in_period}
                </p>
              </div>
              <div className="cg-stat-card">
                <p className="cg-stat-card__label">Assets in scope</p>
                <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                  {report.data.assets_in_scope}
                </p>
              </div>
              <div className="cg-stat-card">
                <p className="cg-stat-card__label">Verified</p>
                <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                  {report.data.assets_verified}
                </p>
              </div>
              <div className="cg-stat-card">
                <p className="cg-stat-card__label">Open discrepancies</p>
                <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                  {report.data.open_discrepancies}
                </p>
              </div>
            </div>

            <table className="cg-table cg-table--no-hover">
              <thead>
                <tr>
                  <th>Classification</th>
                  <th>Raised</th>
                  <th>Resolved</th>
                </tr>
              </thead>
              <tbody>
                {report.data.by_classification.length > 0 ? (
                  report.data.by_classification.map((row) => (
                    <tr key={row.classification}>
                      <td>{row.classification}</td>
                      <td className="cg-table__muted">{row.raised}</td>
                      <td className="cg-table__muted">{row.resolved}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={3} className="cg-table__muted">
                      No discrepancies match these filters.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </>
        ) : null}
      </div>
    </>
  );
}
