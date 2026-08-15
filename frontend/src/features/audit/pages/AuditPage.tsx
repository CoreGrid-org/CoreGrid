import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, Dropdown, DatePicker, DatePickerInput, Pagination, InlineNotification } from "@carbon/react";
import { Add } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { MOCK_CAMPAIGNS, MOCK_DISCREPANCIES } from "../data/mockAudit";
import { useAuditLog } from "../hooks/useAuditLog";
import { getErrorMessage } from "@/shared/lib/errorMessage";

const ENTITY_TYPES = [
  "Asset",
  "AssetTransfer",
  "DisposalRequest",
  "Department",
  "Location",
  "OrganizationPolicy",
  "User",
  "VerificationCampaign",
  "VerificationTask",
  "Discrepancy",
];
const OPERATIONS = ["Create", "Update", "Delete"];
const OPERATION_TAG: Record<string, "green" | "blue" | "red"> = { Create: "green", Update: "blue", Delete: "red" };

export default function AuditPage() {
  const [entityType, setEntityType] = useState<string | undefined>();
  const [operation, setOperation] = useState<string | undefined>();
  const [from, setFrom] = useState<string | undefined>();
  const [to, setTo] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const auditLog = useAuditLog({ entityType, operation, from, to, page, pageSize });

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
            <p className="cg-table__muted" style={{ margin: "0 0 1rem", fontSize: "0.8125rem" }}>
              Every state-changing operation writes an immutable entry — actor, entity, operation, before/after
              values, timestamp and correlation id. Not editable or deletable through any API (FR-063, FR-064).
            </p>

            <div className="cg-section" style={{ marginBottom: "1rem" }}>
              <div
                style={{
                  display: "flex",
                  flexWrap: "wrap",
                  gap: "1rem",
                  padding: "1rem 1.5rem",
                  alignItems: "flex-end",
                }}
              >
                <Dropdown
                  id="audit-entity-type"
                  titleText="Entity"
                  label="All entities"
                  items={["", ...ENTITY_TYPES]}
                  itemToString={(item) => (item ? item : "All entities")}
                  selectedItem={entityType ?? ""}
                  onChange={({ selectedItem }) => {
                    setEntityType(selectedItem || undefined);
                    setPage(1);
                  }}
                  style={{ minWidth: "12rem" }}
                />
                <Dropdown
                  id="audit-operation"
                  titleText="Operation"
                  label="All operations"
                  items={["", ...OPERATIONS]}
                  itemToString={(item) => (item ? item : "All operations")}
                  selectedItem={operation ?? ""}
                  onChange={({ selectedItem }) => {
                    setOperation(selectedItem || undefined);
                    setPage(1);
                  }}
                  style={{ minWidth: "10rem" }}
                />
                <DatePicker
                  datePickerType="single"
                  dateFormat="Y-m-d"
                  onChange={([date]) => {
                    setFrom(date ? date.toISOString() : undefined);
                    setPage(1);
                  }}
                >
                  <DatePickerInput id="audit-from" labelText="From" placeholder="yyyy-mm-dd" />
                </DatePicker>
                <DatePicker
                  datePickerType="single"
                  dateFormat="Y-m-d"
                  onChange={([date]) => {
                    setTo(date ? date.toISOString() : undefined);
                    setPage(1);
                  }}
                >
                  <DatePickerInput id="audit-to" labelText="To" placeholder="yyyy-mm-dd" />
                </DatePicker>
              </div>
            </div>

            {auditLog.isError && (
              <InlineNotification
                kind="error"
                title="Could not load the audit log"
                subtitle={getErrorMessage(auditLog.error, "Something went wrong. Please try again.")}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}

            <div className="cg-section">
              {auditLog.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading the audit log…</p>
                </div>
              ) : auditLog.data && auditLog.data.items.length > 0 ? (
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
                    {auditLog.data.items.map((e) => (
                      <tr key={e.id}>
                        <td className="cg-table__mono">{new Date(e.created_at).toLocaleString()}</td>
                        <td>{e.actor_email ?? "System"}</td>
                        <td className="cg-table__muted">
                          {e.entity_type}
                          {e.entity_id ? ` (${e.entity_id.slice(0, 8)})` : ""}
                        </td>
                        <td>
                          <Tag type={OPERATION_TAG[e.operation] ?? "gray"}>{e.operation}</Tag>
                        </td>
                        <td className="cg-table__mono">{e.correlation_id.slice(0, 8)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No audit log entries match these filters.</p>
                </div>
              )}
            </div>

            {auditLog.data && auditLog.data.total_count > 0 && (
              <Pagination
                page={page}
                pageSize={pageSize}
                pageSizes={[10, 25, 50, 100]}
                totalItems={auditLog.data.total_count}
                onChange={({ page: nextPage, pageSize: nextPageSize }) => {
                  setPage(nextPage);
                  setPageSize(nextPageSize);
                }}
                style={{ marginTop: "1rem" }}
              />
            )}
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
