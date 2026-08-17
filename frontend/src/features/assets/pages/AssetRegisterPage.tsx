import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  Button,
  TextInput,
  NumberInput,
  ComboBox,
  Select,
  SelectItem,
  Checkbox,
  InlineNotification,
  CodeSnippet,
  Tag,
} from "@carbon/react";
import {
  useAssetDetail,
  useAssetTypes,
  useAssetTypeAttributes,
  useCreateAsset,
  useDepartments,
  useLocations,
  useOrganizationCode,
  useUpdateAsset,
} from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import {
  ASSET_CONDITIONS,
  type AssetCondition,
  type AssetAttributeValueRequest,
  type AssetType,
  type Department,
  type Location,
} from "../types/asset";
import { formatStatusLabel } from "@/shared/lib/statusTag";

type AttributeValue = string | number | boolean;

// POST /api/assets to create, PUT /api/assets/{id} to update (id present in
// the route). The full asset code (org code + category code + type code +
// sequence) and its QR payload are generated server-side on first save and
// never change afterwards — see backend/Features/Assets/Services/AssetService.cs.
export default function AssetRegisterPage() {
  const navigate = useNavigate();
  const { id: assetId } = useParams<{ id: string }>();
  const isEditMode = Boolean(assetId);

  const { data: assetTypes } = useAssetTypes();
  const { data: departments } = useDepartments();
  const { data: existingAsset, isLoading: isLoadingAsset } = useAssetDetail(assetId);
  const { data: organizationCode } = useOrganizationCode();

  const [assetTypeId, setAssetTypeId] = useState("");
  const [name, setName] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [condition, setCondition] = useState<AssetCondition>("NEW");
  const [acquisitionDate, setAcquisitionDate] = useState("");
  const [acquisitionCost, setAcquisitionCost] = useState<number | "">("");
  const [residualValue, setResidualValue] = useState<number | "">("");
  const [attributeValues, setAttributeValues] = useState<Record<string, AttributeValue>>({});
  const [prefilled, setPrefilled] = useState(false);
  const skipNextDepartmentReset = useRef(false);
  const skipNextAttributeReset = useRef(false);

  const { data: locations } = useLocations(departmentId || undefined);
  const { data: attributeDefs } = useAssetTypeAttributes(assetTypeId);
  const createAsset = useCreateAsset();
  const updateAsset = useUpdateAsset();
  const saveAsset = isEditMode ? updateAsset : createAsset;

  const selectedType = assetTypes?.find((t) => t.id === assetTypeId);

  // Inactive asset types aren't offered when registering a new asset, but an
  // existing asset must keep displaying/keeping its own (possibly since
  // deactivated) type.
  const selectableAssetTypes =
    assetTypes?.filter((t) => t.is_active || t.id === assetTypeId) ?? [];

  // Inactive attributes aren't requested when registering a new asset; an
  // existing asset must keep displaying (and not lose, on next save) values
  // it already has for attributes that have since been deactivated.
  const visibleAttributeDefs = isEditMode
    ? (attributeDefs ?? [])
    : (attributeDefs ?? []).filter((def) => def.is_active);

  // Edit mode: once the existing asset loads, seed every field from it —
  // once only, so the user's own edits afterwards aren't clobbered by a
  // background refetch.
  useEffect(() => {
    if (!existingAsset || prefilled) return;

    setAssetTypeId(existingAsset.asset_type_id);
    setName(existingAsset.name);
    setDepartmentId(existingAsset.department_id);
    setLocationId(existingAsset.location_id);
    setAcquisitionDate(existingAsset.acquisition_date.slice(0, 10));
    setAcquisitionCost(existingAsset.acquisition_cost);
    setResidualValue(existingAsset.residual_value);

    const values: Record<string, AttributeValue> = {};
    for (const attr of existingAsset.attributes) {
      if (attr.value_text !== null) values[attr.attribute_definition_id] = attr.value_text;
      else if (attr.value_number !== null) values[attr.attribute_definition_id] = attr.value_number;
      else if (attr.value_date !== null) values[attr.attribute_definition_id] = attr.value_date.slice(0, 10);
      else if (attr.value_boolean !== null) values[attr.attribute_definition_id] = attr.value_boolean;
    }
    setAttributeValues(values);
    skipNextDepartmentReset.current = true;
    skipNextAttributeReset.current = true;
    setPrefilled(true);
  }, [existingAsset, prefilled]);

  // Department changed: the previously selected location may no longer belong to it.
  // Skipped once right after prefilling so the asset's own saved location survives.
  useEffect(() => {
    if (skipNextDepartmentReset.current) {
      skipNextDepartmentReset.current = false;
      return;
    }
    setLocationId("");
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only department changes should trigger this reset
  }, [departmentId]);

  // Asset type changed: previous attribute values belonged to a different type's fields.
  // Skipped once right after prefilling so the asset's own saved attribute values survive.
  useEffect(() => {
    if (skipNextAttributeReset.current) {
      skipNextAttributeReset.current = false;
      return;
    }
    setAttributeValues({});
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only asset type changes should trigger this reset
  }, [assetTypeId]);

  const requiredAttributesFilled = visibleAttributeDefs.every((def) => {
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
    if (!canSubmit || saveAsset.isPending) return;

    const attributes: AssetAttributeValueRequest[] = visibleAttributeDefs
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

    if (isEditMode && assetId) {
      updateAsset.mutate(
        {
          id: assetId,
          payload: {
            asset_type_id: assetTypeId,
            department_id: departmentId,
            location_id: locationId,
            name: name.trim(),
            acquisition_date: acquisitionDate,
            acquisition_cost: acquisitionCost,
            residual_value: residualValue === "" ? 0 : residualValue,
            attributes,
          },
        },
        {
          onSuccess: (asset) => {
            navigate("/admin/assets", { state: { openAssetId: asset.id } });
          },
        },
      );
      return;
    }

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

  if (isEditMode && isLoadingAsset && !prefilled) {
    return (
      <div className="cg-page">
        <div className="cg-placeholder">
          <p>Loading asset…</p>
        </div>
      </div>
    );
  }

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">{isEditMode ? "Update asset" : "Register new asset"}</h1>
          <p className="cg-page__subtitle">
            {isEditMode
              ? "Change any field and save — the asset code and QR payload stay fixed."
              : "Attribute fields appear once a type is chosen — the form renders itself from that type's definitions."}
          </p>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button kind="secondary" onClick={() => navigate("/admin/assets")}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!canSubmit || saveAsset.isPending}>
            {saveAsset.isPending ? "Saving…" : isEditMode ? "Save changes" : "Save asset"}
          </Button>
        </div>
      </div>

      {saveAsset.isError && (
        <InlineNotification
          kind="error"
          title={isEditMode ? "Could not update asset" : "Could not create asset"}
          subtitle={getErrorMessage(saveAsset.error, "Something went wrong. Please try again.")}
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
              <ComboBox<AssetType>
                id="register-asset-type"
                titleText="Asset type"
                placeholder="Search asset types…"
                items={selectableAssetTypes}
                itemToString={(item) => (item ? `${item.name}${item.is_active ? "" : " — inactive"}` : "")}
                selectedItem={selectableAssetTypes.find((t) => t.id === assetTypeId) ?? null}
                onChange={({ selectedItem }) => setAssetTypeId(selectedItem?.id ?? "")}
              />
              <TextInput
                id="register-asset-name"
                labelText="Asset name"
                placeholder="e.g. Dell Latitude 5420"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
              <ComboBox<Department>
                id="register-asset-department"
                titleText="Department"
                placeholder="Search departments…"
                items={departments ?? []}
                itemToString={(item) => item?.name ?? ""}
                selectedItem={departments?.find((d) => d.id === departmentId) ?? null}
                onChange={({ selectedItem }) => setDepartmentId(selectedItem?.id ?? "")}
              />
              <ComboBox<Location>
                id="register-asset-location"
                titleText="Location"
                placeholder={departmentId ? "Search locations…" : "Choose a department first"}
                disabled={!departmentId}
                items={locations ?? []}
                itemToString={(item) => item?.name ?? ""}
                selectedItem={locations?.find((l) => l.id === locationId) ?? null}
                onChange={({ selectedItem }) => setLocationId(selectedItem?.id ?? "")}
              />
              {!isEditMode && (
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
              )}
              <TextInput
                id="register-asset-acquisition-date"
                labelText="Purchase date"
                type="date"
                value={acquisitionDate}
                onChange={(e) => setAcquisitionDate(e.target.value)}
              />
              <NumberInput
                id="register-asset-acquisition-cost"
                label="Purchase cost (LKR)"
                min={0}
                value={acquisitionCost}
                allowEmpty
                onChange={(_, { value }) => setAcquisitionCost(value === "" ? "" : Number(value))}
              />
              <NumberInput
                id="register-asset-residual-value"
                label="Residual value (LKR)"
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

            {assetTypeId && attributeDefs && visibleAttributeDefs.length === 0 && (
              <div className="cg-placeholder" style={{ marginTop: "1rem" }}>
                <p>This type has no custom attributes configured.</p>
              </div>
            )}

            {assetTypeId && attributeDefs && visibleAttributeDefs.length > 0 && (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", marginTop: "1rem" }}>
                {visibleAttributeDefs.map((def) => {
                  const value = attributeValues[def.id];
                  const setValue = (v: AttributeValue) => setAttributeValues((prev) => ({ ...prev, [def.id]: v }));
                  const label = def.is_required
                    ? `${def.name} *${def.is_active ? "" : " (inactive)"}`
                    : `${def.name}${def.is_active ? "" : " (inactive)"}`;

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
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
              <span
                style={{ fontSize: "0.625rem", letterSpacing: "0.05em", color: "#8d8d8d", textTransform: "uppercase", fontWeight: 600 }}
              >
                {isEditMode ? "Asset code" : "Code preview"}
              </span>
              {!isEditMode && <Tag type="cyan" size="sm">Auto-generated</Tag>}
            </div>

            <div style={{ marginTop: "0.75rem" }}>
              <CodeSnippet
                type="single"
                hideCopyButton
                disabled={isEditMode ? !existingAsset?.asset_code : !selectedType}
              >
                {isEditMode
                  ? (existingAsset?.asset_code ?? "…")
                  : selectedType
                    ? `${organizationCode?.code ?? "…"}-${selectedType.category_code}-${selectedType.code}-####`
                    : `${organizationCode?.code ?? "…"}-••-••-••••`}
              </CodeSnippet>
            </div>

            {!isEditMode && selectedType && (
              <div style={{ display: "flex", flexWrap: "wrap", gap: "0.375rem", marginTop: "0.75rem" }}>
                <Tag type="gray" size="sm" title="Organisation code">
                  Org · {organizationCode?.code ?? "…"}
                </Tag>
                <Tag type="blue" size="sm" title="Asset category code">
                  Category · {selectedType.category_code}
                </Tag>
                <Tag type="purple" size="sm" title="Asset type code">
                  Type · {selectedType.code}
                </Tag>
                <Tag type="gray" size="sm" title="Sequential number">
                  Seq · ####
                </Tag>
              </div>
            )}

            <p style={{ margin: "0.75rem 0 0", fontSize: "0.75rem", color: "#525252", lineHeight: 1.4 }}>
              {isEditMode
                ? "The asset code and QR payload were fixed at creation and cannot be changed."
                : "The full asset code (organisation code + category code + type code + sequence) and its QR code are generated on save."}
            </p>
          </div>
        </aside>
      </div>
    </div>
  );
}
