import { useState } from "react";
import {
  Modal,
  TextInput,
  Select,
  SelectItem,
  InlineNotification,
} from "@carbon/react";
import { useUpdateCampaign } from "../hooks/useCampaigns";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { campaignScopeLabel } from "../lib/campaignScopeLabel";
import type { Campaign, CampaignStatus } from "../api/campaigns";
import DateRangeFilter from "@/shared/components/DateRangeFilter";
import { CAMPAIGN_NAME_MAX, hasCampaignErrors, validateCampaign } from "../lib/campaignValidation";

interface EditCampaignModalProps {
  campaign: Campaign;
  onClose: () => void;
  onUpdated: (updated: Campaign) => void;
}

export default function EditCampaignModal({
  campaign,
  onClose,
  onUpdated,
}: EditCampaignModalProps) {
  const updateCampaign = useUpdateCampaign();

  const [name, setName] = useState(campaign.name);
  const [periodStart, setPeriodStart] = useState<string | undefined>(campaign.period_start);
  const [periodEnd, setPeriodEnd] = useState<string | undefined>(campaign.period_end);
  const [status, setStatus] = useState<CampaignStatus>(campaign.status);

  // Everything starts filled in, so errors can show straight away.
  const errors = validateCampaign({ name, periodStart, periodEnd });

  const handleSubmit = () => {
    if (hasCampaignErrors(errors) || !periodStart || !periodEnd || updateCampaign.isPending) return;
    updateCampaign.mutate(
      {
        id: campaign.id,
        payload: {
          name: name.trim(),
          period_start: periodStart,
          period_end: periodEnd,
          status,
        },
      },
      {
        onSuccess: (res) => {
          onUpdated(res);
        },
      }
    );
  };

  return (
    <Modal
      open
      modalLabel="Audit & Compliance"
      modalHeading={`Edit campaign: ${campaign.name}`}
      primaryButtonText={updateCampaign.isPending ? "Saving…" : "Save changes"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={hasCampaignErrors(errors) || updateCampaign.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {updateCampaign.isError && (
        <InlineNotification
          kind="error"
          title="Could not update campaign"
          subtitle={getErrorMessage(updateCampaign.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput
          id="edit-campaign-name"
          labelText="Campaign name"
          value={name}
          maxLength={CAMPAIGN_NAME_MAX}
          onChange={(e) => setName(e.target.value)}
          invalid={Boolean(errors.name)}
          invalidText={errors.name}
        />

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <DateRangeFilter
            idPrefix="edit-campaign-period"
            fromLabel="Period start"
            toLabel="Period end"
            allowFuture
            value={{ from: periodStart, to: periodEnd }}
            onChange={(r) => {
              setPeriodStart(r.from);
              setPeriodEnd(r.to);
            }}
            errors={{ from: errors.periodStart, to: errors.periodEnd }}
          />
        </div>

        <Select
          id="edit-campaign-status"
          labelText="Status"
          value={status}
          onChange={(e) => setStatus(e.target.value as CampaignStatus)}
        >
          <SelectItem value="Active" text="Active" />
          <SelectItem value="Completed" text="Completed" />
          <SelectItem value="Cancelled" text="Cancelled" />
        </Select>

        <div style={{ background: "#f4f4f4", padding: "0.75rem 1rem", borderRadius: "4px" }}>
          <p style={{ fontSize: "0.8125rem", margin: 0, fontWeight: 600 }}>Campaign Scope (Fixed on creation)</p>
          <p style={{ fontSize: "0.875rem", margin: "0.25rem 0 0 0", color: "#525252" }}>
            {campaignScopeLabel(campaign)}
          </p>
        </div>
      </div>
    </Modal>
  );
}
