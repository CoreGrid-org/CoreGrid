import { useEffect, useRef, useState, useMemo } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Button,
  TextInput,
  NumberInput,
  ComboBox,
  Dropdown,
  Checkbox,
  InlineNotification,
  CopyButton,
  ProgressBar,
  Tag,
} from "@carbon/react";
import { Calendar, Information, QrCode as QrCodeIcon } from "@carbon/icons-react";
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
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { localTodayIso, previewDepreciation } from "../utils/depreciation";

type AttributeValue = string | number | boolean;

// POST /api/assets to create, PUT /api/assets/{id} to update (id present in
// the route). The full asset code (org code + category code + type code +
// sequence) and its QR payload are generated server-side on first save and
// never change afterwards — see backend/Features/Assets/Services/AssetService.cs.
export default function AssetRegisterPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { id: assetId } = useParams<{ id: string }>();
  const isEditMode = Boolean(assetId);

  // Mounted under both /admin/assets/... and /inventory/assets/... (App.tsx)
  // — derive the list path from the current role prefix rather than
  // hardcoding one, so Cancel/Save stay within the current role's routes.
  const assetsListPath = `/${location.pathname.split("/")[1]}/assets`;

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
  const selectedDepartment = departments?.find((d) => d.id === departmentId) ?? null;
  const selectedLocation = locations?.find((l) => l.id === locationId) ?? null;

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

  // A purchase can't be in the future; the backend rejects it too.
  const today = localTodayIso();
  const isFutureDate = acquisitionDate.length > 0 && acquisitionDate > today;

  const canSubmit =
    assetTypeId.length > 0 &&
    name.trim().length > 0 &&
    departmentId.length > 0 &&
    locationId.length > 0 &&
    acquisitionDate.length > 0 &&
    !isFutureDate &&
    acquisitionCost !== "" &&
    acquisitionCost >= 0 &&
    requiredAttributesFilled;

  // Live preview of what the server will store as residual_value on save.
  // Needs a type (for its useful life), a date and a cost; null until then.
  const depreciation = useMemo(() => {
    if (!selectedType || !acquisitionDate || isFutureDate || acquisitionCost === "" || acquisitionCost < 0) return null;
    return previewDepreciation(acquisitionCost, acquisitionDate, selectedType.useful_life_years);
  }, [acquisitionCost, acquisitionDate, isFutureDate, selectedType]);

  // Mirrors AssetCodeGenerator (org-category-type-NNNN); the sequence is
  // only known once the server saves the asset.
  const codeSegments = [
    { key: "org", label: "Organisation", value: organizationCode?.code ?? "ORG", pending: !organizationCode?.code },
    { key: "category", label: "Category", value: selectedType?.category_code ?? "CAT", pending: !selectedType },
    { key: "type", label: "Type", value: selectedType?.code ?? "TYPE", pending: !selectedType },
    { key: "seq", label: "Sequence", value: "0000", pending: true },
  ];

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

            attributes,
          },
        },
        {
          onSuccess: (asset) => {
            navigate(assetsListPath, { state: { openAssetId: asset.id } });
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
              ? "Change any field and save - the asset code and QR payload stay fixed."
              : "Attribute fields appear once a type is chosen - the form renders itself from that type's definitions."}
          </p>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button kind="secondary" onClick={() => navigate(assetsListPath)}>
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

      <div className="cg-asset-register">
        <div className="cg-asset-register__main">
          <section className="cg-section cg-asset-register__card">
            <header className="cg-section__header">
              <div>
                <p className="cg-section__title">Asset details</p>
                <p className="cg-asset-register__hint">What the asset is and the state it's in.</p>
              </div>
            </header>
            <div className="cg-section__body cg-asset-register__grid">
              <ComboBox<AssetType>
                id="register-asset-type"
                titleText="Asset type"
                placeholder="Type to search asset types…"
                items={selectableAssetTypes}
                itemToString={(item) => (item ? `${item.name}${item.is_active ? "" : " - inactive"}` : "")}
                selectedItem={selectedType ?? null}
                shouldFilterItem={comboBoxFilter(selectedType)}
                onChange={({ selectedItem }) => setAssetTypeId(selectedItem?.id ?? "")}
              />
              <TextInput
                id="register-asset-name"
                labelText="Asset name"
                placeholder="e.g. Dell Latitude 5420"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
              {!isEditMode && (
                <Dropdown
                  id="register-asset-condition"
                  titleText="Condition"
                  label={formatStatusLabel(condition)}
                  items={ASSET_CONDITIONS}
                  itemToString={(item) => (item ? formatStatusLabel(item) : "")}
                  selectedItem={condition}
                  onChange={({ selectedItem }) => selectedItem && setCondition(selectedItem)}
                />
              )}
            </div>
          </section>

          <section className="cg-section cg-asset-register__card">
            <header className="cg-section__header">
              <div>
                <p className="cg-section__title">Assignment</p>
                <p className="cg-asset-register__hint">Which department owns it and where it's kept.</p>
              </div>
            </header>
            <div className="cg-section__body cg-asset-register__grid">
              <ComboBox<Department>
                id="register-asset-department"
                titleText="Department"
                placeholder="Type to search departments…"
                items={departments ?? []}
                itemToString={(item) => item?.name ?? ""}
                selectedItem={selectedDepartment}
                shouldFilterItem={comboBoxFilter(selectedDepartment)}
                onChange={({ selectedItem }) => setDepartmentId(selectedItem?.id ?? "")}
              />
              <ComboBox<Location>
                id="register-asset-location"
                titleText="Location"
                placeholder={departmentId ? "Type to search locations…" : "Choose a department first"}
                disabled={!departmentId}
                items={locations ?? []}
                itemToString={(item) => item?.name ?? ""}
                selectedItem={selectedLocation}
                shouldFilterItem={comboBoxFilter(selectedLocation)}
                onChange={({ selectedItem }) => setLocationId(selectedItem?.id ?? "")}
              />
            </div>
          </section>

          <section className="cg-section cg-asset-register__card">
            <header className="cg-section__header">
              <div>
                <p className="cg-section__title">Purchase &amp; valuation</p>
                <p className="cg-asset-register__hint">Residual value is worked out from these and the asset type's useful life.</p>
              </div>
            </header>
            <div className="cg-section__body">
              <div className="cg-asset-register__grid">
                <TextInput
                  id="register-asset-acquisition-date"
                  labelText="Purchase date"
                  type="date"
                  max={today}
                  value={acquisitionDate}
                  invalid={isFutureDate}
                  invalidText="Purchase date can't be in the future."
                  helperText={isFutureDate ? undefined : "Today or earlier"}
                  onChange={(e) => setAcquisitionDate(e.target.value)}
                />
                <NumberInput
                  id="register-asset-acquisition-cost"
                  label="Purchase cost (LKR)"
                  min={0}
                  value={acquisitionCost}
                  allowEmpty
                  hideSteppers
                  onChange={(_, { value }) => setAcquisitionCost(value === "" ? "" : Number(value))}
                />
              </div>

              <div className="cg-valuation">
                {depreciation && selectedType ? (
                  <>
                    <div className="cg-valuation__headline">
                      <div>
                        <p className="cg-valuation__label">Calculated residual value (LKR)</p>
                        <p className="cg-valuation__value">{formatLkr(depreciation.residualValue)}</p>
                      </div>
                      <Tag type={depreciation.residualValue === 0 ? "red" : "green"} size="sm">
                        {Math.round((1 - depreciation.depreciatedFraction) * 100)}% of cost remaining
                      </Tag>
                    </div>
                    <ProgressBar
                      label="Depreciated so far"
                      hideLabel
                      size="small"
                      value={Math.round(depreciation.depreciatedFraction * 100)}
                      max={100}
                    />
                    <dl className="cg-valuation__breakdown">
                      <div>
                        <dt>Useful life</dt>
                        <dd>{selectedType.useful_life_years} {selectedType.useful_life_years === 1 ? "year" : "years"}</dd>
                      </div>
                      <div>
                        <dt>Age today</dt>
                        <dd>{formatAge(depreciation.elapsedYears)}</dd>
                      </div>
                      <div>
                        <dt>Annual depreciation</dt>
                        <dd>{formatLkr(depreciation.annualDepreciation)}</dd>
                      </div>
                      <div>
                        <dt>Depreciated so far</dt>
                        <dd>{formatLkr(depreciation.accumulatedDepreciation)}</dd>
                      </div>
                    </dl>
                    <p className="cg-valuation__note">
                      <Information size={14} />
                      Straight-line: cost ÷ useful life × years owned. The server recalculates this on save.
                    </p>
                  </>
                ) : (
                  <p className="cg-valuation__empty">
                    <Calendar size={16} />
                    Choose an asset type, purchase date and cost to see the calculated residual value.
                  </p>
                )}
              </div>
            </div>
          </section>

          <section className="cg-section cg-asset-register__card">
            <header className="cg-section__header">
              <div>
                <p className="cg-section__title">Attribute details</p>
                <p className="cg-asset-register__hint">Fields configured for the chosen asset type.</p>
              </div>
              {selectedType && <Tag type="purple" size="sm">{selectedType.name}</Tag>}
            </header>
            <div className="cg-section__body">
              {!assetTypeId && (
                <div className="cg-placeholder cg-asset-register__placeholder">
                  <p>Pick an asset type above and the fields configured for it render here.</p>
                </div>
              )}

              {assetTypeId && attributeDefs && visibleAttributeDefs.length === 0 && (
                <div className="cg-placeholder cg-asset-register__placeholder">
                  <p>This type has no custom attributes configured.</p>
                </div>
              )}

              {assetTypeId && attributeDefs && visibleAttributeDefs.length > 0 && (
                <div className="cg-asset-register__grid">
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
                      const options = def.select_options ?? [];
                      const currentValue = typeof value === "string" ? value : "";
                      return (
                        <Dropdown
                          key={def.id}
                          id={`register-attr-${def.id}`}
                          titleText={label}
                          label={currentValue || "Choose…"}
                          items={["", ...options]}
                          itemToString={(item) => item || "Choose…"}
                          selectedItem={currentValue}
                          onChange={({ selectedItem }) => setValue(selectedItem ?? "")}
                        />
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
                          hideSteppers
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
          </section>
        </div>

        <aside className="cg-asset-register__aside">
          <section className="cg-section cg-asset-register__card">
            <header className="cg-section__header">
              <p className="cg-section__title">{isEditMode ? "Asset code" : "Code preview"}</p>
              <Tag type={isEditMode ? "gray" : "cyan"} size="sm">{isEditMode ? "Fixed" : "Auto-generated"}</Tag>
            </header>
            <div className="cg-section__body">
              {isEditMode ? (
                <div className="cg-code-preview__code cg-code-preview__code--final">
                  <span>{existingAsset?.asset_code ?? "…"}</span>
                  {existingAsset?.asset_code && (
                    <CopyButton
                      iconDescription="Copy asset code"
                      feedback="Copied"
                      onClick={() => void navigator.clipboard?.writeText(existingAsset.asset_code)}
                    />
                  )}
                </div>
              ) : (
                <>
                  <div className="cg-code-preview__code" aria-label="Asset code preview">
                    {codeSegments.map((segment, index) => (
                      <span key={segment.key} className="cg-code-preview__segment-wrap">
                        {index > 0 && <span className="cg-code-preview__sep">-</span>}
                        <span
                          className={`cg-code-preview__segment cg-code-preview__segment--${segment.key}${segment.pending ? " is-pending" : ""}`}
                          title={segment.label}
                        >
                          {segment.value}
                        </span>
                      </span>
                    ))}
                  </div>

                  <ul className="cg-code-preview__legend">
                    {codeSegments.map((segment) => (
                      <li key={segment.key}>
                        <span className={`cg-code-preview__swatch cg-code-preview__swatch--${segment.key}`} />
                        <span className="cg-code-preview__legend-label">{segment.label}</span>
                        <span className="cg-code-preview__legend-value">{segment.pending ? "-" : segment.value}</span>
                      </li>
                    ))}
                  </ul>
                </>
              )}

              <p className="cg-code-preview__note">
                <QrCodeIcon size={16} />
                {isEditMode
                  ? "The asset code and QR payload were fixed at creation and cannot be changed."
                  : "The final number and its QR code are assigned when you save."}
              </p>
            </div>
          </section>
        </aside>
      </div>
    </div>
  );
}

const lkrFormatter = new Intl.NumberFormat("en-LK", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

function formatLkr(value: number): string {
  return lkrFormatter.format(value);
}

// 2.37 → "2 yrs 4 mos"; under a month → "Less than a month".
function formatAge(years: number): string {
  const totalMonths = Math.floor(years * 12);
  if (totalMonths < 1) return "Less than a month";
  const y = Math.floor(totalMonths / 12);
  const m = totalMonths % 12;
  const parts = [];
  if (y > 0) parts.push(`${y} ${y === 1 ? "yr" : "yrs"}`);
  if (m > 0) parts.push(`${m} ${m === 1 ? "mo" : "mos"}`);
  return parts.join(" ");
}
