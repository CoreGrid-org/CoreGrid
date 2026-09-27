import { useState } from "react";
import { Modal, TextInput, ComboBox, InlineNotification } from "@carbon/react";
import { useCreateCampaign } from "../hooks/useCampaigns";
import { useDepartments, useLocations, useAssetCategories, useAssetTypes } from "@/features/assets/hooks/useAssets";
import type { AssetCategory, AssetType, Department, Location } from "@/features/assets/types/asset";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import type { Campaign } from "../api/campaigns";
import DateRangeFilter from "@/shared/components/DateRangeFilter";
import { CAMPAIGN_NAME_MAX, hasCampaignErrors, validateCampaign } from "../lib/campaignValidation";

interface CreateCampaignModalProps {
  onClose: () => void;
  onCreated: (campaign: Campaign) => void;
}

//  an Auditor/Administrator scopes a campaign by department,
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

  const selectedDepartment = departments?.find((d) => d.id === departmentId) ?? null;
  const selectedLocation = locations?.find((l) => l.id === locationId) ?? null;
  const selectedCategory = categories?.find((c) => c.id === categoryId) ?? null;
  const typeOptions = (types ?? []).filter((t) => !categoryId || t.asset_category_id === categoryId);
  const selectedType = typeOptions.find((t) => t.id === typeId) ?? null;

  const [submitted, setSubmitted] = useState(false);

  const errors = validateCampaign({ name, periodStart, periodEnd });
  // Required-field messages wait for a submit attempt; wrong values show straight away.
  const show = (message: string | undefined, touched: boolean) => (submitted || touched ? message : undefined);

  const handleSubmit = () => {
    setSubmitted(true);
    if (hasCampaignErrors(errors) || createCampaign.isPending) return;
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
      primaryButtonDisabled={createCampaign.isPending}
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
        <TextInput
          id="campaign-name"
          labelText="Campaign name"
          placeholder="e.g. Q4 2026 Radiology verification"
          value={name}
          maxLength={CAMPAIGN_NAME_MAX}
          onChange={(e) => setName(e.target.value)}
          invalid={Boolean(show(errors.name, false))}
          invalidText={errors.name}
        />

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <DateRangeFilter
            idPrefix="campaign-period"
            fromLabel="Period start"
            toLabel="Period end"
            allowFuture
            value={{ from: periodStart, to: periodEnd }}
            onChange={(r) => {
              setPeriodStart(r.from);
              setPeriodEnd(r.to);
            }}
            errors={{
              from: show(errors.periodStart, Boolean(periodStart)),
              to: show(errors.periodEnd, Boolean(periodEnd)),
            }}
          />
        </div>

        <p className="cg-table__muted" style={{ margin: 0, fontSize: "0.8125rem" }}>
          Scope — every filter below is optional; leaving all of them unset covers the whole register.
        </p>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <ComboBox<Department>
            id="campaign-department"
            titleText="Department"
            placeholder="Any department"
            autoAlign
            items={departments ?? []}
            itemToString={(item) => item?.name ?? ""}
            selectedItem={selectedDepartment}
            shouldFilterItem={comboBoxFilter(selectedDepartment)}
            onChange={({ selectedItem }) => {
              setDepartmentId(selectedItem?.id ?? "");
              setLocationId("");
            }}
          />
          <ComboBox<Location>
            id="campaign-location"
            titleText="Location"
            placeholder={departmentId ? "Any location" : "Choose a department first"}
            disabled={!departmentId}
            autoAlign
            items={locations ?? []}
            itemToString={(item) => item?.name ?? ""}
            selectedItem={selectedLocation}
            shouldFilterItem={comboBoxFilter(selectedLocation)}
            onChange={({ selectedItem }) => setLocationId(selectedItem?.id ?? "")}
          />
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <ComboBox<AssetCategory>
            id="campaign-category"
            titleText="Asset category"
            placeholder="Any category"
            autoAlign
            items={categories ?? []}
            itemToString={(item) => item?.name ?? ""}
            selectedItem={selectedCategory}
            shouldFilterItem={comboBoxFilter(selectedCategory)}
            onChange={({ selectedItem }) => {
              setCategoryId(selectedItem?.id ?? "");
              setTypeId("");
            }}
          />
          <ComboBox<AssetType>
            id="campaign-type"
            titleText="Asset type"
            placeholder="Any type"
            autoAlign
            items={typeOptions}
            itemToString={(item) => item?.name ?? ""}
            selectedItem={selectedType}
            shouldFilterItem={comboBoxFilter(selectedType)}
            onChange={({ selectedItem }) => setTypeId(selectedItem?.id ?? "")}
          />
        </div>
      </div>
    </Modal>
  );
}
