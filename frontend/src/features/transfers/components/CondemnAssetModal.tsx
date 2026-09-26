import { useState } from "react";
import { ComposedModal, ModalHeader, ModalBody, ModalFooter, Button, InlineNotification, ComboBox, TextArea, TextInput, Tag } from "@carbon/react";
import { WarningAltFilled } from "@carbon/icons-react";
import { useCondemnAsset } from "../hooks/useDisposals";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import type { Asset } from "@/features/assets/types/asset";
import AssetSummary from "@/features/assets/components/AssetSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";

// Matches CondemnAssetRequest's [MaxLength(1000)] on the backend.
const REASON_MAX = 1000;

function isValidUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

// Provides the asset condemnation form.
export default function CondemnAssetModal({ onClose, onCondemned }: { onClose: () => void; onCondemned: () => void }) {
  const [assetId, setAssetId] = useState("");
  const [reason, setReason] = useState("");
  const [evidenceUrl, setEvidenceUrl] = useState("");
  const [submitted, setSubmitted] = useState(false);

  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const candidates = (assetsData?.items ?? []).filter((a) => a.status === "ACTIVE" || a.status === "UNDER_MAINTENANCE");
  const selectedAsset = candidates.find((a) => a.id === assetId) ?? null;

  const condemnAsset = useCondemnAsset();

  const errors = {
    asset: !assetId ? "Choose the asset to condemn." : null,
    reason: !reason.trim() ? "Explain why the asset can't be repaired or used." : null,
    evidence: evidenceUrl.trim() && !isValidUrl(evidenceUrl.trim()) ? "Enter a full link starting with http:// or https://." : null,
  };

  const handleSubmit = () => {
    setSubmitted(true);
    if (Object.values(errors).some(Boolean) || condemnAsset.isPending) return;
    condemnAsset.mutate(
      { assetId, payload: { reason: reason.trim(), evidence_url: evidenceUrl.trim() || undefined } },
      { onSuccess: onCondemned },
    );
  };

  const handleClose = () => {
    if (!condemnAsset.isPending) onClose();
  };

  return (
    <ComposedModal open size="sm" danger onClose={handleClose} preventCloseOnClickOutside aria-label="Condemn unserviceable asset">
      <ModalHeader label="Disposals" title="Condemn unserviceable asset" closeModal={handleClose} />
      <ModalBody hasScrollingContent>
        <div className="cg-modal-form">
          <div className="cg-modal-form__callout cg-modal-form__callout--danger">
            <WarningAltFilled size={20} />
            <p>
              The asset will be marked <strong>Unserviceable</strong> and <strong>Condemned</strong>, and taken out of
              use. It can then be submitted for disposal.
            </p>
          </div>

          {condemnAsset.isError && (
            <InlineNotification
              kind="error"
              title="Could not condemn asset"
              subtitle={getErrorMessage(condemnAsset.error, "Asset condemnation failed.")}
              lowContrast
              hideCloseButton
              style={{ maxWidth: "100%" }}
            />
          )}

          <div>
            <ComboBox<Asset>
              id="condemn-asset-select"
              titleText="Asset to condemn"
              helperText={selectedAsset ? undefined : "Active assets and assets under maintenance."}
              placeholder={isLoadingAssets ? "Loading assets…" : "Type to search by code or name…"}
              disabled={isLoadingAssets}
              autoAlign
              items={candidates}
              itemToString={(item) => (item ? `${item.asset_code} - ${item.name}` : "")}
              selectedItem={selectedAsset}
              shouldFilterItem={comboBoxFilter(selectedAsset)}
              onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
              invalid={submitted && Boolean(errors.asset)}
              invalidText={errors.asset ?? undefined}
            />
            {selectedAsset && (
              <AssetSummary
                items={[
                  { label: "Type", value: selectedAsset.asset_type_name },
                  { label: "Location", value: selectedAsset.location_name },
                  {
                    label: "Status",
                    value: <Tag type={statusTagColor(selectedAsset.status)} size="sm">{formatStatusLabel(selectedAsset.status)}</Tag>,
                  },
                  {
                    label: "Condition",
                    value: <Tag type={statusTagColor(selectedAsset.condition)} size="sm">{formatStatusLabel(selectedAsset.condition)}</Tag>,
                  },
                ]}
              />
            )}
          </div>

          <TextArea
            id="condemn-reason"
            labelText="Reason"
            placeholder="e.g. Motherboard failed; repair quote exceeds replacement cost and parts are no longer made."
            rows={4}
            enableCounter
            maxCount={REASON_MAX}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            invalid={submitted && Boolean(errors.reason)}
            invalidText={errors.reason ?? undefined}
          />

          <TextInput
            id="condemn-evidence"
            labelText="Evidence link (optional)"
            helperText="A link to an inspection report, repair quote or photo."
            placeholder="https://…"
            type="url"
            value={evidenceUrl}
            onChange={(e) => setEvidenceUrl(e.target.value)}
            invalid={submitted && Boolean(errors.evidence)}
            invalidText={errors.evidence ?? undefined}
          />
        </div>
      </ModalBody>
      <ModalFooter danger>
        <Button kind="secondary" onClick={handleClose} disabled={condemnAsset.isPending}>
          Cancel
        </Button>
        <Button kind="danger" onClick={handleSubmit} disabled={condemnAsset.isPending}>
          {condemnAsset.isPending ? "Condemning…" : "Condemn asset"}
        </Button>
      </ModalFooter>
    </ComposedModal>
  );
}
