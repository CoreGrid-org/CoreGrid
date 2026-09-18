import { Tabs, TabList, Tab, TabPanels, TabPanel, Button } from "@carbon/react";
import { DocumentPdf, DocumentExport } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { useMe } from "@/features/auth/hooks/useMe";
import { MOCK_REPORTS } from "../data/mockReports";
import AuditReportPanel from "../components/AuditReportPanel";
import InventoryReportPanel from "../components/InventoryReportPanel";
import MaintenanceReportPanel from "../components/MaintenanceReportPanel";

// FR-084/FR-085: the backend's own AuditReportController is
// Auditor/Administrator-only (audit is a compliance function, not an
// inventory-operations one) — this page is shared across all three roles
// (App.tsx mounts it at /admin/reports, /inventory/reports, /audit/reports),
// so it has to match that gate itself rather than showing an Inventory
// Officer a tab that just 403s. Inventory/Maintenance/Disposal stay visible
// to all three roles — their own backend read endpoints already are.
export default function ReportsPage() {
  const { data: me } = useMe();
  const canSeeAudit = me?.role === "Auditor" || me?.role === "Administrator";

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Reports</h1>
          <p className="cg-page__subtitle">
            {canSeeAudit
              ? "Inventory, maintenance, disposal and audit reports."
              : "Inventory, maintenance and disposal reports."}
          </p>
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Report sections">
          {MOCK_REPORTS.map((r) => (
            <Tab key={r.key}>{r.title.replace(" Report", "")}</Tab>
          ))}
          {canSeeAudit && <Tab>Audit</Tab>}
        </TabList>
        <TabPanels>
          {MOCK_REPORTS.map((report) => (
            <TabPanel key={report.key}>
              {report.key === "inventory" ? (
                <InventoryReportPanel />
              ) : report.key === "maintenance" ? (
                <MaintenanceReportPanel />
              ) : (
                <>
              <MockNotice>
                {`${report.description} Real exports reflect exactly the filters applied on screen and are restricted to the departments the caller's role permits them to see.`}
              </MockNotice>

              <div className="cg-section">
                <div className="cg-toolbar" style={{ justifyContent: "space-between" }}>
                  <span className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
                    Filters: department, category, status, condition, date range
                  </span>
                  <div style={{ display: "flex", gap: "0.5rem" }}>
                    <Button kind="tertiary" size="sm" renderIcon={DocumentPdf}>
                      Export PDF
                    </Button>
                    <Button kind="tertiary" size="sm" renderIcon={DocumentExport}>
                      Export CSV
                    </Button>
                  </div>
                </div>

                <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: 0, gridTemplateColumns: `repeat(${report.stats.length}, 1fr)` }}>
                  {report.stats.map((s) => (
                    <div className="cg-stat-card" key={s.label}>
                      <p className="cg-stat-card__label">{s.label}</p>
                      <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                        {s.value}
                      </p>
                    </div>
                  ))}
                </div>

                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      {report.columns.map((c) => (
                        <th key={c}>{c}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {report.rows.map((row, i) => (
                      <tr key={i}>
                        {report.columns.map((c) => (
                          <td key={c} className={c === report.columns[0] ? "" : "cg-table__muted"}>
                            {row[c]}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
                </>
              )}
            </TabPanel>
          ))}

          {canSeeAudit && (
            <TabPanel>
              <AuditReportPanel />
            </TabPanel>
          )}
        </TabPanels>
      </Tabs>
    </div>
  );
}
