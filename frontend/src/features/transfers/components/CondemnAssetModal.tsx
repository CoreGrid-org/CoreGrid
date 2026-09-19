import { useState } from "react";
import { Modal, InlineNotification, ComboBox, TextArea, TextInput } from "@carbon/react";
import { useCondemnAsset } from "../hooks/useDisposals";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";

// FR-049 — CanRequestDisposal (Officer, Administrator). Shared by
// TransfersPage (Administrator) and InventoryTransfersPage (Officer).
export default function CondemnAssetModal({ onClose, onCondemned }: { onClose: () => void; onCondemned: () => void }) {
  const [assetId, setAssetId] = useState("");
  const [reason, setReason] = useState("");
  const [evidenceUrl, setEvidenceUrl] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const { data: assetsData } = useAssetsList({ pageSize: 100 });
  const candidates = (assetsData?.items ?? []).filter((a) => a.status === "ACTIVE" || a.status === "UNDER_MAINTENANCE");

  const condemnAsset = useCondemnAsset();

  const handleSubmit = () => {
    if (!assetId || !reason.trim()) {
      setFormError("Please select an asset and specify the condemnation reason.");
      return;
    }
    setFormError(null);
    condemnAsset.mutate(
      { assetId, payload: { reason: reason.trim(), evidence_url: evidenceUrl.trim() || undefined } },
      { onSuccess: onCondemned },
    );
  };

  return (
    <Modal
      open
      modalHeading="Condemn Unserviceable Asset (FR-049)"
      primaryButtonText="Condemn Asset"
      secondaryButtonText="Cancel"
      danger
      primaryButtonDisabled={condemnAsset.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <p style={{ marginBottom: "1rem", fontSize: "0.875rem" }}>
        Condemning transitions the asset condition to <code>UNSERVICEABLE</code> and status to <code>CONDEMNED</code>, releasing it for the disposal workflow.
      </p>

      {formError && (
        <InlineNotification kind="error" title="Validation error" subtitle={formError} lowContrast hideCloseButton style={{ marginBottom: "1rem" }} />
      )}
      {condemnAsset.isError && (
        <InlineNotification
          kind="error"
          title="Could not condemn asset"
          subtitle={getErrorMessage(condemnAsset.error, "Asset condemnation failed.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem" }}
        />
      )}

      <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
        <ComboBox
          id="condemn-asset-select"
          titleText="Asset to Condemn *"
          placeholder="Select asset"
          items={candidates}
          itemToString={(item) => (item ? `${item.asset_code} — ${item.name} (${item.condition})` : "")}
          onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
        />

        <TextArea
          id="condemn-reason"
          labelText="Condemnation Justification / Reason *"
          placeholder="Detailed assessment why this asset cannot be repaired or used..."
          rows={3}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
        />

        <TextInput
          id="condemn-evidence"
          labelText="Evidence Document / Photo URL (Optional)"
          placeholder="https://..."
          value={evidenceUrl}
          onChange={(e) => setEvidenceUrl(e.target.value)}
        />
      </div>
    </Modal>
  );
}
