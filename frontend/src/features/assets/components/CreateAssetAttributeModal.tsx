import { useState } from "react";
import { Modal, TextInput, NumberInput, Select, SelectItem, Checkbox, InlineNotification } from "@carbon/react";
import { useCreateAssetAttributeDefinition } from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { ASSET_ATTRIBUTE_DATA_TYPES, type AssetAttributeDefinition, type AssetAttributeDataType } from "../types/asset";

interface CreateAssetAttributeModalProps {
  assetTypeId: string;
  assetTypeName: string;
  onClose: () => void;
  onCreated: (definition: AssetAttributeDefinition) => void;
}

// POST /api/asset-types/{id}/attributes.
export default function CreateAssetAttributeModal({
  assetTypeId,
  assetTypeName,
  onClose,
  onCreated,
}: CreateAssetAttributeModalProps) {
  const createAttribute = useCreateAssetAttributeDefinition();

  const [name, setName] = useState("");
  const [dataType, setDataType] = useState<AssetAttributeDataType>("TEXT");
  const [isRequired, setIsRequired] = useState(false);
  const [selectOptions, setSelectOptions] = useState("");
  const [validationRule, setValidationRule] = useState("");
  const [displayOrder, setDisplayOrder] = useState<number | "">("");

  const options = selectOptions
    .split(",")
    .map((o) => o.trim())
    .filter((o) => o.length > 0);

  const canSubmit = name.trim().length > 0 && (dataType !== "SELECT" || options.length > 0);

  const handleSubmit = () => {
    if (!canSubmit || createAttribute.isPending) return;
    createAttribute.mutate(
      {
        assetTypeId,
        payload: {
          name: name.trim(),
          data_type: dataType,
          is_required: isRequired,
          validation_rule: validationRule.trim() || null,
          select_options: dataType === "SELECT" ? options : null,
          display_order: displayOrder === "" ? null : displayOrder,
        },
      },
      { onSuccess: onCreated },
    );
  };

  return (
    <Modal
      open
      modalLabel={`Asset Config · ${assetTypeName}`}
      modalHeading="New attribute"
      primaryButtonText={createAttribute.isPending ? "Creating…" : "Create attribute"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || createAttribute.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {createAttribute.isError && (
        <InlineNotification
          kind="error"
          title="Could not create attribute"
          subtitle={getErrorMessage(createAttribute.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput
          id="create-attribute-name"
          labelText="Field label"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <Select
          id="create-attribute-data-type"
          labelText="Data type"
          value={dataType}
          onChange={(e) => setDataType(e.target.value as AssetAttributeDataType)}
        >
          {ASSET_ATTRIBUTE_DATA_TYPES.map((t) => (
            <SelectItem key={t} value={t} text={t} />
          ))}
        </Select>
        {dataType === "SELECT" && (
          <TextInput
            id="create-attribute-options"
            labelText="Options"
            helperText="Comma-separated, e.g. Black, Grey, Blue"
            value={selectOptions}
            onChange={(e) => setSelectOptions(e.target.value)}
            invalid={options.length === 0}
            invalidText="At least one option is required for a SELECT attribute."
          />
        )}
        <Checkbox
          id="create-attribute-required"
          labelText="Required"
          checked={isRequired}
          onChange={(_, { checked }) => setIsRequired(checked)}
        />
        <TextInput
          id="create-attribute-validation-rule"
          labelText="Validation rule"
          helperText="Optional"
          value={validationRule}
          onChange={(e) => setValidationRule(e.target.value)}
        />
        <NumberInput
          id="create-attribute-display-order"
          label="Display order"
          helperText="Optional — defaults to the end of the list"
          min={1}
          value={displayOrder}
          allowEmpty
          onChange={(_, { value }) => setDisplayOrder(value === "" ? "" : Number(value))}
        />
      </div>
    </Modal>
  );
}
