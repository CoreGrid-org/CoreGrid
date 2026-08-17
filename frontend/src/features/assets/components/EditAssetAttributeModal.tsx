import { useState } from "react";
import { Modal, TextInput, NumberInput, Select, SelectItem, Checkbox, InlineNotification } from "@carbon/react";
import { useUpdateAssetAttributeDefinition } from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { ASSET_ATTRIBUTE_DATA_TYPES, type AssetAttributeDefinition, type AssetAttributeDataType } from "../types/asset";

interface EditAssetAttributeModalProps {
  assetTypeId: string;
  assetTypeName: string;
  attribute: AssetAttributeDefinition;
  onClose: () => void;
  onUpdated: (definition: AssetAttributeDefinition) => void;
}

// PUT /api/asset-types/{id}/attributes/{attributeId}.
export default function EditAssetAttributeModal({
  assetTypeId,
  assetTypeName,
  attribute,
  onClose,
  onUpdated,
}: EditAssetAttributeModalProps) {
  const updateAttribute = useUpdateAssetAttributeDefinition();

  const [name, setName] = useState(attribute.name);
  const [dataType, setDataType] = useState<AssetAttributeDataType>(attribute.data_type);
  const [isRequired, setIsRequired] = useState(attribute.is_required);
  const [selectOptions, setSelectOptions] = useState((attribute.select_options ?? []).join(", "));
  const [validationRule, setValidationRule] = useState(attribute.validation_rule ?? "");
  const [displayOrder, setDisplayOrder] = useState<number | "">(attribute.display_order);

  const options = selectOptions
    .split(",")
    .map((o) => o.trim())
    .filter((o) => o.length > 0);

  const canSubmit =
    name.trim().length > 0 && (dataType !== "SELECT" || options.length > 0) && displayOrder !== "";

  const handleSubmit = () => {
    if (!canSubmit || updateAttribute.isPending) return;
    updateAttribute.mutate(
      {
        assetTypeId,
        attributeId: attribute.id,
        payload: {
          name: name.trim(),
          data_type: dataType,
          is_required: isRequired,
          validation_rule: validationRule.trim() || null,
          select_options: dataType === "SELECT" ? options : null,
          display_order: displayOrder === "" ? attribute.display_order : displayOrder,
        },
      },
      { onSuccess: onUpdated },
    );
  };

  return (
    <Modal
      open
      modalLabel={`Asset Config · ${assetTypeName}`}
      modalHeading="Edit attribute"
      primaryButtonText={updateAttribute.isPending ? "Saving…" : "Save changes"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || updateAttribute.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {updateAttribute.isError && (
        <InlineNotification
          kind="error"
          title="Could not update attribute"
          subtitle={getErrorMessage(updateAttribute.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput
          id="edit-attribute-name"
          labelText="Field label"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <Select
          id="edit-attribute-data-type"
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
            id="edit-attribute-options"
            labelText="Options"
            helperText="Comma-separated, e.g. Black, Grey, Blue"
            value={selectOptions}
            onChange={(e) => setSelectOptions(e.target.value)}
            invalid={options.length === 0}
            invalidText="At least one option is required for a SELECT attribute."
          />
        )}
        <Checkbox
          id="edit-attribute-required"
          labelText="Required"
          checked={isRequired}
          onChange={(_, { checked }) => setIsRequired(checked)}
        />
        <TextInput
          id="edit-attribute-validation-rule"
          labelText="Validation rule"
          helperText="Optional"
          value={validationRule}
          onChange={(e) => setValidationRule(e.target.value)}
        />
        <NumberInput
          id="edit-attribute-display-order"
          label="Display order"
          min={1}
          value={displayOrder}
          allowEmpty
          onChange={(_, { value }) => setDisplayOrder(value === "" ? "" : Number(value))}
        />
      </div>
    </Modal>
  );
}
