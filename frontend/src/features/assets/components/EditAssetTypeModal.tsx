import { useState } from "react";
import { Modal, TextInput, NumberInput, ComboBox, InlineNotification } from "@carbon/react";
import { useAssetCategories, useUpdateAssetType } from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { AssetCategory, AssetType } from "../types/asset";

interface EditAssetTypeModalProps {
  assetType: AssetType;
  onClose: () => void;
  onUpdated: (assetType: AssetType) => void;
}

// PUT /api/asset-types/{id}.
export default function EditAssetTypeModal({ assetType, onClose, onUpdated }: EditAssetTypeModalProps) {
  const { data: categories } = useAssetCategories();
  const updateAssetType = useUpdateAssetType();

  const [code, setCode] = useState(assetType.code);
  const [name, setName] = useState(assetType.name);
  const [categoryId, setCategoryId] = useState(assetType.asset_category_id);
  const [usefulLifeYears, setUsefulLifeYears] = useState(assetType.useful_life_years);
  const [maintenanceIntervalDays, setMaintenanceIntervalDays] = useState<number | "">(
    assetType.default_maintenance_interval_days ?? "",
  );

  const canSubmit =
    code.trim().length > 0 &&
    code.trim().length <= 20 &&
    name.trim().length > 0 &&
    categoryId.length > 0 &&
    usefulLifeYears > 0;

  const handleSubmit = () => {
    if (!canSubmit || updateAssetType.isPending) return;
    updateAssetType.mutate(
      {
        id: assetType.id,
        payload: {
          code: code.trim(),
          name: name.trim(),
          asset_category_id: categoryId,
          useful_life_years: usefulLifeYears,
          default_maintenance_interval_days: maintenanceIntervalDays === "" ? null : maintenanceIntervalDays,
        },
      },
      { onSuccess: onUpdated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Asset Config"
      modalHeading="Edit type"
      primaryButtonText={updateAssetType.isPending ? "Saving…" : "Save changes"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || updateAssetType.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {updateAssetType.isError && (
        <InlineNotification
          kind="error"
          title="Could not update type"
          subtitle={getErrorMessage(updateAssetType.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <ComboBox<AssetCategory>
          id="edit-type-category"
          titleText="Category"
          placeholder="Search categories…"
          items={categories ?? []}
          itemToString={(item) => (item ? `${item.name} (${item.code})` : "")}
          selectedItem={categories?.find((c) => c.id === categoryId) ?? null}
          onChange={({ selectedItem }) => setCategoryId(selectedItem?.id ?? "")}
        />
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <TextInput
            id="edit-type-code"
            labelText="Code"
            helperText="Used in generated asset codes, e.g. LAP."
            value={code}
            onChange={(e) => setCode(e.target.value)}
            invalid={code.trim().length > 20}
            invalidText="Code cannot be longer than 20 characters."
          />
          <TextInput
            id="edit-type-name"
            labelText="Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <NumberInput
            id="edit-type-useful-life"
            label="Useful life (years)"
            min={1}
            value={usefulLifeYears}
            onChange={(_, { value }) => setUsefulLifeYears(typeof value === "number" ? value : 1)}
          />
          <NumberInput
            id="edit-type-maintenance-interval"
            label="Maintenance interval (days)"
            helperText="Optional"
            min={1}
            value={maintenanceIntervalDays}
            allowEmpty
            onChange={(_, { value }) => setMaintenanceIntervalDays(value === "" ? "" : Number(value))}
          />
        </div>
      </div>
    </Modal>
  );
}
