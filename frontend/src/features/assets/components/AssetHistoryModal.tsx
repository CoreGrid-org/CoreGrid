import { useState } from "react";
import { Modal, InlineNotification, Pagination } from "@carbon/react";
import { useAssetHistory } from "../hooks/useAssets";
import { formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";

interface AssetHistoryModalProps {
  assetId: string;
  assetName: string;
  assetCode: string;
  onClose: () => void;
}

// GET /api/assets/{id}/history — read-only history feed for one asset.
// Kept separate from AssetDetailModal (which also lets an officer edit the
// condition) so opening this view never risks an accidental field change.
export default function AssetHistoryModal({ assetId, assetName, assetCode, onClose }: AssetHistoryModalProps) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const history = useAssetHistory(assetId, page, pageSize);

  return (
    <Modal
      open
      modalLabel={assetCode}
      modalHeading={`${assetName} — history`}
      passiveModal
      onRequestClose={onClose}
      size="md"
    >
      {history.isError && (
        <InlineNotification
          kind="error"
          title="Could not load history"
          subtitle={getErrorMessage(history.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {history.isLoading ? (
        <div className="cg-placeholder">
          <p>Loading history…</p>
        </div>
      ) : history.data && history.data.items.length > 0 ? (
        <table className="cg-table cg-table--no-hover">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Event</th>
              <th>Description</th>
              <th>Actor</th>
            </tr>
          </thead>
          <tbody>
            {history.data.items.map((entry) => (
              <tr key={entry.id}>
                <td className="cg-table__mono">{new Date(entry.created_at).toLocaleString()}</td>
                <td>{formatStatusLabel(entry.event_type)}</td>
                <td>{entry.description}</td>
                <td className="cg-table__muted">{entry.actor_email ?? "System"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <div className="cg-placeholder">
          <p>No history recorded for this asset yet.</p>
        </div>
      )}

      {history.data && history.data.total_count > 0 && (
        <Pagination
          page={page}
          pageSize={pageSize}
          pageSizes={[10, 20, 50]}
          totalItems={history.data.total_count}
          onChange={({ page: nextPage, pageSize: nextPageSize }) => {
            setPage(nextPage);
            setPageSize(nextPageSize);
          }}
          style={{ marginTop: "1rem" }}
        />
      )}
    </Modal>
  );
}
