import { Modal, InlineNotification, Tag } from "@carbon/react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useDisposalDetail } from "../hooks/useDisposals";
import PreconditionChecklist from "./PreconditionChecklist";
import { formatDate, formatLkr } from "../lib/format";

// Read-only compliance view of one disposal request (auditor).
export default function DisposalDetailModal({ disposalId, onClose }: { disposalId: string; onClose: () => void }) {
  const { data: disposal, isLoading, isError, error } = useDisposalDetail(disposalId);

  return (
    <Modal
      open
      passiveModal
      modalLabel="Disposal compliance"
      modalHeading={disposal ? `${disposal.asset_name} (${disposal.asset_code})` : "Loading…"}
      onRequestClose={onClose}
    >
      {isLoading ? (
        <div className="cg-placeholder">
          <p>Loading compliance record…</p>
        </div>
      ) : isError || !disposal ? (
        <InlineNotification
          kind="error"
          title="Could not load disposal details"
          subtitle={getErrorMessage(error, "Failed to load detailed record.")}
          lowContrast
          hideCloseButton
        />
      ) : (
        <div className="cg-modal-form">
          <dl className="cg-asset-summary">
            <div>
              <dt>Status</dt>
              <dd>
                <Tag type={statusTagColor(disposal.status)} size="sm">{formatStatusLabel(disposal.status)}</Tag>
              </dd>
            </div>
            <div>
              <dt>Method</dt>
              <dd>{formatStatusLabel(disposal.disposal_method)}</dd>
            </div>
            <div>
              <dt>Residual value</dt>
              <dd>{formatLkr(disposal.estimated_residual_value)}</dd>
            </div>
            <div>
              <dt>Valuation date</dt>
              <dd>{formatDate(disposal.valuation_date)}</dd>
            </div>
            <div>
              <dt>Disposed</dt>
              <dd>{disposal.disposed_at ? formatDate(disposal.disposed_at) : "-"}</dd>
            </div>
          </dl>

          {disposal.notes && (
            <div>
              <p className="cds--label">Notes &amp; revision history</p>
              <pre className="cg-notes-log">{disposal.notes}</pre>
            </div>
          )}

          {disposal.precondition_evaluation && (
            <div>
              <p className="cds--label">Precondition checks</p>
              <PreconditionChecklist evaluation={disposal.precondition_evaluation} />
            </div>
          )}
        </div>
      )}
    </Modal>
  );
}
