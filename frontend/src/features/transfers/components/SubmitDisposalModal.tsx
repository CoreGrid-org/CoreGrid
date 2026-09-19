import { useState } from "react";
import { Modal, InlineNotification, ComboBox, Select, SelectItem, NumberInput, TextInput, TextArea } from "@carbon/react";
import { useSubmitDisposal } from "../hooks/useDisposals";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { DisposalMethod } from "../types";

// FR-050 — CanRequestDisposal (Officer, Administrator). Shared by
// TransfersPage (Administrator) and InventoryTransfersPage (Officer).
export default function SubmitDisposalModal({ onClose, onSubmitted }: { onClose: () => void; onSubmitted: () => void }) {
  const [assetId, setAssetId] = useState("");
  const [method, setMethod] = useState<DisposalMethod>("AUCTION");
  const [residualValue, setResidualValue] = useState<number>(0);
  const [valuationDate, setValuationDate] = useState("");
  const [notes, setNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const { data: assetsData } = useAssetsList({ pageSize: 100 });
  const condemnedAssets = (assetsData?.items ?? []).filter((a) => a.status === "CONDEMNED");

  const submitDisposal = useSubmitDisposal();

  const handleSubmit = () => {
    if (!assetId) {
      setFormError("Please select a condemned asset.");
      return;
    }
    if (residualValue < 0) {
      setFormError("Residual value must be non-negative.");
      return;
    }
    setFormError(null);
    submitDisposal.mutate(
      {
        asset_id: assetId,
        disposal_method: method,
        estimated_residual_value: residualValue,
        valuation_date: valuationDate ? valuationDate : null,
        notes: notes.trim() || null,
      },
      { onSuccess: onSubmitted },
    );
  };

  return (
    <Modal
      open
      modalHeading="Submit Disposal Request (FR-050)"
      primaryButtonText="Submit Disposal"
      secondaryButtonText="Cancel"
      primaryButtonDisabled={submitDisposal.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <p style={{ marginBottom: "1rem", fontSize: "0.875rem" }}>
        Submit an asset for disposal evaluation. Only <code>CONDEMNED</code> assets can be submitted for disposal.
      </p>

      {formError && (
        <InlineNotification kind="error" title="Validation error" subtitle={formError} lowContrast hideCloseButton style={{ marginBottom: "1rem" }} />
      )}
      {submitDisposal.isError && (
        <InlineNotification
          kind="error"
          title="Could not submit disposal request"
          subtitle={getErrorMessage(submitDisposal.error, "Disposal submission failed.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem" }}
        />
      )}

      <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
        <ComboBox
          id="disposal-asset-select"
          titleText="Condemned Asset *"
          placeholder="Select condemned asset"
          items={condemnedAssets}
          itemToString={(item) => (item ? `${item.asset_code} — ${item.name}` : "")}
          onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
        />

        <Select id="disposal-method-select" labelText="Proposed Disposal Method *" value={method} onChange={(e) => setMethod(e.target.value as DisposalMethod)}>
          <SelectItem value="AUCTION" text="Auction / Public Sale" />
          <SelectItem value="SCRAP" text="Scrap / Salvage" />
          <SelectItem value="DONATION" text="Donation" />
          <SelectItem value="DESTROY" text="Destruction / Recycling" />
        </Select>

        <NumberInput
          id="disposal-residual-value"
          label="Estimated Residual Value (LKR) *"
          min={0}
          step={100}
          value={residualValue}
          onChange={(_e, { value }) => setResidualValue(Number(value) || 0)}
        />

        <TextInput
          id="disposal-valuation-date"
          labelText="Valuation Date (YYYY-MM-DD)"
          placeholder="2026-09-12"
          value={valuationDate}
          onChange={(e) => setValuationDate(e.target.value)}
        />

        <TextArea
          id="disposal-notes"
          labelText="Notes / Comments (Optional)"
          placeholder="Additional context, disposal committee notes..."
          rows={3}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />
      </div>
    </Modal>
  );
}
