import { useMemo, useState } from "react";
import { Modal, Button, Tag, InlineNotification, ProgressBar, Tabs, TabList, Tab, TabPanels, TabPanel, Search, Select, SelectItem } from "@carbon/react";
import { DocumentPdf, DocumentExport } from "@carbon/icons-react";
import { useCampaignReport, useExportCampaignReport } from "../hooks/useCampaignReport";
import type { CampaignReport, CampaignReportTaskRow, VerificationOutcome } from "../api/campaignReport";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { formatDate } from "@/shared/lib/dates";
import PhotoThumbnail from "@/shared/components/PhotoThumbnail";

interface CampaignReportModalProps {
  campaignId: string;
  campaignName: string;
  onClose: () => void;
}

type TagType = "green" | "red" | "magenta" | "purple" | "gray";

const OUTCOMES: Record<VerificationOutcome, { label: string; tag: TagType }> = {
  Verified: { label: "Verified", tag: "green" },
  NotFound: { label: "Not found", tag: "red" },
  LocationMismatch: { label: "Location mismatch", tag: "magenta" },
  ConditionMismatch: { label: "Condition mismatch", tag: "purple" },
  LocationAndConditionMismatch: { label: "Location & condition", tag: "magenta" },
  Pending: { label: "Pending", tag: "gray" },
};

const lkr = (value: number) => `LKR ${value.toLocaleString("en-LK", { maximumFractionDigits: 0 })}`;
const humanize = (value: string | null) => (value ? formatStatusLabel(value) : "-");

// The campaign completion report: coverage, findings against the register,
// progress by department and verifier, the per-asset verification register
// and every discrepancy. The PDF/CSV exports carry the same content.
export default function CampaignReportModal({ campaignId, campaignName, onClose }: CampaignReportModalProps) {
  const report = useCampaignReport(campaignId);
  const exportReport = useExportCampaignReport();
  const data = report.data;

  return (
    <Modal
      open
      passiveModal
      size="lg"
      modalLabel="Audit & Compliance"
      modalHeading={`Campaign report: ${campaignName}`}
      onRequestClose={onClose}
      className="cg-campaign-report-modal"
    >
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

      {report.isLoading ? (
        <div className="cg-placeholder">
          <p>Loading the report…</p>
        </div>
      ) : data ? (
        <div className="cg-campaign-report">
          <header className="cg-campaign-report__header">
            <dl className="cg-campaign-report__meta">
              <div>
                <dt>Period</dt>
                <dd>
                  {data.period_start} to {data.period_end}
                </dd>
              </div>
              <div>
                <dt>Scope</dt>
                <dd>{data.scope}</dd>
              </div>
              <div>
                <dt>Status</dt>
                <dd>
                  <Tag type={statusTagColor(data.status)} size="sm">
                    {formatStatusLabel(data.status)}
                  </Tag>
                </dd>
              </div>
              <div>
                <dt>Created by</dt>
                <dd>
                  {data.created_by_name ?? "-"} · {formatDate(data.created_at)}
                </dd>
              </div>
            </dl>
            <div className="cg-campaign-report__export">
              <Button
                size="sm"
                renderIcon={DocumentPdf}
                disabled={exportReport.isPending}
                onClick={() => exportReport.mutate({ campaignId, format: "pdf" })}
              >
                {exportReport.isPending ? "Preparing…" : "Download PDF report"}
              </Button>
              <Button
                kind="tertiary"
                size="sm"
                renderIcon={DocumentExport}
                disabled={exportReport.isPending}
                onClick={() => exportReport.mutate({ campaignId, format: "csv" })}
              >
                CSV
              </Button>
            </div>
          </header>

          <Summary data={data} />

          <Tabs>
            <TabList aria-label="Report sections" contained>
              <Tab>Overview</Tab>
              <Tab>Assets ({data.tasks.length})</Tab>
              <Tab>Discrepancies ({data.discrepancies.length})</Tab>
            </TabList>
            <TabPanels>
              <TabPanel className="cg-campaign-report__panel">
                <Overview data={data} />
              </TabPanel>
              <TabPanel className="cg-campaign-report__panel">
                <AssetRegister tasks={data.tasks} />
              </TabPanel>
              <TabPanel className="cg-campaign-report__panel">
                <DiscrepancyRegister data={data} />
              </TabPanel>
            </TabPanels>
          </Tabs>

          <p className="cg-campaign-report__footnote">
            Generated {formatDate(data.generated_at)}
            {data.generated_by_name ? ` by ${data.generated_by_name}` : ""}. Recorded values reflect the register as of today.
          </p>
        </div>
      ) : null}
    </Modal>
  );
}

function Summary({ data }: { data: CampaignReport }) {
  return (
    <>
      <div className="cg-report__stats">
        <Stat label="Assets in scope" value={data.assets_in_scope} caption={lkr(data.value_in_scope)} />
        <Stat label="Verified" value={data.verified} caption={`${data.completion_percent}% complete`} tone="good" />
        <Stat
          label="Outstanding"
          value={data.outstanding}
          caption={data.overdue_tasks > 0 ? `${data.overdue_tasks} overdue` : "None overdue"}
          tone={data.overdue_tasks > 0 ? "warn" : undefined}
        />
        <Stat
          label="Open discrepancies"
          value={data.open_discrepancies}
          caption={`${data.resolved_discrepancies} resolved`}
          tone={data.open_discrepancies > 0 ? "bad" : "good"}
        />
      </div>

      <ProgressBar
        label={`Verification progress: ${data.verified} of ${data.assets_in_scope} assets`}
        value={data.completion_percent}
        max={100}
        size="small"
        helperText={`${data.completion_percent}% complete`}
        className="cg-campaign-report__progress"
      />

      <p className="cg-campaign-report__section-title">Findings</p>
      <div className="cg-report__stats">
        <Stat label="Found as recorded" value={data.found_as_recorded} tone="good" />
        <Stat
          label="Not found"
          value={data.not_found}
          caption={data.not_found > 0 ? `${lkr(data.value_not_found)} at risk` : undefined}
          tone={data.not_found > 0 ? "bad" : undefined}
        />
        <Stat label="Location mismatches" value={data.location_mismatches} tone={data.location_mismatches > 0 ? "warn" : undefined} />
        <Stat label="Condition mismatches" value={data.condition_mismatches} tone={data.condition_mismatches > 0 ? "warn" : undefined} />
      </div>
    </>
  );
}

function Stat({ label, value, caption, tone }: { label: string; value: number; caption?: string; tone?: "good" | "warn" | "bad" }) {
  return (
    <div className={`cg-report__stat${tone ? ` is-${tone}` : ""}`}>
      <p className="cg-report__stat-label">{label}</p>
      <p className="cg-report__stat-value">{value.toLocaleString()}</p>
      {caption && <p className="cg-campaign-report__stat-caption">{caption}</p>}
    </div>
  );
}

function Overview({ data }: { data: CampaignReport }) {
  return (
    <div className="cg-campaign-report__overview">
      <section>
        <p className="cg-campaign-report__section-title">By department</p>
        <table className="cg-table cg-table--no-hover">
          <thead>
            <tr>
              <th>Department</th>
              <th>In scope</th>
              <th>Verified</th>
              <th>Discrepancies</th>
              <th style={{ width: "30%" }}>Completion</th>
            </tr>
          </thead>
          <tbody>
            {data.by_department.map((d) => (
              <tr key={d.department}>
                <td>{d.department}</td>
                <td className="cg-table__muted">{d.assets_in_scope}</td>
                <td className="cg-table__muted">{d.verified}</td>
                <td className={d.discrepancies > 0 ? "cg-campaign-report__alert" : "cg-table__muted"}>{d.discrepancies}</td>
                <td>
                  <div className="cg-campaign-report__bar">
                    <span style={{ width: `${d.completion_percent}%` }} />
                  </div>
                  <span className="cg-table__muted">{d.completion_percent}%</span>
                </td>
              </tr>
            ))}
            {data.by_department.length === 0 && (
              <tr>
                <td colSpan={5} className="cg-table__muted">
                  No assets in scope.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </section>

      <div className="cg-campaign-report__split">
        <section>
          <p className="cg-campaign-report__section-title">By verifier</p>
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Verifier</th>
                <th>Assigned</th>
                <th>Completed</th>
                <th>Issues found</th>
              </tr>
            </thead>
            <tbody>
              {data.by_verifier.map((v) => (
                <tr key={v.name}>
                  <td>{v.name}</td>
                  <td className="cg-table__muted">{v.assigned}</td>
                  <td className="cg-table__muted">{v.completed}</td>
                  <td className={v.issues_found > 0 ? "cg-campaign-report__alert" : "cg-table__muted"}>{v.issues_found}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
        <section>
          <p className="cg-campaign-report__section-title">Discrepancies by type</p>
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Type</th>
                <th>Count</th>
              </tr>
            </thead>
            <tbody>
              {data.discrepancies_by_classification.map((c) => (
                <tr key={c.label}>
                  <td>
                    <Tag type={statusTagColor(c.label)} size="sm">
                      {formatStatusLabel(c.label)}
                    </Tag>
                  </td>
                  <td className="cg-table__muted">{c.count}</td>
                </tr>
              ))}
              {data.discrepancies_by_classification.length === 0 && (
                <tr>
                  <td colSpan={2} className="cg-table__muted">
                    None raised.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </section>
      </div>
    </div>
  );
}

function AssetRegister({ tasks }: { tasks: CampaignReportTaskRow[] }) {
  const [search, setSearch] = useState("");
  const [outcome, setOutcome] = useState("");

  const visible = useMemo(() => {
    const q = search.trim().toLowerCase();
    return tasks.filter(
      (t) =>
        (!outcome || (outcome === "Issues" ? !["Verified", "Pending"].includes(t.outcome) : t.outcome === outcome)) &&
        (!q || [t.asset_code, t.asset_name, t.department, t.recorded_location].some((v) => v?.toLowerCase().includes(q))),
    );
  }, [tasks, search, outcome]);

  return (
    <>
      <div className="cg-campaign-report__filters">
        <Search id="campaign-report-search" size="sm" labelText="Search assets" placeholder="Search by code, name, department or location" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Select id="campaign-report-outcome" size="sm" labelText="Outcome" hideLabel value={outcome} onChange={(e) => setOutcome(e.target.value)}>
          <SelectItem value="" text="All outcomes" />
          <SelectItem value="Issues" text="Any issue" />
          {(Object.keys(OUTCOMES) as VerificationOutcome[]).map((o) => (
            <SelectItem key={o} value={o} text={OUTCOMES[o].label} />
          ))}
        </Select>
        <span className="cg-table__muted">
          {visible.length} of {tasks.length}
        </span>
      </div>
      <div style={{ overflowX: "auto" }}>
        <table className="cg-table cg-table--no-hover">
          <thead>
            <tr>
              <th>Asset</th>
              <th>Department</th>
              <th>Outcome</th>
              <th>Recorded location</th>
              <th>Found at</th>
              <th>Condition</th>
              <th>Verified by</th>
              <th>Date</th>
            </tr>
          </thead>
          <tbody>
            {visible.map((t) => {
              const locationIssue = t.outcome === "LocationMismatch" || t.outcome === "LocationAndConditionMismatch" || t.outcome === "NotFound";
              const conditionIssue = t.outcome === "ConditionMismatch" || t.outcome === "LocationAndConditionMismatch";
              return (
                <tr key={t.asset_code}>
                  <td>
                    <span className="cg-table__mono">{t.asset_code}</span>
                    <br />
                    <span className="cg-table__muted">{t.asset_name}</span>
                  </td>
                  <td className="cg-table__muted">{t.department ?? "-"}</td>
                  <td>
                    <Tag type={OUTCOMES[t.outcome].tag} size="sm">
                      {OUTCOMES[t.outcome].label}
                    </Tag>
                    {t.is_overdue && <span className="cg-campaign-report__overdue">Overdue</span>}
                  </td>
                  <td className="cg-table__muted">{t.recorded_location ?? "-"}</td>
                  <td className={locationIssue ? "cg-campaign-report__alert" : "cg-table__muted"}>
                    {t.asserted_present === false ? "Not found" : (t.asserted_location ?? (t.outcome === "Pending" ? "-" : t.recorded_location ?? "-"))}
                  </td>
                  <td className={conditionIssue ? "cg-campaign-report__warn" : "cg-table__muted"}>
                    {conditionIssue ? `${humanize(t.recorded_condition)} → ${humanize(t.asserted_condition)}` : humanize(t.recorded_condition)}
                  </td>
                  <td className="cg-table__muted">{t.completed_by_name ?? (t.assigned_to_name ? `${t.assigned_to_name} (assigned)` : "Unassigned")}</td>
                  <td className="cg-table__muted">{t.completed_at ? formatDate(t.completed_at) : `Due ${t.due_date}`}</td>
                </tr>
              );
            })}
            {visible.length === 0 && (
              <tr>
                <td colSpan={8} className="cg-table__muted">
                  No assets match.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}

function DiscrepancyRegister({ data }: { data: CampaignReport }) {
  if (data.discrepancies.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No discrepancies were raised during this campaign.</p>
      </div>
    );
  }
  return (
    <div style={{ overflowX: "auto" }}>
      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr>
            <th>Asset</th>
            <th>Classification</th>
            <th>Status</th>
            <th>Finding</th>
            <th>Photo</th>
            <th>Raised</th>
            <th>Resolution</th>
          </tr>
        </thead>
        <tbody>
          {data.discrepancies.map((d) => (
            <tr key={d.id}>
              <td>
                <span className="cg-table__mono">{d.asset_code}</span>
                <br />
                <span className="cg-table__muted">{d.asset_name}</span>
              </td>
              <td>
                <Tag type={statusTagColor(d.type)} size="sm">
                  {formatStatusLabel(d.type)}
                </Tag>
              </td>
              <td>
                <Tag type={d.status === "Open" ? "red" : "green"} size="sm">
                  {d.status}
                </Tag>
              </td>
              <td style={{ maxWidth: "16rem" }}>{d.description}</td>
              <td>
                {d.photo_url ? (
                  <PhotoThumbnail url={d.photo_url} alt={d.description} title={`Discrepancy photo: ${d.asset_code}`} />
                ) : d.has_photo ? (
                  <span className="cg-table__muted" title="Photo storage isn't reachable right now">On file</span>
                ) : (
                  <span className="cg-table__muted">-</span>
                )}
              </td>
              <td className="cg-table__muted">
                {formatDate(d.raised_at)}
                <br />
                {d.is_automatic ? `${d.raised_by_name ?? "System"} (auto)` : d.raised_by_name ?? "-"}
              </td>
              <td style={{ maxWidth: "18rem" }}>
                {d.status === "Open" ? (
                  <span className="cg-table__muted">Awaiting resolution</span>
                ) : (
                  <>
                    <strong>{humanize(d.resolution_type)}</strong>
                    {d.resolution_explanation && <p className="cg-campaign-report__resolution">{d.resolution_explanation}</p>}
                    {d.corrective_action && <p className="cg-campaign-report__resolution">Action: {d.corrective_action}</p>}
                    <p className="cg-table__muted cg-campaign-report__resolution">
                      {d.resolved_by_name ?? "-"} · {formatDate(d.resolved_at)}
                      {d.register_corrected ? " · register corrected" : ""}
                    </p>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
