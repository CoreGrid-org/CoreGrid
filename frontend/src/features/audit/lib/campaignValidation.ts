import { validateDateRange } from "@/shared/lib/dates";

// Matches Create/UpdateCampaignRequest's [MaxLength(200)] on the backend.
export const CAMPAIGN_NAME_MAX = 200;

export interface CampaignFields {
  name: string;
  periodStart?: string;
  periodEnd?: string;
}

export interface CampaignErrors {
  name?: string;
  periodStart?: string;
  periodEnd?: string;
}

// Same rules the backend enforces (VerificationCampaignService): a name, both
// period dates, and an end no earlier than the start. Periods may be in the
// future — campaigns are often planned ahead.
export function validateCampaign({ name, periodStart, periodEnd }: CampaignFields): CampaignErrors {
  const range = validateDateRange({ from: periodStart, to: periodEnd }, { allowFuture: true });
  const errors: CampaignErrors = {
    periodStart: range.from ?? (!periodStart ? "Choose the start date." : undefined),
    periodEnd: range.to ?? (!periodEnd ? "Choose the end date." : undefined),
  };
  if (!name.trim()) errors.name = "Give the campaign a name.";
  else if (name.trim().length > CAMPAIGN_NAME_MAX) errors.name = `Keep it to ${CAMPAIGN_NAME_MAX} characters or fewer.`;
  return errors;
}

export const hasCampaignErrors = (errors: CampaignErrors) => Object.values(errors).some(Boolean);
