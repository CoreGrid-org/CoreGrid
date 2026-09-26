import { useState } from "react";
import { Modal, ComboBox, NumberInput, InlineNotification } from "@carbon/react";
import { useCreateOrganizationPolicy, useUpdateOrganizationPolicy } from "../hooks/useOrgConfig";
import { useAssetTypes } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import type { OrganizationPolicy, SaveOrganizationPolicyRequest } from "../api/orgConfig";
import { POLICY_DEFAULTS, POLICY_FIELDS, POLICY_GROUPS, policyToPayload, policyValueError } from "../lib/policyFields";

type Target = { id: string; name: string };
const ORGANIZATION_WIDE_DEFAULT: Target = { id: "", name: "Organisation-wide default" };

interface PolicyModalProps {
  policy?: OrganizationPolicy;
  /** Every existing policy: used to offer only unclaimed targets and to start from the default's values. */
  existingPolicies: OrganizationPolicy[];
  onClose: () => void;
  /** Called with the policy's display name. */
  onSaved: (name: string) => void;
}

// Create or amend an organisation policy (FR-015): at most one per asset type
// plus at most one organisation-wide default (assetTypeId = null). Targets
// that already have a policy aren't offered, so the backend's conflict 400
// can't be hit from here.
export default function PolicyModal({ policy, existingPolicies, onClose, onSaved }: PolicyModalProps) {
  const { data: assetTypes } = useAssetTypes();
  const defaultPolicy = existingPolicies.find((p) => p.asset_type_id === null);

  // A new asset-type override starts from the organisation default's values.
  const [form, setForm] = useState<SaveOrganizationPolicyRequest>(() =>
    policy ? policyToPayload(policy) : defaultPolicy ? { ...policyToPayload(defaultPolicy), asset_type_id: null } : POLICY_DEFAULTS,
  );
  const [submitted, setSubmitted] = useState(false);

  const createPolicy = useCreateOrganizationPolicy();
  const updatePolicy = useUpdateOrganizationPolicy();
  const mutation = policy ? updatePolicy : createPolicy;

  const claimed = new Set(existingPolicies.filter((p) => p.id !== policy?.id).map((p) => p.asset_type_id ?? ""));
  const targets: Target[] = [ORGANIZATION_WIDE_DEFAULT, ...(assetTypes ?? []).map((t) => ({ id: t.id, name: t.name }))].filter(
    (t) => !claimed.has(t.id),
  );
  const selectedTarget = targets.find((t) => t.id === (form.asset_type_id ?? "")) ?? null;
  const targetError = !selectedTarget ? "Choose what this policy applies to." : undefined;

  const errors = Object.fromEntries(POLICY_FIELDS.map((f) => [f.key, policyValueError(f, form[f.key])]));
  const hasErrors = Boolean(targetError) || Object.values(errors).some(Boolean);

  const handleSubmit = () => {
    setSubmitted(true);
    if (hasErrors || mutation.isPending) return;
    const name = selectedTarget?.name ?? "Policy";
    if (policy) {
      updatePolicy.mutate({ id: policy.id, payload: form }, { onSuccess: () => onSaved(name) });
    } else {
      createPolicy.mutate(form, { onSuccess: () => onSaved(name) });
    }
  };

  return (
    <Modal
      open
      size="lg"
      modalLabel="Organisation Settings"
      modalHeading={policy ? `Edit policy: ${policy.asset_type_name ?? "Organisation-wide default"}` : "Add policy"}
      primaryButtonText={mutation.isPending ? "Saving…" : policy ? "Save changes" : "Add policy"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={mutation.isPending}
      preventCloseOnClickOutside
      hasScrollingContent
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <div className="cg-modal-form">
        {mutation.isError && (
          <InlineNotification
            kind="error"
            title="Could not save policy"
            subtitle={getErrorMessage(mutation.error, "Something went wrong. Please try again.")}
            hideCloseButton
            lowContrast
            style={{ maxWidth: "100%" }}
          />
        )}

        {!policy && (
          <>
            <ComboBox<Target>
              id="policy-asset-type"
              titleText="Applies to"
              helperText={
                defaultPolicy
                  ? "Asset types that already have a policy aren't listed. Values start from the organisation default."
                  : "Start with the organisation-wide default; add asset-type overrides afterwards."
              }
              placeholder="Type to search asset types…"
              autoAlign
              items={targets}
              itemToString={(item) => item?.name ?? ""}
              selectedItem={selectedTarget}
              shouldFilterItem={comboBoxFilter(selectedTarget)}
              onChange={({ selectedItem }) => setForm((f) => ({ ...f, asset_type_id: selectedItem ? selectedItem.id || null : f.asset_type_id }))}
              invalid={submitted && Boolean(targetError)}
              invalidText={targetError}
            />
          </>
        )}

        {POLICY_GROUPS.map((group) => (
          <fieldset key={group.title} className="cg-policy-form__group">
            <legend className="cg-policy__group-title">{group.title}</legend>
            <div className="cg-modal-form__row">
              {group.fields.map((field) => (
                <NumberInput
                  key={field.key}
                  id={`policy-${field.key}`}
                  label={field.unit ? `${field.label} (${field.unit})` : field.label}
                  helperText={field.purpose}
                  hideSteppers
                  step={field.step}
                  min={field.min}
                  max={field.max}
                  value={form[field.key]}
                  onChange={(_e, { value }) => setForm((f) => ({ ...f, [field.key]: Number(value) }))}
                  invalid={Boolean(errors[field.key])}
                  invalidText={errors[field.key]}
                />
              ))}
            </div>
          </fieldset>
        ))}
      </div>
    </Modal>
  );
}
