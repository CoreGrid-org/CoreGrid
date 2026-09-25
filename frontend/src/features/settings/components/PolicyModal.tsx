import { useState } from "react";
import { Modal, ComboBox, NumberInput, InlineNotification } from "@carbon/react";
import { useCreateOrganizationPolicy, useUpdateOrganizationPolicy } from "../hooks/useOrgConfig";
import { useAssetTypes } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { OrganizationPolicy, SaveOrganizationPolicyRequest } from "../api/orgConfig";

const ORGANIZATION_WIDE_DEFAULT = { id: "", name: "Organisation-wide default" };

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

// Mirrors SaveOrganizationPolicyRequest's [Range] attributes
// (backend/Features/OrgConfig/DTOs/SaveOrganizationPolicyRequest.cs).
const BOUNDS: Record<keyof Omit<SaveOrganizationPolicyRequest, "asset_type_id">, { min: number; max: number }> = {
  repair_to_replace_cost_threshold: { min: 0, max: 100 },
  minimum_service_life_years: { min: 0, max: 100 },
  max_acceptable_failure_frequency: { min: 0, max: 100 },
  valuation_validity_window_days: { min: 0, max: 3650 },
  confidence_floor: { min: 0, max: 1 },
  cost_variance_tolerance_percent: { min: 0, max: 100 },
  outstanding_transfer_days: { min: 0, max: 3650 },
  approval_overdue_period_hours: { min: 0, max: 8760 },
};

type NumericField = keyof typeof BOUNDS;

function isFieldValid(field: NumericField, value: number): boolean {
  const { min, max } = BOUNDS[field];
  return Number.isFinite(value) && value >= min && value <= max;
}

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

  const [touched, setTouched] = useState<Partial<Record<NumericField, boolean>>>({});
  const markTouched = (field: NumericField) => setTouched((t) => ({ ...t, [field]: true }));

  const createPolicy = useCreateOrganizationPolicy();
  const updatePolicy = useUpdateOrganizationPolicy();
  const mutation = policy ? updatePolicy : createPolicy;

  const set = <K extends keyof SaveOrganizationPolicyRequest>(key: K, value: SaveOrganizationPolicyRequest[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const fieldInvalid = (field: NumericField) => touched[field] && !isFieldValid(field, form[field]);
  const canSubmit = (Object.keys(BOUNDS) as NumericField[]).every((field) => isFieldValid(field, form[field]));

  const handleSubmit = () => {
    setTouched({
      repair_to_replace_cost_threshold: true,
      minimum_service_life_years: true,
      max_acceptable_failure_frequency: true,
      valuation_validity_window_days: true,
      confidence_floor: true,
      cost_variance_tolerance_percent: true,
      outstanding_transfer_days: true,
      approval_overdue_period_hours: true,
    });
    if (!canSubmit || mutation.isPending) return;
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
      primaryButtonDisabled={!canSubmit || mutation.isPending}
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
          className="cg-panel-notification"
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <ComboBox<{ id: string; name: string }>
          id="policy-asset-type"
          titleText="Applies to"
          placeholder="Search asset types…"
          items={[ORGANIZATION_WIDE_DEFAULT, ...(assetTypes ?? [])]}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={
            form.asset_type_id
              ? assetTypes?.find((t) => t.id === form.asset_type_id) ?? null
              : ORGANIZATION_WIDE_DEFAULT
          }
          onChange={({ selectedItem }) => set("asset_type_id", selectedItem?.id || null)}
        />

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <NumberInput
            id="policy-repair-to-replace"
            label="Repair-to-replace cost threshold"
            helperText="Above this ratio, favour replacement over repair."
            step={0.01}
            min={BOUNDS.repair_to_replace_cost_threshold.min}
            max={BOUNDS.repair_to_replace_cost_threshold.max}
            value={form.repair_to_replace_cost_threshold}
            onChange={(_e, { value }) => set("repair_to_replace_cost_threshold", Number(value))}
            onBlur={() => markTouched("repair_to_replace_cost_threshold")}
            invalid={!!fieldInvalid("repair_to_replace_cost_threshold")}
            invalidText="Must be between 0 and 100."
          />
          <NumberInput
            id="policy-min-service-life"
            label="Minimum service life (years)"
            helperText="Required before a disposal recommendation."
            min={BOUNDS.minimum_service_life_years.min}
            max={BOUNDS.minimum_service_life_years.max}
            value={form.minimum_service_life_years}
            onChange={(_e, { value }) => set("minimum_service_life_years", Number(value))}
            onBlur={() => markTouched("minimum_service_life_years")}
            invalid={!!fieldInvalid("minimum_service_life_years")}
            invalidText="Must be between 0 and 100."
          />
          <NumberInput
            id="policy-max-failure-frequency"
            label="Max acceptable failure frequency (/year)"
            min={BOUNDS.max_acceptable_failure_frequency.min}
            max={BOUNDS.max_acceptable_failure_frequency.max}
            value={form.max_acceptable_failure_frequency}
            onChange={(_e, { value }) => set("max_acceptable_failure_frequency", Number(value))}
            onBlur={() => markTouched("max_acceptable_failure_frequency")}
            invalid={!!fieldInvalid("max_acceptable_failure_frequency")}
            invalidText="Must be between 0 and 100."
          />
          <NumberInput
            id="policy-valuation-window"
            label="Valuation validity window (days)"
            min={BOUNDS.valuation_validity_window_days.min}
            max={BOUNDS.valuation_validity_window_days.max}
            value={form.valuation_validity_window_days}
            onChange={(_e, { value }) => set("valuation_validity_window_days", Number(value))}
            onBlur={() => markTouched("valuation_validity_window_days")}
            invalid={!!fieldInvalid("valuation_validity_window_days")}
            invalidText="Must be between 0 and 3650 days."
          />
          <NumberInput
            id="policy-confidence-floor"
            label="Confidence floor"
            helperText="Below this, human review is forced."
            step={0.01}
            min={BOUNDS.confidence_floor.min}
            max={BOUNDS.confidence_floor.max}
            value={form.confidence_floor}
            onChange={(_e, { value }) => set("confidence_floor", Number(value))}
            onBlur={() => markTouched("confidence_floor")}
            invalid={!!fieldInvalid("confidence_floor")}
            invalidText="Must be between 0 and 1."
          />
          <NumberInput
            id="policy-cost-variance"
            label="Cost variance tolerance (%)"
            min={BOUNDS.cost_variance_tolerance_percent.min}
            max={BOUNDS.cost_variance_tolerance_percent.max}
            value={form.cost_variance_tolerance_percent}
            onChange={(_e, { value }) => set("cost_variance_tolerance_percent", Number(value))}
            onBlur={() => markTouched("cost_variance_tolerance_percent")}
            invalid={!!fieldInvalid("cost_variance_tolerance_percent")}
            invalidText="Must be between 0 and 100."
          />
          <NumberInput
            id="policy-outstanding-transfer"
            label="Outstanding transfer threshold (days)"
            min={BOUNDS.outstanding_transfer_days.min}
            max={BOUNDS.outstanding_transfer_days.max}
            value={form.outstanding_transfer_days}
            onChange={(_e, { value }) => set("outstanding_transfer_days", Number(value))}
            onBlur={() => markTouched("outstanding_transfer_days")}
            invalid={!!fieldInvalid("outstanding_transfer_days")}
            invalidText="Must be between 0 and 3650 days."
          />
          <NumberInput
            id="policy-approval-overdue"
            label="Approval overdue period (hours)"
            min={BOUNDS.approval_overdue_period_hours.min}
            max={BOUNDS.approval_overdue_period_hours.max}
            value={form.approval_overdue_period_hours}
            onChange={(_e, { value }) => set("approval_overdue_period_hours", Number(value))}
            onBlur={() => markTouched("approval_overdue_period_hours")}
            invalid={!!fieldInvalid("approval_overdue_period_hours")}
            invalidText="Must be between 0 and 8760 hours."
          />
        </div>
      </div>
    </Modal>
  );
}
