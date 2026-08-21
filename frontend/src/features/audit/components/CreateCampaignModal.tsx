import { useState } from "react";
import { Modal, TextInput, Select, SelectItem, DatePicker, DatePickerInput, InlineNotification } from "@carbon/react";
import { useCreateCampaign } from "../hooks/useCampaigns";
import { useDepartments, useLocations, useAssetCategories, useAssetTypes } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Campaign } from "../api/campaigns";

interface CreateCampaignModalProps {
  onClose: () => void;
  onCreated: (campaign: Campaign) => void;
}

// yyyy-MM-dd from local date parts — avoids the UTC-shift a plain
// toISOString() would introduce for a DateOnly-typed field on the backend.
function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

// FR-056: an Auditor/Administrator scopes a campaign by department,
// location, category and/or asset type — every filter is optional and
// combined with AND; leaving all of them unset scopes the whole register.
export default function CreateCampaignModal({ onClose, onCreated }: CreateCampaignModalProps) {
  const createCampaign = useCreateCampaign();

  const [name, setName] = useState("");
  const [periodStart, setPeriodStart] = useState<string>();
  const [periodEnd, setPeriodEnd] = useState<string>();
  const [departmentId, setDepartmentId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [typeId, setTypeId] = useState("");

  const { data: departments } = useDepartments();
  const { data: locations } = useLocations(departmentId || undefined);
  const { data: categories } = useAssetCategories();
  const { data: types } = useAssetTypes();

  const canSubmit = name.trim().length > 0 && !!periodStart && !!periodEnd && periodStart <= periodEnd;

  const handleSubmit = () => {
    if (!canSubmit || createCampaign.isPending) return;
    createCampaign.mutate(
      {
        name: name.trim(),
        period_start: periodStart!,
        period_end: periodEnd!,
        scope_department_id: departmentId || null,
        scope_location_id: locationId || null,
        scope_asset_category_id: categoryId || null,
        scope_asset_type_id: typeId || null,
      },
      { onSuccess: onCreated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Audit & Compliance"
      modalHeading="New verification campaign"
      primaryButtonText={createCampaign.isPending ? "Creating…" : "Create campaign"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || createCampaign.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {createCampaign.isError && (
        <InlineNotification
          kind="error"
          title="Could not create the campaign"
          subtitle={getErrorMessage(createCampaign.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput id="campaign-name" labelText="Campaign name" value={name} onChange={(e) => setName(e.target.value)} />

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setPeriodStart(date ? toDateOnly(date) : undefined)}>
            <DatePickerInput id="campaign-period-start" labelText="Period start" placeholder="yyyy-mm-dd" />
          </DatePicker>
          <DatePicker datePickerType="single" dateFormat="Y-m-d" onChange={([date]) => setPeriodEnd(date ? toDateOnly(date) : undefined)}>
            <DatePickerInput id="campaign-period-end" labelText="Period end" placeholder="yyyy-mm-dd" />
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

        <p className="cg-table__muted" style={{ margin: 0, fontSize: "0.8125rem" }}>
          Scope — every filter below is optional; leaving all of them unset covers the whole register.
        </p>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <Select
            id="campaign-department"
            labelText="Department"
            value={departmentId}
            onChange={(e) => {
              setDepartmentId(e.target.value);
              setLocationId("");
            }}
          >
            <SelectItem value="" text="Any department" />
            {departments?.map((d) => <SelectItem key={d.id} value={d.id} text={d.name} />)}
          </Select>
          <Select
            id="campaign-location"
            labelText="Location"
            value={locationId}
            onChange={(e) => setLocationId(e.target.value)}
            disabled={!departmentId}
          >
            <SelectItem value="" text={departmentId ? "Any location" : "Choose a department first"} />
            {locations?.map((l) => <SelectItem key={l.id} value={l.id} text={l.name} />)}
          </Select>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <Select
            id="campaign-category"
            labelText="Asset category"
            value={categoryId}
            onChange={(e) => {
              setCategoryId(e.target.value);
              setTypeId("");
            }}
          >
            <SelectItem value="" text="Any category" />
            {categories?.map((c) => <SelectItem key={c.id} value={c.id} text={c.name} />)}
          </Select>
          <Select id="campaign-type" labelText="Asset type" value={typeId} onChange={(e) => setTypeId(e.target.value)}>
            <SelectItem value="" text="Any type" />
            {types
              ?.filter((t) => !categoryId || t.asset_category_id === categoryId)
              .map((t) => <SelectItem key={t.id} value={t.id} text={t.name} />)}
          </Select>
        </div>
      </div>
    </Modal>
  );
}
