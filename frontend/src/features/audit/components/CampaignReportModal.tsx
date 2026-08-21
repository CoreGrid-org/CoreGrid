import { Modal, Button, Tag, InlineNotification } from "@carbon/react";
import { DocumentPdf, DocumentExport } from "@carbon/icons-react";
import { useCampaignReport, useExportCampaignReport } from "../hooks/useCampaignReport";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";

interface CampaignReportModalProps {
  campaignId: string;
  campaignName: string;
  onClose: () => void;
}

// FR-065/FR-084/FR-085: the campaign completion report — what got verified,
// what didn't, and every discrepancy raised along the way, with a PDF/CSV
// export of exactly what's on screen. This is the only report Component D
// owns end to end; the asset inventory, maintenance and disposal reports on
// the Reports page belong to Components A/B/C respectively.
export default function CampaignReportModal({ campaignId, campaignName, onClose }: CampaignReportModalProps) {
  const report = useCampaignReport(campaignId);
  const exportReport = useExportCampaignReport();

  return (
    <Modal
      open
      passiveModal
      size="lg"
      modalLabel="Audit & Compliance"
      modalHeading={`Campaign report — ${campaignName}`}
      onRequestClose={onClose}
    >
      <p className="cg-table__muted" style={{ margin: "0 0 1rem", fontSize: "0.8125rem" }}>
        This is the campaign's completion report: how many in-scope assets were verified, how many are still
        outstanding, and every discrepancy raised during the campaign, broken down by classification and by
        resolution status (FR-065). Export it as a PDF to file or share, or as a CSV to work with the raw numbers —
        either one reflects exactly what's shown here (FR-084, FR-085).
      </p>

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
      ) : report.data ? (
        <>
          <div className="cg-toolbar" style={{ justifyContent: "space-between", marginBottom: "1rem" }}>
            <span className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
              {report.data.period_start} – {report.data.period_end} · {report.data.scope}
            </span>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <Button
                kind="tertiary"
                size="sm"
                renderIcon={DocumentPdf}
                disabled={exportReport.isPending}
                onClick={() => exportReport.mutate({ campaignId, format: "pdf" })}
              >
                Export PDF
              </Button>
              <Button
                kind="tertiary"
                size="sm"
                renderIcon={DocumentExport}
                disabled={exportReport.isPending}
                onClick={() => exportReport.mutate({ campaignId, format: "csv" })}
              >
                Export CSV
              </Button>
            </div>
          </div>

          <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: "1rem", gridTemplateColumns: "repeat(3, 1fr)" }}>
            <div className="cg-stat-card">
              <p className="cg-stat-card__label">Assets in scope</p>
              <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                {report.data.assets_in_scope}
              </p>
            </div>
            <div className="cg-stat-card">
              <p className="cg-stat-card__label">Verified</p>
              <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                {report.data.verified}
              </p>
            </div>
            <div className="cg-stat-card">
              <p className="cg-stat-card__label">Outstanding</p>
              <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>
                {report.data.outstanding}
              </p>
            </div>
          </div>

          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", marginBottom: "1rem" }}>
            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Discrepancies by classification</p>
              </div>
              <table className="cg-table cg-table--no-hover">
                <tbody>
                  {report.data.discrepancies_by_classification.length > 0 ? (
                    report.data.discrepancies_by_classification.map((c) => (
                      <tr key={c.label}>
                        <td>
                          <Tag type={statusTagColor(c.label)}>{formatStatusLabel(c.label)}</Tag>
                        </td>
                        <td>{c.count}</td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td className="cg-table__muted">None</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Discrepancies by resolution status</p>
              </div>
              <table className="cg-table cg-table--no-hover">
                <tbody>
                  {report.data.discrepancies_by_resolution_status.length > 0 ? (
                    report.data.discrepancies_by_resolution_status.map((c) => (
                      <tr key={c.label}>
                        <td>
                          <Tag type={statusTagColor(c.label)}>{formatStatusLabel(c.label)}</Tag>
                        </td>
                        <td>{c.count}</td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td className="cg-table__muted">None</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          <div className="cg-section" style={{ marginBottom: "1rem" }}>
            <div className="cg-section__header">
              <p className="cg-section__title">Verification tasks</p>
            </div>
            <table className="cg-table cg-table--no-hover">
              <thead>
                <tr>
                  <th>Asset</th>
                  <th>Status</th>
                  <th>Assigned to</th>
                  <th>Due</th>
                </tr>
              </thead>
              <tbody>
                {report.data.tasks.length > 0 ? (
                  report.data.tasks.map((t, i) => (
                    <tr key={i}>
                      <td className="cg-table__mono">{t.asset_code}</td>
                      <td>
                        <Tag type={statusTagColor(t.status)}>{formatStatusLabel(t.status)}</Tag>
                      </td>
                      <td className="cg-table__muted">{t.assigned_to_email ?? "Unassigned"}</td>
                      <td className="cg-table__muted">{t.due_date}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={4} className="cg-table__muted">
                      No tasks.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="cg-section">
            <div className="cg-section__header">
              <p className="cg-section__title">Discrepancies</p>
            </div>
            <table className="cg-table cg-table--no-hover">
              <thead>
                <tr>
                  <th>Asset</th>
                  <th>Classification</th>
                  <th>Status</th>
                  <th>Description</th>
                  <th>Resolution</th>
                </tr>
              </thead>
              <tbody>
                {report.data.discrepancies.length > 0 ? (
                  report.data.discrepancies.map((d, i) => (
                    <tr key={i}>
                      <td className="cg-table__mono">{d.asset_code}</td>
                      <td>
                        <Tag type={statusTagColor(d.type)}>{formatStatusLabel(d.type)}</Tag>
                      </td>
                      <td>
                        <Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag>
                      </td>
                      <td className="cg-table__muted">{d.description}</td>
                      <td className="cg-table__muted">{d.resolution_type ? formatStatusLabel(d.resolution_type) : "—"}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5} className="cg-table__muted">
                      No discrepancies.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      ) : null}
    </Modal>
  );
}
