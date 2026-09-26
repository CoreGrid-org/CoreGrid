import type { OrganizationPolicy, SaveOrganizationPolicyRequest } from "../api/orgConfig";

export type PolicyFieldKey = keyof Omit<SaveOrganizationPolicyRequest, "asset_type_id">;

export interface PolicyField {
  key: PolicyFieldKey;
  label: string;
  /** What the value controls: context the backend doesn't send. */
  purpose: string;
  unit?: string;
  step: number;
  /** Mirrors SaveOrganizationPolicyRequest's [Range] (backend/Features/OrgConfig/DTOs). */
  min: number;
  max: number;
  format: (value: number) => string;
}

export interface PolicyFieldGroup {
  title: string;
  fields: PolicyField[];
}

const plural = (value: number, one: string, many: string) => `${value} ${value === 1 ? one : many}`;

export const POLICY_GROUPS: PolicyFieldGroup[] = [
  {
    title: "Maintenance & repair",
    fields: [
      {
        key: "repair_to_replace_cost_threshold",
        label: "Repair-to-replace cost threshold",
        purpose: "Above this repair-cost ratio, Budget Analysis favours replacing over repairing.",
        step: 0.01,
        min: 0,
        max: 100,
        format: (v) => v.toFixed(2),
      },
      {
        key: "max_acceptable_failure_frequency",
        label: "Maximum acceptable failure frequency",
        purpose: "Failures per year before the Maintenance Analysis Agent flags a worsening trend.",
        unit: "per year",
        step: 1,
        min: 0,
        max: 100,
        format: (v) => `${v} / year`,
      },
      {
        key: "cost_variance_tolerance_percent",
        label: "Cost variance tolerance",
        purpose: "Completing maintenance above this overrun needs a recorded justification.",
        unit: "%",
        step: 1,
        min: 0,
        max: 100,
        format: (v) => `${v}%`,
      },
    ],
  },
  {
    title: "Disposal",
    fields: [
      {
        key: "minimum_service_life_years",
        label: "Minimum service life before disposal",
        purpose: "An asset must have been in service at least this long to be recommended for disposal.",
        unit: "years",
        step: 1,
        min: 0,
        max: 100,
        format: (v) => plural(v, "year", "years"),
      },
      {
        key: "valuation_validity_window_days",
        label: "Valuation validity window",
        purpose: "A disposal valuation older than this must be redone before approval.",
        unit: "days",
        step: 1,
        min: 0,
        max: 3650,
        format: (v) => plural(v, "day", "days"),
      },
    ],
  },
  {
    title: "Workflows & approvals",
    fields: [
      {
        key: "confidence_floor",
        label: "AI confidence floor",
        purpose: "Recommendations below this confidence always go to a person for review.",
        step: 0.01,
        min: 0,
        max: 1,
        format: (v) => `${Math.round(v * 100)}%`,
      },
      {
        key: "approval_overdue_period_hours",
        label: "Approval overdue period",
        purpose: "A workflow waiting for approval longer than this is shown as overdue.",
        unit: "hours",
        step: 1,
        min: 0,
        max: 8760,
        format: (v) => plural(v, "hour", "hours"),
      },
      {
        key: "outstanding_transfer_days",
        label: "Outstanding transfer threshold",
        purpose: "An approved transfer not yet confirmed as received is flagged after this.",
        unit: "days",
        step: 1,
        min: 0,
        max: 3650,
        format: (v) => plural(v, "day", "days"),
      },
    ],
  },
];

export const POLICY_FIELDS: PolicyField[] = POLICY_GROUPS.flatMap((g) => g.fields);

export const POLICY_DEFAULTS: SaveOrganizationPolicyRequest = {
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

/** An error message for an out-of-range value, or undefined when it's fine. */
export function policyValueError(field: PolicyField, value: number): string | undefined {
  if (!Number.isFinite(value)) return "Enter a number.";
  if (value < field.min || value > field.max) return `Must be between ${field.min} and ${field.max}.`;
  return undefined;
}

export function policyToPayload(p: OrganizationPolicy): SaveOrganizationPolicyRequest {
  const payload: SaveOrganizationPolicyRequest = { ...POLICY_DEFAULTS, asset_type_id: p.asset_type_id };
  for (const field of POLICY_FIELDS) payload[field.key] = p[field.key];
  return payload;
}
