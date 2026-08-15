import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button, TextInput, NumberInput, Select, SelectItem, Checkbox, InlineNotification } from "@carbon/react";
import {
  useAssetTypes,
  useAssetTypeAttributes,
  useCreateAsset,
  useDepartments,
  useLocations,
} from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { ASSET_CONDITIONS, type AssetCondition, type AssetAttributeValueRequest } from "../types/asset";
import { formatStatusLabel } from "@/shared/lib/statusTag";

type AttributeValue = string | number | boolean;

// POST /api/assets. The full asset code (org prefix + type code + sequence)
// and its QR payload are generated server-side on save — see
// backend/Features/Assets/Services/AssetService.cs — so this form can only
// preview the type-code portion before submitting.
export default function AssetRegisterPage() {
  const navigate = useNavigate();

  const { data: assetTypes } = useAssetTypes();
  const { data: departments } = useDepartments();

  const [assetTypeId, setAssetTypeId] = useState("");
  const [name, setName] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [condition, setCondition] = useState<AssetCondition>("NEW");
  const [acquisitionDate, setAcquisitionDate] = useState("");
  const [acquisitionCost, setAcquisitionCost] = useState<number | "">("");
  const [residualValue, setResidualValue] = useState<number | "">("");
  const [attributeValues, setAttributeValues] = useState<Record<string, AttributeValue>>({});

  const { data: locations } = useLocations(departmentId || undefined);
  const { data: attributeDefs } = useAssetTypeAttributes(assetTypeId);
  const createAsset = useCreateAsset();

  const selectedType = assetTypes?.find((t) => t.id === assetTypeId);

  // Department changed: the previously selected location may no longer belong to it.
  useEffect(() => {
    setLocationId("");
  }, [departmentId]);

  // Asset type changed: previous attribute values belonged to a different type's fields.
  useEffect(() => {
    setAttributeValues({});
  }, [assetTypeId]);

  const requiredAttributesFilled = (attributeDefs ?? []).every((def) => {
    if (!def.is_required) return true;
    const value = attributeValues[def.id];
    return value !== undefined && value !== "";
  });

  const canSubmit =
    assetTypeId.length > 0 &&
    name.trim().length > 0 &&
    departmentId.length > 0 &&
    locationId.length > 0 &&
    acquisitionDate.length > 0 &&
    acquisitionCost !== "" &&
    acquisitionCost >= 0 &&
    requiredAttributesFilled;

  const handleSubmit = () => {
    if (!canSubmit || createAsset.isPending) return;

    const attributes: AssetAttributeValueRequest[] = (attributeDefs ?? [])
      .map((def): AssetAttributeValueRequest | null => {
        const value = attributeValues[def.id];
        if (value === undefined || value === "") return null;

        const base: AssetAttributeValueRequest = {
          asset_attribute_definition_id: def.id,
          value_text: null,
          value_number: null,
          value_date: null,
          value_boolean: null,
        };

        switch (def.data_type) {
          case "NUMBER":
            return { ...base, value_number: Number(value) };
          case "DATE":
            return { ...base, value_date: String(value) };
          case "BOOLEAN":
            return { ...base, value_boolean: Boolean(value) };
          default:
            return { ...base, value_text: String(value) };
        }
      })
      .filter((v): v is AssetAttributeValueRequest => v !== null);

    createAsset.mutate(
      {
        asset_type_id: assetTypeId,
        department_id: departmentId,
        location_id: locationId,
        name: name.trim(),
        acquisition_date: acquisitionDate,
        acquisition_cost: acquisitionCost,
        residual_value: residualValue === "" ? 0 : residualValue,
        condition,
        attributes,
      },
      {
        onSuccess: (asset) => {
          navigate("/admin/assets", { state: { openAssetId: asset.id } });
        },
      },
    );
  };

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Register new asset</h1>
          <p className="cg-page__subtitle">
            Attribute fields appear once a type is chosen — the form renders itself from that type's definitions.
          </p>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button kind="secondary" onClick={() => navigate("/admin/assets")}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!canSubmit || createAsset.isPending}>
            {createAsset.isPending ? "Saving…" : "Save asset"}
          </Button>
        </div>
      </div>

      {createAsset.isError && (
        <InlineNotification
          kind="error"
          title="Could not create asset"
          subtitle={getErrorMessage(createAsset.error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div style={{ display: "flex", gap: "1.5rem", alignItems: "flex-start", flexWrap: "wrap" }}>
        <div style={{ flex: "1 1 32rem", minWidth: "24rem", display: "flex", flexDirection: "column", gap: "1.5rem" }}>
          <div className="cg-section" style={{ margin: 0 }}>
            <p className="cg-section__title">Basic details</p>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", marginTop: "1rem" }}>
              <Select
                id="register-asset-type"
                labelText="Asset type"
                value={assetTypeId}
                onChange={(e) => setAssetTypeId(e.target.value)}
              >
                <SelectItem value="" text="Choose a type…" />
                {assetTypes?.map((t) => <SelectItem key={t.id} value={t.id} text={t.name} />)}
              </Select>
              <TextInput
                id="register-asset-name"
                labelText="Asset name"
                placeholder="e.g. Dell Latitude 5420"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
              <Select
                id="register-asset-department"
                labelText="Department"
                value={departmentId}
                onChange={(e) => setDepartmentId(e.target.value)}
              >
                <SelectItem value="" text="Choose a department…" />
                {departments?.map((d) => <SelectItem key={d.id} value={d.id} text={d.name} />)}
              </Select>
              <Select
                id="register-asset-location"
                labelText="Location"
                value={locationId}
                disabled={!departmentId}
                onChange={(e) => setLocationId(e.target.value)}
              >
                <SelectItem
                  value=""
                  text={departmentId ? "Choose a location…" : "Choose a department first"}
                />
                {locations?.map((l) => <SelectItem key={l.id} value={l.id} text={l.name} />)}
              </Select>
              <Select
                id="register-asset-condition"
                labelText="Condition"
                value={condition}
                onChange={(e) => setCondition(e.target.value as AssetCondition)}
              >
                {ASSET_CONDITIONS.map((c) => (
                  <SelectItem key={c} value={c} text={formatStatusLabel(c)} />
                ))}
              </Select>
              <TextInput
                id="register-asset-acquisition-date"
                labelText="Purchase date"
                type="date"
                value={acquisitionDate}
                onChange={(e) => setAcquisitionDate(e.target.value)}
              />
              <NumberInput
                id="register-asset-acquisition-cost"
                label="Purchase cost"
                min={0}
                value={acquisitionCost}
                allowEmpty
                onChange={(_, { value }) => setAcquisitionCost(value === "" ? "" : Number(value))}
              />
              <NumberInput
                id="register-asset-residual-value"
                label="Residual value"
                helperText="Optional — defaults to 0"
                min={0}
                value={residualValue}
                allowEmpty
                onChange={(_, { value }) => setResidualValue(value === "" ? "" : Number(value))}
              />
            </div>
          </div>

          <div className="cg-section" style={{ margin: 0 }}>
            <div style={{ display: "flex", alignItems: "baseline", gap: "0.5rem" }}>
              <p className="cg-section__title" style={{ margin: 0 }}>Attribute details</p>
              {selectedType && <span className="cg-table__muted" style={{ fontSize: "0.75rem" }}>{selectedType.name}</span>}
            </div>

            {!assetTypeId && (
              <div className="cg-placeholder" style={{ marginTop: "1rem" }}>
                <p>Pick an asset type above and the fields configured for it render here.</p>
              </div>
            )}

            {assetTypeId && attributeDefs && attributeDefs.length === 0 && (
              <div className="cg-placeholder" style={{ marginTop: "1rem" }}>
                <p>This type has no custom attributes configured.</p>
              </div>
            )}

            {assetTypeId && attributeDefs && attributeDefs.length > 0 && (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", marginTop: "1rem" }}>
                {attributeDefs.map((def) => {
                  const value = attributeValues[def.id];
                  const setValue = (v: AttributeValue) => setAttributeValues((prev) => ({ ...prev, [def.id]: v }));
                  const label = def.is_required ? `${def.name} *` : def.name;

                  if (def.data_type === "BOOLEAN") {
                    return (
                      <Checkbox
                        key={def.id}
                        id={`register-attr-${def.id}`}
                        labelText={label}
                        checked={Boolean(value)}
                        onChange={(_, { checked }) => setValue(checked)}
                      />
                    );
                  }

                  if (def.data_type === "SELECT") {
                    return (
                      <Select
                        key={def.id}
                        id={`register-attr-${def.id}`}
                        labelText={label}
                        value={typeof value === "string" ? value : ""}
                        onChange={(e) => setValue(e.target.value)}
                      >
                        <SelectItem value="" text="Choose…" />
                        {def.select_options?.map((o) => <SelectItem key={o} value={o} text={o} />)}
                      </Select>
                    );
                  }

                  if (def.data_type === "NUMBER") {
                    return (
                      <NumberInput
                        key={def.id}
                        id={`register-attr-${def.id}`}
                        label={label}
                        value={typeof value === "number" ? value : ""}
                        allowEmpty
                        onChange={(_, { value: v }) => setValue(v === "" ? "" : Number(v))}
                      />
                    );
                  }

                  if (def.data_type === "DATE") {
                    return (
                      <TextInput
                        key={def.id}
                        id={`register-attr-${def.id}`}
                        labelText={label}
                        type="date"
                        value={typeof value === "string" ? value : ""}
                        onChange={(e) => setValue(e.target.value)}
                      />
                    );
                  }

                  return (
                    <TextInput
                      key={def.id}
                      id={`register-attr-${def.id}`}
                      labelText={label}
                      value={typeof value === "string" ? value : ""}
                      onChange={(e) => setValue(e.target.value)}
                    />
                  );
                })}
              </div>
            )}
          </div>
        </div>

        <aside style={{ width: "18rem", flex: "none", display: "flex", flexDirection: "column", gap: "1rem" }}>
          <div className="cg-section" style={{ margin: 0 }}>
            <div style={{ fontSize: "0.625rem", letterSpacing: "0.05em", color: "#8d8d8d", textTransform: "uppercase" }}>
              Code preview
            </div>
            <div className="cg-table__mono" style={{ fontSize: "1.125rem", marginTop: "0.5rem" }}>
              {selectedType ? `…-${selectedType.code}-####` : "…-••-••••"}
            </div>
            <p style={{ margin: "0.5rem 0 0", fontSize: "0.75rem", color: "#525252" }}>
              The full asset code (organisation prefix + type code + sequence) and its QR code are generated on
              save.
            </p>
          </div>
        </aside>
      </div>
    </div>
  );
}
