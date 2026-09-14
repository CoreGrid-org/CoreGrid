import { useState } from "react";
import { Tag, Button, InlineNotification, NumberInput, Accordion, AccordionItem } from "@carbon/react";
import { Add, Edit, Checkmark, Close } from "@carbon/icons-react";
import { useOrganizationPolicies, useUpdateOrganizationPolicy } from "../hooks/useOrgConfig";
import PolicyModal from "./PolicyModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { OrganizationPolicy, SaveOrganizationPolicyRequest } from "../api/orgConfig";

type PolicyFieldKey = keyof Omit<OrganizationPolicy, "id" | "asset_type_id" | "asset_type_name">;

// FR-015 field descriptions — genuinely useful context the backend doesn't
// (and shouldn't) send over the wire; keyed to OrganizationPolicy's fields.
const POLICY_LABELS: Record<PolicyFieldKey, { label: string; purpose: string; format: (v: number) => string; step: number }> = {
  repair_to_replace_cost_threshold: {
    label: "Repair-to-replace cost threshold",
    purpose: "Above this ratio, Budget Analysis favours REPLACE over REPAIR.",
    format: (v) => v.toFixed(2),
    step: 0.01,
  },
  minimum_service_life_years: {
    label: "Minimum service life before disposal",
    purpose: "A disposal recommendation requires elapsed service life at or above this.",
    format: (v) => `${v} years`,
    step: 1,
  },
  max_acceptable_failure_frequency: {
    label: "Maximum acceptable failure frequency",
    purpose: "Feeds the Maintenance Analysis Agent's cost-trend assessment.",
    format: (v) => `${v} / year`,
    step: 1,
  },
  valuation_validity_window_days: {
    label: "Valuation validity window",
    purpose: "A disposal valuation older than this forces NEEDS_REVISION.",
    format: (v) => `${v} days`,
    step: 1,
  },
  confidence_floor: {
    label: "Confidence floor",
    purpose: "Below this, human review is forced regardless of the recommended action.",
    format: (v) => v.toFixed(2),
    step: 0.01,
  },
  cost_variance_tolerance_percent: {
    label: "Cost variance tolerance",
    purpose: "Maintenance completion is rejected above this without a recorded justification.",
    format: (v) => `${v}%`,
    step: 1,
  },
  outstanding_transfer_days: {
    label: "Outstanding transfer threshold",
    purpose: "An approved but unconfirmed transfer is flagged on the dashboard past this.",
    format: (v) => `${v} days`,
    step: 1,
  },
  approval_overdue_period_hours: {
    label: "Approval overdue period",
    purpose: "A workflow awaiting approval past this is surfaced as overdue.",
    format: (v) => `${v} hours`,
    step: 1,
  },
};

function policyToPayload(p: OrganizationPolicy): SaveOrganizationPolicyRequest {
  return {
    asset_type_id: p.asset_type_id,
    repair_to_replace_cost_threshold: p.repair_to_replace_cost_threshold,
    minimum_service_life_years: p.minimum_service_life_years,
    max_acceptable_failure_frequency: p.max_acceptable_failure_frequency,
    valuation_validity_window_days: p.valuation_validity_window_days,
    confidence_floor: p.confidence_floor,
    cost_variance_tolerance_percent: p.cost_variance_tolerance_percent,
    outstanding_transfer_days: p.outstanding_transfer_days,
    approval_overdue_period_hours: p.approval_overdue_period_hours,
  };
}

export default function PolicyParametersPanel() {
  const policies = useOrganizationPolicies();
  const updatePolicyField = useUpdateOrganizationPolicy();
  const [policyModal, setPolicyModal] = useState<{ policy?: OrganizationPolicy } | null>(null);
  const [editingField, setEditingField] = useState<{ policyId: string; key: PolicyFieldKey; value: number } | null>(null);

  const saveEditingField = () => {
    if (!editingField || updatePolicyField.isPending) return;
    const policy = policies.data?.find((p) => p.id === editingField.policyId);
    if (!policy) return;
    updatePolicyField.mutate(
      { id: policy.id, payload: { ...policyToPayload(policy), [editingField.key]: editingField.value } },
      {
        onSuccess: () => {
          setEditingField(null);
          policies.refetch();
        },
      },
    );
  };

  return (
    <>
      <div className="cg-section">
        <div className="cg-section__header">
          <p className="cg-section__title">Policy Parameters</p>
          <Button kind="ghost" size="sm" renderIcon={Add} onClick={() => setPolicyModal({})}>
            Add policy
          </Button>
        </div>

        {policies.isError && (
          <InlineNotification
            kind="error"
            title="Could not load policy parameters"
            subtitle={getErrorMessage(policies.error, "Something went wrong. Please try again.")}
            lowContrast
            hideCloseButton
            className="cg-panel-notification cg-panel-notification--inset"
          />
        )}

        {policies.isLoading ? (
          <div className="cg-placeholder">
            <p>Loading policy parameters…</p>
          </div>
        ) : policies.data && policies.data.length > 0 ? (
          <Accordion>
            {policies.data.map((p) => (
              <AccordionItem key={p.id} title={p.asset_type_name ?? "Organisation-wide default"}>
                <div className="cg-row-actions cg-row-actions--full">
                  <Button kind="ghost" size="sm" onClick={() => setPolicyModal({ policy: p })}>
                    <Edit size={16} />
                    &nbsp;Edit all
                  </Button>
                </div>
                {(Object.keys(POLICY_LABELS) as PolicyFieldKey[]).map((key) => {
                  const isEditingThis = editingField?.policyId === p.id && editingField.key === key;
                  return (
                    <div key={key} className="cg-policy-row">
                      <div>
                        <p className="cg-policy-row__label">{POLICY_LABELS[key].label}</p>
                        <p className="cg-table__muted cg-policy-row__purpose">{POLICY_LABELS[key].purpose}</p>
                      </div>
                      {isEditingThis ? (
                        <div className="cg-policy-row__edit">
                          <NumberInput
                            id={`policy-field-${p.id}-${key}`}
                            size="sm"
                            hideLabel
                            label={POLICY_LABELS[key].label}
                            step={POLICY_LABELS[key].step}
                            value={editingField.value}
                            disabled={updatePolicyField.isPending}
                            onChange={(_e, { value }) => setEditingField({ ...editingField, value: Number(value) })}
                            style={{ width: "8rem" }}
                          />
                          <Button
                            kind="ghost"
                            size="sm"
                            iconDescription="Save"
                            hasIconOnly
                            disabled={updatePolicyField.isPending}
                            onClick={saveEditingField}
                            renderIcon={Checkmark}
                          />
                          <Button
                            kind="ghost"
                            size="sm"
                            iconDescription="Cancel"
                            hasIconOnly
                            disabled={updatePolicyField.isPending}
                            onClick={() => setEditingField(null)}
                            renderIcon={Close}
                          />
                        </div>
                      ) : (
                        <Tag
                          type="high-contrast"
                          size="lg"
                          className="cg-policy-row__value"
                          onClick={() => setEditingField({ policyId: p.id, key, value: p[key] })}
                        >
                          {POLICY_LABELS[key].format(p[key])}
                        </Tag>
                      )}
                    </div>
                  );
                })}
              </AccordionItem>
            ))}
          </Accordion>
        ) : (
          <div className="cg-placeholder">
            <p>No policies configured yet. Add the organisation-wide default to get started.</p>
          </div>
        )}
      </div>
      {updatePolicyField.isError && (
        <InlineNotification
          kind="error"
          title="Could not save policy parameter"
          subtitle={getErrorMessage(updatePolicyField.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          className="cg-panel-notification"
        />
      )}

      {policyModal && (
        <PolicyModal
          policy={policyModal.policy}
          onClose={() => setPolicyModal(null)}
          onSaved={() => {
            setPolicyModal(null);
            policies.refetch();
          }}
        />
      )}
    </>
  );
}
