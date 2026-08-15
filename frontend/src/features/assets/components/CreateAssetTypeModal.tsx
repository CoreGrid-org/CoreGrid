import { useState } from "react";
import { Modal, TextInput, NumberInput, Select, SelectItem, InlineNotification } from "@carbon/react";
import { useAssetCategories, useCreateAssetType } from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { AssetType } from "../types/asset";

interface CreateAssetTypeModalProps {
  onClose: () => void;
  onCreated: (assetType: AssetType) => void;
}

// POST /api/asset-types.
export default function CreateAssetTypeModal({ onClose, onCreated }: CreateAssetTypeModalProps) {
  const { data: categories } = useAssetCategories();
  const createAssetType = useCreateAssetType();

  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [usefulLifeYears, setUsefulLifeYears] = useState(5);
  const [maintenanceIntervalDays, setMaintenanceIntervalDays] = useState<number | "">("");

  const canSubmit =
    code.trim().length > 0 &&
    code.trim().length <= 20 &&
    name.trim().length > 0 &&
    categoryId.length > 0 &&
    usefulLifeYears > 0;

  const handleSubmit = () => {
    if (!canSubmit || createAssetType.isPending) return;
    createAssetType.mutate(
      {
        code: code.trim(),
        name: name.trim(),
        asset_category_id: categoryId,
        useful_life_years: usefulLifeYears,
        default_maintenance_interval_days: maintenanceIntervalDays === "" ? null : maintenanceIntervalDays,
      },
      { onSuccess: onCreated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Asset Config"
      modalHeading="New type"
      primaryButtonText={createAssetType.isPending ? "Creating…" : "Create type"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || createAssetType.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {createAssetType.isError && (
        <InlineNotification
          kind="error"
          title="Could not create type"
          subtitle={getErrorMessage(createAssetType.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <Select
          id="create-type-category"
          labelText="Category"
          value={categoryId}
          onChange={(e) => setCategoryId(e.target.value)}
        >
          <SelectItem value="" text="Choose a category…" />
          {categories?.map((c) => <SelectItem key={c.id} value={c.id} text={c.name} />)}
        </Select>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <TextInput
            id="create-type-code"
            labelText="Code"
            helperText="Used in generated asset codes, e.g. LAP."
            value={code}
            onChange={(e) => setCode(e.target.value)}
            invalid={code.trim().length > 20}
            invalidText="Code cannot be longer than 20 characters."
          />
          <TextInput
            id="create-type-name"
            labelText="Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <NumberInput
            id="create-type-useful-life"
            label="Useful life (years)"
            min={1}
            value={usefulLifeYears}
            onChange={(_, { value }) => setUsefulLifeYears(typeof value === "number" ? value : 1)}
          />
          <NumberInput
            id="create-type-maintenance-interval"
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
