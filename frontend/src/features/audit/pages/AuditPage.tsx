import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, Dropdown, DatePicker, DatePickerInput, Pagination, InlineNotification } from "@carbon/react";
import { Add } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { useAuditLog } from "../hooks/useAuditLog";
import { useCampaignsList } from "../hooks/useCampaigns";
import { useDiscrepanciesList } from "../hooks/useDiscrepancies";
import CreateCampaignModal from "../components/CreateCampaignModal";
import ResolveDiscrepancyModal from "../components/ResolveDiscrepancyModal";
import CampaignReportModal from "../components/CampaignReportModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Discrepancy } from "../api/discrepancies";

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
const DISCREPANCY_STATUS_FILTERS = ["Open only", "All"];

function campaignScopeLabel(c: {
  scope_department_name: string | null;
  scope_location_name: string | null;
  scope_asset_category_name: string | null;
  scope_asset_type_name: string | null;
}): string {
  const parts = [c.scope_department_name, c.scope_location_name, c.scope_asset_category_name, c.scope_asset_type_name].filter(
    (p): p is string => !!p,
  );
  return parts.length > 0 ? parts.join(" · ") : "Whole register";
}

export default function AuditPage() {
  const [entityType, setEntityType] = useState<string | undefined>();
  const [operation, setOperation] = useState<string | undefined>();
  const [from, setFrom] = useState<string | undefined>();
  const [to, setTo] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const auditLog = useAuditLog({ entityType, operation, from, to, page, pageSize });

  const [showCreateCampaign, setShowCreateCampaign] = useState(false);
  const [reportCampaign, setReportCampaign] = useState<{ id: string; name: string } | null>(null);
  const campaigns = useCampaignsList();

  const [discrepancyCampaignId, setDiscrepancyCampaignId] = useState<string | undefined>();
  const [discrepancyStatusFilter, setDiscrepancyStatusFilter] = useState(DISCREPANCY_STATUS_FILTERS[0]);
  const discrepancies = useDiscrepanciesList({
    campaignId: discrepancyCampaignId,
    onlyOpen: discrepancyStatusFilter === "Open only",
  });
  const [resolvingDiscrepancy, setResolvingDiscrepancy] = useState<Discrepancy | null>(null);

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Audit & Compliance</h1>
          <p className="cg-page__subtitle">
            Verification campaigns, discrepancies and the audit log (FR-056 to FR-066).
          </p>
        </div>
        <Button renderIcon={Add} onClick={() => setShowCreateCampaign(true)}>
          New campaign
        </Button>
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
            <p className="cg-table__muted" style={{ margin: "0 0 1rem", fontSize: "0.8125rem" }}>
              A campaign is a scoped, time-boxed physical verification: it generates one task per in-scope asset,
              assigns each to the responsible officer, and tracks completion and discrepancies as officers scan and
              confirm assets against the register (FR-056, FR-057). Once a campaign has run, open its report to see
              how it went — verified vs. outstanding, discrepancies by type and resolution — and export it as a PDF
              or CSV record (FR-065).
            </p>

            {campaigns.isError && (
              <InlineNotification
                kind="error"
                title="Could not load verification campaigns"
                subtitle={getErrorMessage(campaigns.error, "Something went wrong. Please try again.")}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}

            <div className="cg-section">
              {campaigns.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading campaigns…</p>
                </div>
              ) : campaigns.data && campaigns.data.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Campaign</th>
                      <th>Period</th>
                      <th>Scope</th>
                      <th>Status</th>
                      <th>Progress</th>
                      <th>Discrepancies</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {campaigns.data.map((c) => (
                      <tr key={c.id}>
                        <td>{c.name}</td>
                        <td className="cg-table__muted">
                          {c.period_start} – {c.period_end}
                        </td>
                        <td className="cg-table__muted">{campaignScopeLabel(c)}</td>
                        <td>
                          <Tag type={statusTagColor(c.status)}>{formatStatusLabel(c.status)}</Tag>
                        </td>
                        <td className="cg-table__muted">
                          {c.completed_task_count} / {c.task_count} verified
                        </td>
                        <td>
                          <Tag type={c.open_discrepancy_count > 0 ? "magenta" : "gray"}>{c.open_discrepancy_count}</Tag>
                        </td>
                        <td>
                          <Button kind="ghost" size="sm" onClick={() => setReportCampaign({ id: c.id, name: c.name })}>
                            View report
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No verification campaigns yet.</p>
                </div>
              )}
            </div>
          </TabPanel>

          {/* ── Discrepancies ───────────────────────────────────────────── */}
          <TabPanel>
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
                  id="discrepancy-campaign"
                  titleText="Campaign"
                  label="All campaigns"
                  items={["", ...(campaigns.data?.map((c) => c.id) ?? [])]}
                  itemToString={(id) => (id ? campaigns.data?.find((c) => c.id === id)?.name ?? id : "All campaigns")}
                  selectedItem={discrepancyCampaignId ?? ""}
                  onChange={({ selectedItem }) => setDiscrepancyCampaignId(selectedItem || undefined)}
                  style={{ minWidth: "16rem" }}
                />
                <Dropdown
                  id="discrepancy-status"
                  titleText="Status"
                  label={discrepancyStatusFilter}
                  items={DISCREPANCY_STATUS_FILTERS}
                  selectedItem={discrepancyStatusFilter}
                  onChange={({ selectedItem }) => setDiscrepancyStatusFilter(selectedItem ?? DISCREPANCY_STATUS_FILTERS[0])}
                  style={{ minWidth: "10rem" }}
                />
              </div>
            </div>

            {discrepancies.isError && (
              <InlineNotification
                kind="error"
                title="Could not load discrepancies"
                subtitle={getErrorMessage(discrepancies.error, "Something went wrong. Please try again.")}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}

            <div className="cg-section">
              {discrepancies.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading discrepancies…</p>
                </div>
              ) : discrepancies.data && discrepancies.data.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Asset</th>
                      <th>Classification</th>
                      <th>Status</th>
                      <th>Raised by</th>
                      <th>Date</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {discrepancies.data.map((d) => (
                      <tr key={d.id}>
                        <td className="cg-table__mono">{d.asset_code}</td>
                        <td>
                          <Tag type={statusTagColor(d.type)}>{formatStatusLabel(d.type)}</Tag>
                        </td>
                        <td>
                          <Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag>
                        </td>
                        <td className="cg-table__muted">{d.is_automatic ? "System (auto)" : d.raised_by_email ?? "—"}</td>
                        <td className="cg-table__muted">{new Date(d.created_at).toLocaleDateString()}</td>
                        <td>
                          {d.status === "Open" && (
                            <Button kind="ghost" size="sm" onClick={() => setResolvingDiscrepancy(d)}>
                              Resolve
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No discrepancies match these filters.</p>
                </div>
              )}
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

      {showCreateCampaign && (
        <CreateCampaignModal
          onClose={() => setShowCreateCampaign(false)}
          onCreated={() => {
            setShowCreateCampaign(false);
            campaigns.refetch();
          }}
        />
      )}

      {resolvingDiscrepancy && (
        <ResolveDiscrepancyModal
          discrepancy={resolvingDiscrepancy}
          onClose={() => setResolvingDiscrepancy(null)}
          onResolved={() => {
            setResolvingDiscrepancy(null);
            discrepancies.refetch();
            campaigns.refetch();
          }}
        />
      )}

      {reportCampaign && (
        <CampaignReportModal
          campaignId={reportCampaign.id}
          campaignName={reportCampaign.name}
          onClose={() => setReportCampaign(null)}
        />
      )}
    </div>
  );
}
