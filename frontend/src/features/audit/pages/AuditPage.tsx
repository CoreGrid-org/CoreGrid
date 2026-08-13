import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button } from "@carbon/react";
import { Add } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { MOCK_CAMPAIGNS, MOCK_DISCREPANCIES, MOCK_AUDIT_LOG } from "../data/mockAudit";

export default function AuditPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Audit & Compliance</h1>
          <p className="cg-page__subtitle">
            Verification campaigns, discrepancies and the audit log (FR-056 to FR-066).
          </p>
        </div>
        <Button renderIcon={Add}>New campaign</Button>
      </div>

      <Tabs>
        <TabList aria-label="Audit sections">
          <Tab>Verification Campaigns</Tab>
          <Tab>Discrepancies</Tab>
          <Tab>Audit Log</Tab>
        </TabList>
        <TabPanels>
          {/* ── Campaigns ───────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-056", "FR-057", "FR-066"]}>
              An Auditor scopes a campaign by department, location, category or asset type; the system
              generates the task list and assigns it to the officers responsible for the in-scope locations,
              then reports progress on this tab in real time as tasks are completed.
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Campaign</th>
                    <th>Period</th>
                    <th>Scope</th>
                    <th>Status</th>
                    <th>Progress</th>
                    <th>Discrepancies</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_CAMPAIGNS.map((c, i) => (
                    <tr key={i}>
                      <td>{c.name}</td>
                      <td className="cg-table__muted">{c.period}</td>
                      <td className="cg-table__muted">{c.scope}</td>
                      <td>
                        <Tag type={statusTagColor(c.status)}>{formatStatusLabel(c.status)}</Tag>
                      </td>
                      <td className="cg-table__muted">
                        {c.verified} / {c.total} verified
                      </td>
                      <td>
                        <Tag type={c.discrepancies > 0 ? "magenta" : "gray"}>{c.discrepancies}</Tag>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Discrepancies ───────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-060", "FR-061", "FR-062"]}>
              Raised automatically when a verification assertion diverges from the register, or manually by
              an officer; an Auditor resolves each with a typed resolution — Register Corrected, Asset
              Relocated, Condition Updated, Written Off or No Action — applying the correction where the
              resolution requires it.
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Asset</th>
                    <th>Classification</th>
                    <th>Status</th>
                    <th>Raised by</th>
                    <th>Date</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_DISCREPANCIES.map((d, i) => (
                    <tr key={i}>
                      <td className="cg-table__mono">{d.assetCode}</td>
                      <td>
                        <Tag type={statusTagColor(d.classification)}>{formatStatusLabel(d.classification)}</Tag>
                      </td>
                      <td>
                        <Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag>
                      </td>
                      <td className="cg-table__muted">{d.raisedBy}</td>
                      <td className="cg-table__muted">{d.date}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Audit log ───────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-063", "FR-064"]}>
              Every state-changing operation writes an immutable entry — actor, organisation, entity,
              operation, before/after values, timestamp and correlation id. No API path may update or delete
              a row here (DR-12); the database itself revokes UPDATE/DELETE on this table for the application
              role (doc/SRS/system.md §F.7).
            </MockNotice>

            <div className="cg-section">
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Timestamp</th>
                    <th>Actor</th>
                    <th>Entity</th>
                    <th>Operation</th>
                    <th>Correlation ID</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_AUDIT_LOG.map((e, i) => (
                    <tr key={i}>
                      <td className="cg-table__mono">{e.timestamp}</td>
                      <td>{e.actor}</td>
                      <td className="cg-table__muted">{e.entity}</td>
                      <td>
                        <Tag type="blue">{e.operation}</Tag>
                      </td>
                      <td className="cg-table__mono">{e.correlationId}</td>
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
