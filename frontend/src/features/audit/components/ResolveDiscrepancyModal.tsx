import { useState } from "react";
import { Modal, Select, SelectItem, TextArea, Checkbox, InlineNotification, Tag } from "@carbon/react";
import { useResolveDiscrepancy } from "../hooks/useDiscrepancies";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { CORRECTABLE_DISCREPANCY_TYPES } from "../api/discrepancies";
import type { Discrepancy } from "../api/discrepancies";

// FR-062's five canonical resolution types — sent to the backend as-is; the
// Select displays them through formatStatusLabel for a readable label.
const RESOLUTION_TYPES = ["REGISTER_CORRECTED", "ASSET_RELOCATED", "CONDITION_UPDATED", "WRITTEN_OFF", "NO_ACTION"];
const NO_ACTION_MIN_LENGTH = 20;

interface ResolveDiscrepancyModalProps {
  discrepancy: Discrepancy;
  onClose: () => void;
  onResolved: () => void;
}

// FR-062: an Auditor/Administrator resolves a discrepancy with a typed
// resolution and an explanation; the register correction checkbox is only
// meaningful for ConditionMismatch/LocationMismatch (see
// DiscrepancyService.ApplyRegisterCorrection on the backend).
export default function ResolveDiscrepancyModal({ discrepancy, onClose, onResolved }: ResolveDiscrepancyModalProps) {
  const resolve = useResolveDiscrepancy();

  const [resolutionType, setResolutionType] = useState("");
  const [resolutionExplanation, setResolutionExplanation] = useState("");
  const [correctiveAction, setCorrectiveAction] = useState("");
  const [applyCorrection, setApplyCorrection] = useState(false);

  const isCorrectable = CORRECTABLE_DISCREPANCY_TYPES.includes(discrepancy.type);
  const explanationMinLength = resolutionType === "NO_ACTION" ? NO_ACTION_MIN_LENGTH : 1;
  const canSubmit = resolutionType.length > 0 && resolutionExplanation.trim().length >= explanationMinLength;

  const handleSubmit = () => {
    if (!canSubmit || resolve.isPending) return;
    resolve.mutate(
      {
        id: discrepancy.id,
        payload: {
          resolution_type: resolutionType,
          resolution_explanation: resolutionExplanation.trim(),
          corrective_action: correctiveAction.trim() || undefined,
          apply_correction: isCorrectable && applyCorrection,
        },
      },
      { onSuccess: onResolved },
    );
  };

  return (
    <Modal
      open
      modalLabel="Audit & Compliance"
      modalHeading="Resolve discrepancy"
      primaryButtonText={resolve.isPending ? "Resolving…" : "Resolve"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || resolve.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {resolve.isError && (
        <InlineNotification
          kind="error"
          title="Could not resolve this discrepancy"
          subtitle={getErrorMessage(resolve.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div style={{ marginBottom: "1rem", display: "flex", gap: "0.5rem", alignItems: "center", flexWrap: "wrap" }}>
        <span className="cg-table__mono">{discrepancy.asset_code}</span>
        <Tag type={statusTagColor(discrepancy.type)}>{formatStatusLabel(discrepancy.type)}</Tag>
        <span className="cg-table__muted" style={{ fontSize: "0.8125rem" }}>
          {discrepancy.is_automatic ? "Raised automatically" : `Raised by ${discrepancy.raised_by_email ?? "an officer"}`}
        </span>
      </div>
      <p style={{ margin: "0 0 1rem", fontSize: "0.875rem" }}>{discrepancy.description}</p>

      <div style={{ display: "grid", gap: "1rem" }}>
        <Select
          id="resolve-type"
          labelText="Resolution"
          value={resolutionType}
          onChange={(e) => setResolutionType(e.target.value)}
        >
          <SelectItem value="" text="Choose a resolution…" />
          {RESOLUTION_TYPES.map((t) => (
            <SelectItem key={t} value={t} text={formatStatusLabel(t)} />
          ))}
        </Select>
        <TextArea
          id="resolve-explanation"
          labelText="Explanation"
          helperText={
            resolutionType === "NO_ACTION" ? `No Action requires a justification of at least ${NO_ACTION_MIN_LENGTH} characters.` : undefined
          }
          rows={3}
          value={resolutionExplanation}
          onChange={(e) => setResolutionExplanation(e.target.value)}
          invalid={resolutionExplanation.length > 0 && resolutionExplanation.trim().length < explanationMinLength}
          invalidText={`Must be at least ${explanationMinLength} characters.`}
        />
        <TextArea
          id="resolve-corrective-action"
          labelText="Corrective action (optional)"
          rows={2}
          value={correctiveAction}
          onChange={(e) => setCorrectiveAction(e.target.value)}
        />
        <Checkbox
          id="resolve-apply-correction"
          labelText={
            isCorrectable
              ? "Apply the officer's asserted value to the asset register"
              : "Register correction isn't available for this discrepancy type"
          }
          checked={applyCorrection}
          onChange={(_, { checked }) => setApplyCorrection(checked)}
          disabled={!isCorrectable}
        />
      </div>
    </Modal>
  );
}
