import { useState } from "react";
import {
  Modal,
  TextInput,
  Select,
  SelectItem,
  DatePicker,
  DatePickerInput,
  InlineNotification,
} from "@carbon/react";
import { useUpdateCampaign } from "../hooks/useCampaigns";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { campaignScopeLabel } from "../lib/campaignScopeLabel";
import type { Campaign, CampaignStatus } from "../api/campaigns";

interface EditCampaignModalProps {
  campaign: Campaign;
  onClose: () => void;
  onUpdated: (updated: Campaign) => void;
}

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export default function EditCampaignModal({
  campaign,
  onClose,
  onUpdated,
}: EditCampaignModalProps) {
  const updateCampaign = useUpdateCampaign();

  const [name, setName] = useState(campaign.name);
  const [periodStart, setPeriodStart] = useState<string>(campaign.period_start);
  const [periodEnd, setPeriodEnd] = useState<string>(campaign.period_end);
  const [status, setStatus] = useState<CampaignStatus>(campaign.status);

  const canSubmit =
    name.trim().length > 0 && !!periodStart && !!periodEnd && periodStart <= periodEnd;

  const handleSubmit = () => {
    if (!canSubmit || updateCampaign.isPending) return;
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
      modalHeading={`Edit campaign — ${campaign.name}`}
      primaryButtonText={updateCampaign.isPending ? "Saving…" : "Save changes"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || updateCampaign.isPending}
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
          onChange={(e) => setName(e.target.value)}
        />

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <DatePicker
            datePickerType="single"
            dateFormat="Y-m-d"
            value={periodStart}
            onChange={([date]) => setPeriodStart(date ? toDateOnly(date) : periodStart)}
          >
            <DatePickerInput
              id="edit-campaign-period-start"
              labelText="Period start"
              placeholder="yyyy-mm-dd"
            />
          </DatePicker>
          <DatePicker
            datePickerType="single"
            dateFormat="Y-m-d"
            value={periodEnd}
            onChange={([date]) => setPeriodEnd(date ? toDateOnly(date) : periodEnd)}
          >
            <DatePickerInput
              id="edit-campaign-period-end"
              labelText="Period end"
              placeholder="yyyy-mm-dd"
            />
          </DatePicker>
        </div>

        {periodStart && periodEnd && periodStart > periodEnd && (
          <InlineNotification
            kind="warning"
            lowContrast
            hideCloseButton
            title="Period end must be on or after period start"
            style={{ maxWidth: "100%" }}
          />
        )}

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
