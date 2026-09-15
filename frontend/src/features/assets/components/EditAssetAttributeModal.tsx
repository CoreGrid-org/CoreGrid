import { useState, useRef } from "react";
import {
  Modal,
  TextInput,
  NumberInput,
  Select,
  SelectItem,
  Checkbox,
  InlineNotification,
  Button,
} from "@carbon/react";
import { Add, TrashCan } from "@carbon/icons-react";
import { useUpdateAssetAttributeDefinition } from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import {
  ASSET_ATTRIBUTE_DATA_TYPES,
  type AssetAttributeDefinition,
  type AssetAttributeDataType,
} from "../types/asset";

interface EditAssetAttributeModalProps {
  assetTypeId: string;
  assetTypeName: string;
  attribute: AssetAttributeDefinition;
  onClose: () => void;
  onUpdated: (definition: AssetAttributeDefinition) => void;
}

interface ValidationRuleFields {
  min: string;
  max: string;
  minLength: string;
  maxLength: string;
  minDate: string;
  maxDate: string;
}

const emptyRuleFields = (): ValidationRuleFields => ({
  min: "",
  max: "",
  minLength: "",
  maxLength: "",
  minDate: "",
  maxDate: "",
});

function parseValidationRule(rule: string | null | undefined): ValidationRuleFields {
  const fields = emptyRuleFields();
  if (!rule) return fields;
  rule.split(",").forEach((segment) => {
    const colonIdx = segment.indexOf(":");
    if (colonIdx <= 0) return;
    const key = segment.slice(0, colonIdx).trim().toLowerCase();
    const val = segment.slice(colonIdx + 1).trim();
    switch (key) {
      case "min":       fields.min = val; break;
      case "max":       fields.max = val; break;
      case "minlength": fields.minLength = val; break;
      case "maxlength": fields.maxLength = val; break;
      case "mindate":   fields.minDate = val; break;
      case "maxdate":   fields.maxDate = val; break;
    }
  });
  return fields;
}

function buildValidationRule(
  dataType: AssetAttributeDataType,
  fields: ValidationRuleFields
): string | null {
  const parts: string[] = [];
  if (dataType === "NUMBER") {
    if (fields.min.trim()) parts.push(`min:${fields.min.trim()}`);
    if (fields.max.trim()) parts.push(`max:${fields.max.trim()}`);
  } else if (dataType === "TEXT") {
    if (fields.minLength.trim()) parts.push(`minLength:${fields.minLength.trim()}`);
    if (fields.maxLength.trim()) parts.push(`maxLength:${fields.maxLength.trim()}`);
  } else if (dataType === "DATE") {
    if (fields.minDate.trim()) parts.push(`minDate:${fields.minDate.trim()}`);
    if (fields.maxDate.trim()) parts.push(`maxDate:${fields.maxDate.trim()}`);
  }
  return parts.length > 0 ? parts.join(",") : null;
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
  const [displayOrder, setDisplayOrder] = useState<number | "">(attribute.display_order);
  const [ruleFields, setRuleFields] = useState<ValidationRuleFields>(
    () => parseValidationRule(attribute.validation_rule)
  );

  // SELECT options — initialised from existing attribute
  const [options, setOptions] = useState<string[]>(attribute.select_options ?? []);
  const [newOption, setNewOption] = useState("");
  const optionInputRef = useRef<HTMLInputElement>(null);

  const addOption = () => {
    const trimmed = newOption.trim();
    if (!trimmed || options.includes(trimmed)) return;
    setOptions((prev) => [...prev, trimmed]);
    setNewOption("");
    optionInputRef.current?.focus();
  };

  const removeOption = (index: number) =>
    setOptions((prev) => prev.filter((_, i) => i !== index));

  const canSubmit =
    name.trim().length > 0 &&
    (dataType !== "SELECT" || options.length > 0) &&
    displayOrder !== "";

  const setRule = (key: keyof ValidationRuleFields, value: string) =>
    setRuleFields((prev) => ({ ...prev, [key]: value }));

  const handleDataTypeChange = (newType: AssetAttributeDataType) => {
    setDataType(newType);
    setRuleFields(emptyRuleFields());
    setOptions([]);
    setNewOption("");
  };

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
          validation_rule: buildValidationRule(dataType, ruleFields),
          select_options: dataType === "SELECT" ? options : null,
          display_order: typeof displayOrder === "string" ? attribute.display_order : displayOrder,
        },
      },
      { onSuccess: onUpdated }
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
        {/* ── Name ── */}
        <TextInput
          id="edit-attribute-name"
          labelText="Field label"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />

        {/* ── Data type ── */}
        <Select
          id="edit-attribute-data-type"
          labelText="Data type"
          value={dataType}
          onChange={(e) => handleDataTypeChange(e.target.value as AssetAttributeDataType)}
        >
          {ASSET_ATTRIBUTE_DATA_TYPES.map((t) => (
            <SelectItem key={t} value={t} text={t} />
          ))}
        </Select>

        {/* ── SELECT — options builder ── */}
        {dataType === "SELECT" && (
          <div>
            <p style={{ fontSize: "0.75rem", fontWeight: 600, marginBottom: "0.5rem", color: "#525252" }}>
              Options
            </p>

            {/* existing options list */}
            {options.length > 0 && (
              <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem", marginBottom: "0.75rem" }}>
                {options.map((opt, idx) => (
                  <div
                    key={idx}
                    style={{
                      display: "inline-flex",
                      alignItems: "center",
                      gap: "0.375rem",
                      background: "#e0e0e0",
                      borderRadius: "1rem",
                      padding: "0.25rem 0.625rem",
                      fontSize: "0.8125rem",
                    }}
                  >
                    <span>{opt}</span>
                    <button
                      type="button"
                      aria-label={`Remove option ${opt}`}
                      onClick={() => removeOption(idx)}
                      style={{
                        background: "none",
                        border: "none",
                        cursor: "pointer",
                        padding: 0,
                        display: "flex",
                        alignItems: "center",
                        color: "#525252",
                      }}
                    >
                      <TrashCan size={14} />
                    </button>
                  </div>
                ))}
              </div>
            )}

            {/* add new option row */}
            <div style={{ display: "flex", gap: "0.5rem", alignItems: "flex-end" }}>
              <div style={{ flex: 1 }}>
                <TextInput
                  ref={optionInputRef}
                  id="edit-attribute-new-option"
                  labelText="New option"
                  placeholder="e.g. Blue"
                  value={newOption}
                  onChange={(e) => setNewOption(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && (e.preventDefault(), addOption())}
                  invalid={options.length === 0}
                  invalidText="At least one option is required."
                />
              </div>
              <Button
                kind="secondary"
                size="md"
                renderIcon={Add}
                iconDescription="Add option"
                onClick={addOption}
                disabled={!newOption.trim() || options.includes(newOption.trim())}
              >
                Add
              </Button>
            </div>
          </div>
        )}

        {/* ── Required ── */}
        <Checkbox
          id="edit-attribute-required"
          labelText="Required"
          checked={isRequired}
          onChange={(_, { checked }) => setIsRequired(checked)}
        />

        {/* ── NUMBER validation ── */}
        {dataType === "NUMBER" && (
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
            <TextInput
              id="edit-attribute-rule-min"
              labelText="Minimum value"
              helperText="Optional"
              value={ruleFields.min}
              onChange={(e) => setRule("min", e.target.value)}
            />
            <TextInput
              id="edit-attribute-rule-max"
              labelText="Maximum value"
              helperText="Optional"
              value={ruleFields.max}
              onChange={(e) => setRule("max", e.target.value)}
            />
          </div>
        )}

        {/* ── TEXT validation ── */}
        {dataType === "TEXT" && (
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
            <TextInput
              id="edit-attribute-rule-minlength"
              labelText="Minimum length"
              helperText="Optional — characters"
              value={ruleFields.minLength}
              onChange={(e) => setRule("minLength", e.target.value)}
            />
            <TextInput
              id="edit-attribute-rule-maxlength"
              labelText="Maximum length"
              helperText="Optional — characters"
              value={ruleFields.maxLength}
              onChange={(e) => setRule("maxLength", e.target.value)}
            />
          </div>
        )}

        {/* ── DATE validation ── */}
        {dataType === "DATE" && (
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
            <TextInput
              id="edit-attribute-rule-mindate"
              labelText="Minimum date"
              helperText="Optional — YYYY-MM-DD"
              value={ruleFields.minDate}
              onChange={(e) => setRule("minDate", e.target.value)}
            />
            <TextInput
              id="edit-attribute-rule-maxdate"
              labelText="Maximum date"
              helperText="Optional — YYYY-MM-DD"
              value={ruleFields.maxDate}
              onChange={(e) => setRule("maxDate", e.target.value)}
            />
          </div>
        )}

        {/* ── Display order ── */}
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
