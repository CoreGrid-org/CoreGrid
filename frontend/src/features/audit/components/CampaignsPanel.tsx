import { useState } from "react";
import { Tag, Button, InlineNotification, Modal, Pagination } from "@carbon/react";
import { Add, Edit, TrashCan } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { useClientPagination } from "@/shared/hooks/useClientPagination";
import { useCampaignsList, useDeleteCampaign } from "../hooks/useCampaigns";
import CreateCampaignModal from "./CreateCampaignModal";
import EditCampaignModal from "./EditCampaignModal";
import CampaignReportModal from "./CampaignReportModal";
import CampaignTasksModal from "./CampaignTasksModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { campaignScopeLabel } from "../lib/campaignScopeLabel";
import type { Campaign } from "../api/campaigns";

export default function CampaignsPanel() {
  const campaigns = useCampaignsList();
  const deleteCampaign = useDeleteCampaign();
  const { pageItems, page, pageSize, total, setPage, setPageSize } = useClientPagination(campaigns.data);

  const [showCreateCampaign, setShowCreateCampaign] = useState(false);
  const [editingCampaign, setEditingCampaign] = useState<Campaign | null>(null);
  const [deletingCampaign, setDeletingCampaign] = useState<Campaign | null>(null);
  const [reportCampaign, setReportCampaign] = useState<{ id: string; name: string } | null>(null);
  const [tasksCampaign, setTasksCampaign] = useState<{ id: string; name: string } | null>(null);

  const handleDeleteConfirm = () => {
    if (!deletingCampaign || deleteCampaign.isPending) return;
    deleteCampaign.mutate(deletingCampaign.id, {
      onSuccess: () => {
        setDeletingCampaign(null);
        campaigns.refetch();
      },
    });
  };

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

      {deleteCampaign.isError && (
        <InlineNotification
          kind="error"
          title="Could not delete campaign"
          subtitle={getErrorMessage(deleteCampaign.error, "Something went wrong. Please try again.")}
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
          <>
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Campaign</th>
                <th>Period</th>
                <th>Scope</th>
                <th>Status</th>
                <th>Progress</th>
                <th>Discrepancies</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {pageItems.map((c) => (
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
                    <div style={{ display: "flex", gap: "0.25rem", alignItems: "center" }}>
                      <Button kind="ghost" size="sm" onClick={() => setTasksCampaign({ id: c.id, name: c.name })}>
                        View tasks
                      </Button>
                      <Button kind="ghost" size="sm" onClick={() => setReportCampaign({ id: c.id, name: c.name })}>
                        View report
                      </Button>
                      <Button
                        kind="ghost"
                        size="sm"
                        hasIconOnly
                        iconDescription="Edit campaign"
                        renderIcon={Edit}
                        onClick={() => setEditingCampaign(c)}
                      />
                      <Button
                        kind="danger--ghost"
                        size="sm"
                        hasIconOnly
                        iconDescription="Delete campaign"
                        renderIcon={TrashCan}
                        onClick={() => setDeletingCampaign(c)}
                      />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination
            page={page}
            pageSize={pageSize}
            pageSizes={[10, 20, 50, 100]}
            totalItems={total}
            onChange={({ page: nextPage, pageSize: nextPageSize }) => {
              setPage(nextPage);
              setPageSize(nextPageSize);
            }}
          />
          </>
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

      {editingCampaign && (
        <EditCampaignModal
          campaign={editingCampaign}
          onClose={() => setEditingCampaign(null)}
          onUpdated={() => {
            setEditingCampaign(null);
            campaigns.refetch();
          }}
        />
      )}

      {deletingCampaign && (
        <Modal
          open
          danger
          modalHeading="Delete Verification Campaign"
          modalLabel="Confirm Deletion"
          primaryButtonText={deleteCampaign.isPending ? "Deleting…" : "Delete"}
          secondaryButtonText="Cancel"
          onRequestClose={() => setDeletingCampaign(null)}
          onRequestSubmit={handleDeleteConfirm}
        >
          <p>
            Are you sure you want to delete campaign <strong>"{deletingCampaign.name}"</strong>?
          </p>
          <p className="cg-table__muted" style={{ marginTop: "0.5rem" }}>
            This action will permanently delete the campaign and its {deletingCampaign.task_count} verification task(s).
          </p>
        </Modal>
      )}

      {tasksCampaign && (
        <CampaignTasksModal
          campaignId={tasksCampaign.id}
          campaignName={tasksCampaign.name}
          onClose={() => setTasksCampaign(null)}
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
