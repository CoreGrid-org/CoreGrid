import { useState } from "react";
import { Tag, Button, InlineNotification } from "@carbon/react";
import { Add } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { useCampaignsList } from "../hooks/useCampaigns";
import CreateCampaignModal from "./CreateCampaignModal";
import CampaignReportModal from "./CampaignReportModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { campaignScopeLabel } from "../lib/campaignScopeLabel";

export default function CampaignsPanel() {
  const campaigns = useCampaignsList();
  const [showCreateCampaign, setShowCreateCampaign] = useState(false);
  const [reportCampaign, setReportCampaign] = useState<{ id: string; name: string } | null>(null);

  return (
    <>
      <p className="cg-table__muted cg-panel-intro">
        A campaign is a scoped, time-boxed physical verification: it generates one task per in-scope asset,
        assigns each to the responsible officer, and tracks completion and discrepancies as officers scan and
        confirm assets against the register. Once a campaign has run, open its report to see how it went
        (verified vs. outstanding, discrepancies by type and resolution) and export it as a PDF or CSV record.
      </p>

      {campaigns.isError && (
        <InlineNotification
          kind="error"
          title="Could not load verification campaigns"
          subtitle={getErrorMessage(campaigns.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          className="cg-panel-notification"
        />
      )}

      <div className="cg-section">
        <div className="cg-section__header">
          <p className="cg-section__title">Verification Campaigns</p>
          <Button size="sm" renderIcon={Add} onClick={() => setShowCreateCampaign(true)}>
            New campaign
          </Button>
        </div>
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

      {showCreateCampaign && (
        <CreateCampaignModal
          onClose={() => setShowCreateCampaign(false)}
          onCreated={() => {
            setShowCreateCampaign(false);
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
    </>
  );
}
