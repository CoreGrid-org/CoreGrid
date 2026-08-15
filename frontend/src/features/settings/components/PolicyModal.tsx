import { useState } from "react";
import { Modal, Select, SelectItem, NumberInput, InlineNotification } from "@carbon/react";
import { useCreateOrganizationPolicy, useUpdateOrganizationPolicy } from "../hooks/useOrgConfig";
import { useAssetTypes } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { OrganizationPolicy, SaveOrganizationPolicyRequest } from "../api/orgConfig";

interface PolicyModalProps {
  policy?: OrganizationPolicy;
  onClose: () => void;
  onSaved: () => void;
}

const DEFAULTS: SaveOrganizationPolicyRequest = {
  asset_type_id: null,
  repair_to_replace_cost_threshold: 0.65,
  minimum_service_life_years: 5,
  max_acceptable_failure_frequency: 3,
  valuation_validity_window_days: 90,
  confidence_floor: 0.7,
  cost_variance_tolerance_percent: 15,
  outstanding_transfer_days: 7,
  approval_overdue_period_hours: 48,
};

// Create or amend an organisation policy (FR-015) — at most one per asset
// type, plus at most one organisation-wide default (assetTypeId = null);
// the backend enforces this and returns a 400 on conflict.
export default function PolicyModal({ policy, onClose, onSaved }: PolicyModalProps) {
  const { data: assetTypes } = useAssetTypes();
  const [form, setForm] = useState<SaveOrganizationPolicyRequest>(
    policy
      ? {
          asset_type_id: policy.asset_type_id,
          repair_to_replace_cost_threshold: policy.repair_to_replace_cost_threshold,
          minimum_service_life_years: policy.minimum_service_life_years,
          max_acceptable_failure_frequency: policy.max_acceptable_failure_frequency,
          valuation_validity_window_days: policy.valuation_validity_window_days,
          confidence_floor: policy.confidence_floor,
          cost_variance_tolerance_percent: policy.cost_variance_tolerance_percent,
          outstanding_transfer_days: policy.outstanding_transfer_days,
          approval_overdue_period_hours: policy.approval_overdue_period_hours,
        }
      : DEFAULTS,
  );

  const createPolicy = useCreateOrganizationPolicy();
  const updatePolicy = useUpdateOrganizationPolicy();
  const mutation = policy ? updatePolicy : createPolicy;

  const set = <K extends keyof SaveOrganizationPolicyRequest>(key: K, value: SaveOrganizationPolicyRequest[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const handleSubmit = () => {
    if (mutation.isPending) return;
    if (policy) {
      updatePolicy.mutate({ id: policy.id, payload: form }, { onSuccess: onSaved });
    } else {
      createPolicy.mutate(form, { onSuccess: onSaved });
    }
  };

  return (
    <Modal
      open
      size="lg"
      modalLabel="Organisation Settings"
      modalHeading={policy ? "Edit policy" : "Add policy"}
      primaryButtonText={mutation.isPending ? "Saving…" : "Save"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={mutation.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {mutation.isError && (
        <InlineNotification
          kind="error"
          title="Could not save policy"
          subtitle={getErrorMessage(mutation.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <Select
          id="policy-asset-type"
          labelText="Applies to"
          value={form.asset_type_id ?? ""}
          onChange={(e) => set("asset_type_id", e.target.value || null)}
        >
          <SelectItem value="" text="Organisation-wide default" />
          {assetTypes?.map((t) => <SelectItem key={t.id} value={t.id} text={t.name} />)}
        </Select>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <NumberInput
            id="policy-repair-to-replace"
            label="Repair-to-replace cost threshold"
            helperText="Above this ratio, favour replacement over repair."
            step={0.01}
            value={form.repair_to_replace_cost_threshold}
            onChange={(_e, { value }) => set("repair_to_replace_cost_threshold", Number(value))}
          />
          <NumberInput
            id="policy-min-service-life"
            label="Minimum service life (years)"
            helperText="Required before a disposal recommendation."
            value={form.minimum_service_life_years}
            onChange={(_e, { value }) => set("minimum_service_life_years", Number(value))}
          />
          <NumberInput
            id="policy-max-failure-frequency"
            label="Max acceptable failure frequency (/year)"
            value={form.max_acceptable_failure_frequency}
            onChange={(_e, { value }) => set("max_acceptable_failure_frequency", Number(value))}
          />
          <NumberInput
            id="policy-valuation-window"
            label="Valuation validity window (days)"
            value={form.valuation_validity_window_days}
            onChange={(_e, { value }) => set("valuation_validity_window_days", Number(value))}
          />
          <NumberInput
            id="policy-confidence-floor"
            label="Confidence floor"
            helperText="Below this, human review is forced."
            step={0.01}
            value={form.confidence_floor}
            onChange={(_e, { value }) => set("confidence_floor", Number(value))}
          />
          <NumberInput
            id="policy-cost-variance"
            label="Cost variance tolerance (%)"
            value={form.cost_variance_tolerance_percent}
            onChange={(_e, { value }) => set("cost_variance_tolerance_percent", Number(value))}
          />
          <NumberInput
            id="policy-outstanding-transfer"
            label="Outstanding transfer threshold (days)"
            value={form.outstanding_transfer_days}
            onChange={(_e, { value }) => set("outstanding_transfer_days", Number(value))}
          />
          <NumberInput
            id="policy-approval-overdue"
            label="Approval overdue period (hours)"
            value={form.approval_overdue_period_hours}
            onChange={(_e, { value }) => set("approval_overdue_period_hours", Number(value))}
          />
        </div>
      </div>
    </Modal>
  );
}
