import { Tabs, TabList, Tab, TabPanels, TabPanel, Button } from "@carbon/react";
import { DocumentPdf, DocumentExport } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { MOCK_REPORTS } from "../data/mockReports";

export default function ReportsPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Reports</h1>
          <p className="cg-page__subtitle">Inventory, maintenance, disposal and audit reports (FR-081 to FR-086).</p>
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Report sections">
          {MOCK_REPORTS.map((r) => (
            <Tab key={r.key}>{r.title.replace(" Report", "")}</Tab>
          ))}
        </TabList>
        <TabPanels>
          {MOCK_REPORTS.map((report) => (
            <TabPanel key={report.key}>
              <MockNotice requirements={[report.requirement, "FR-085", "FR-086"]}>
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
            </TabPanel>
          ))}
        </TabPanels>
      </Tabs>
    </div>
  );
}
