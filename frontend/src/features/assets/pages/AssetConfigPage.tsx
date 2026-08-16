import { useEffect, useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, InlineNotification, Button, Search } from "@carbon/react";
import { Add } from "@carbon/icons-react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useAssetCategories, useAssetTypeAttributes, useAssetTypes } from "../hooks/useAssets";
import CreateAssetCategoryModal from "../components/CreateAssetCategoryModal";
import CreateAssetTypeModal from "../components/CreateAssetTypeModal";
import CreateAssetAttributeModal from "../components/CreateAssetAttributeModal";
import type { AssetType } from "../types/asset";

const DATA_TYPE_COLOR: Record<string, "gray" | "blue" | "purple" | "teal" | "magenta"> = {
  TEXT: "gray",
  NUMBER: "blue",
  DATE: "purple",
  BOOLEAN: "teal",
  SELECT: "magenta",
};

type ConfigModal = "category" | "type" | "attribute" | null;

function escapeRegExp(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// True if `term` is blank (no filter) or starts a word in any of `values" —
// e.g. "it" matches "IT_Equipment" but not "Furniture" (the "it" buried
// inside "Furniture" doesn't start a word, so a plain substring search
// would wrongly surface unrelated categories/types/attributes).
function matches(term: string, ...values: (string | undefined)[]): boolean {
  const needle = term.trim();
  if (!needle) return true;
  const pattern = new RegExp(`\\b${escapeRegExp(needle)}`, "i");
  return values.some((v) => v !== undefined && pattern.test(v));
}

// The configurable platform model behind the asset register: categories,
// types and the per-type custom attribute definitions the registration form
// renders itself from. Reads via GET /api/asset-categories, GET
// /api/asset-types and GET /api/asset-types/{id}/attributes; creates via the
// matching POST endpoints — all persisted to the existing Asset schema
// tables (no schema changes).
export default function AssetConfigPage() {
  const [activeTab, setActiveTab] = useState(0);
  const [selectedTypeId, setSelectedTypeId] = useState<string | undefined>(undefined);
  const [attributesRefreshKey, setAttributesRefreshKey] = useState(0);
  const [openModal, setOpenModal] = useState<ConfigModal>(null);
  const [searchTerm, setSearchTerm] = useState("");

  const categories = useAssetCategories();
  const types = useAssetTypes();

  const selectedType = types.data?.find((t) => t.id === selectedTypeId);

  const closeModal = () => setOpenModal(null);

  const headerAction =
    activeTab === 0
      ? { label: "New Category", onClick: () => setOpenModal("category"), disabled: false }
      : activeTab === 1
        ? { label: "New Type", onClick: () => setOpenModal("type"), disabled: false }
        : { label: "New Attribute", onClick: () => setOpenModal("attribute"), disabled: !selectedType };

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Asset Configuration</h1>
          <p className="cg-page__subtitle">
            Categories, types and the custom attributes each type collects. Changes here reshape the
            registration form immediately — no deploy.
          </p>
        </div>
        <Button
          renderIcon={Add}
          onClick={headerAction.onClick}
          disabled={headerAction.disabled}
          title={activeTab === 2 && !selectedType ? "Select an asset type first" : undefined}
        >
          {headerAction.label}
        </Button>
      </div>

      <Search
        id="asset-config-search"
        labelText="Search categories, types, or attributes"
        placeholder="Search categories, types, or attributes…"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        onClear={() => setSearchTerm("")}
        size="lg"
        style={{ marginBottom: "1rem" }}
      />

      <Tabs selectedIndex={activeTab} onChange={({ selectedIndex }) => setActiveTab(selectedIndex)}>
        <TabList aria-label="Asset configuration sections">
          <Tab>Categories</Tab>
          <Tab>Types</Tab>
          <Tab>Attributes</Tab>
        </TabList>
        <TabPanels>
          <TabPanel>
            <CategoriesTab categories={categories} searchTerm={searchTerm} />
          </TabPanel>
          <TabPanel>
            <TypesTab types={types} searchTerm={searchTerm} />
          </TabPanel>
          <TabPanel>
            <AttributesTab
              types={types}
              selectedTypeId={selectedTypeId}
              onSelectType={setSelectedTypeId}
              refreshKey={attributesRefreshKey}
              searchTerm={searchTerm}
            />
          </TabPanel>
        </TabPanels>
      </Tabs>

      {openModal === "category" && (
        <CreateAssetCategoryModal
          onClose={closeModal}
          onCreated={() => {
            closeModal();
            categories.refetch();
          }}
        />
      )}

      {openModal === "type" && (
        <CreateAssetTypeModal
          onClose={closeModal}
          onCreated={(newType) => {
            closeModal();
            types.refetch();
            categories.refetch();
            setSelectedTypeId(newType.id);
          }}
        />
      )}

      {openModal === "attribute" && selectedType && (
        <CreateAssetAttributeModal
          assetTypeId={selectedType.id}
          assetTypeName={selectedType.name}
          onClose={closeModal}
          onCreated={() => {
            closeModal();
            types.refetch();
            setAttributesRefreshKey((n) => n + 1);
          }}
        />
      )}
    </div>
  );
}

function CategoriesTab({
  categories,
  searchTerm,
}: {
  categories: ReturnType<typeof useAssetCategories>;
  searchTerm: string;
}) {
  const { data, isLoading, isError, error } = categories;

  if (isLoading) {
    return (
      <div className="cg-placeholder">
        <p>Loading categories…</p>
      </div>
    );
  }

  if (isError) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load asset categories"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        lowContrast
        hideCloseButton
        style={{ marginBottom: "1rem", maxWidth: "100%" }}
      />
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No asset categories yet. Use "New Category" to add the first one.</p>
      </div>
    );
  }

  const filtered = data.filter((cat) => matches(searchTerm, cat.name, cat.code));

  if (filtered.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No categories match "{searchTerm}".</p>
      </div>
    );
  }

  return (
    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(260px, 1fr))", gap: "1rem" }}>
      {filtered.map((cat) => (
        <div key={cat.id} className="cg-section" style={{ margin: 0 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", gap: "0.75rem" }}>
            <p className="cg-section__title" style={{ margin: 0 }}>{cat.name}</p>
            <Tag type="blue">{cat.code}</Tag>
          </div>
          <div className="cg-kv-grid" style={{ marginTop: "1rem" }}>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Asset types</p>
              <p className="cg-kv-item__value">{cat.type_count}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Assets</p>
              <p className="cg-kv-item__value">{cat.asset_count}</p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

function TypesTab({
  types,
  searchTerm,
}: {
  types: ReturnType<typeof useAssetTypes>;
  searchTerm: string;
}) {
  const { data, isLoading, isError, error } = types;

  if (isLoading) {
    return (
      <div className="cg-placeholder">
        <p>Loading asset types…</p>
      </div>
    );
  }

  if (isError) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load asset types"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        lowContrast
        hideCloseButton
        style={{ marginBottom: "1rem", maxWidth: "100%" }}
      />
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No asset types configured yet. Use "New Type" to add the first one.</p>
      </div>
    );
  }

  const filtered = data.filter((t) => matches(searchTerm, t.name, t.code, t.category_name));

  if (filtered.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No asset types match "{searchTerm}".</p>
      </div>
    );
  }

  return (
    <div className="cg-section">
      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr>
            <th>Type name</th>
            <th>Category</th>
            <th>Useful life</th>
            <th>Maintenance interval</th>
            <th>Attributes</th>
          </tr>
        </thead>
        <tbody>
          {filtered.map((t) => (
            <tr key={t.id}>
              <td style={{ fontWeight: 500 }}>{t.name}</td>
              <td className="cg-table__muted">{t.category_name}</td>
              <td className="cg-table__muted">{t.useful_life_years} years</td>
              <td className="cg-table__muted">
                {t.default_maintenance_interval_days ? `${t.default_maintenance_interval_days} days` : "Not scheduled"}
              </td>
              <td className="cg-table__muted">{t.attribute_count}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

interface AttributesTabProps {
  types: ReturnType<typeof useAssetTypes>;
  selectedTypeId: string | undefined;
  onSelectType: (id: string) => void;
  refreshKey: number;
  searchTerm: string;
}

function AttributesTab({ types, selectedTypeId, onSelectType, refreshKey, searchTerm }: AttributesTabProps) {
  const { data, isLoading, isError, error } = types;

  useEffect(() => {
    if (!selectedTypeId && data && data.length > 0) onSelectType(data[0].id);
  }, [data, selectedTypeId, onSelectType]);

  if (isLoading) {
    return (
      <div className="cg-placeholder">
        <p>Loading asset types…</p>
      </div>
    );
  }

  if (isError) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load asset types"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        lowContrast
        hideCloseButton
        style={{ marginBottom: "1rem", maxWidth: "100%" }}
      />
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No asset types yet — add one on the Types tab first.</p>
      </div>
    );
  }

  return (
    <AttributesTabBody
      data={data}
      selectedTypeId={selectedTypeId}
      onSelectType={onSelectType}
      refreshKey={refreshKey}
      searchTerm={searchTerm}
    />
  );
}

function AttributesTabBody({
  data,
  selectedTypeId,
  onSelectType,
  refreshKey,
  searchTerm,
}: {
  data: AssetType[];
  selectedTypeId: string | undefined;
  onSelectType: (id: string) => void;
  refreshKey: number;
  searchTerm: string;
}) {
  const filteredTypes = data.filter((t) => matches(searchTerm, t.name, t.code, t.category_name));

  return (
    <div style={{ display: "flex", gap: "1.5rem", alignItems: "flex-start" }}>
      <div className="cg-section" style={{ width: "16rem", flex: "none", margin: 0, padding: 0 }}>
        {filteredTypes.length === 0 && (
          <p className="cg-table__muted" style={{ padding: "0.75rem 1rem" }}>
            No types match "{searchTerm}".
          </p>
        )}
        {filteredTypes.map((t) => (
          <button
            key={t.id}
            type="button"
            aria-current={selectedTypeId === t.id ? "true" : undefined}
            onClick={() => onSelectType(t.id)}
            style={{
              display: "block",
              width: "100%",
              textAlign: "left",
              padding: "0.75rem 1rem",
              border: "none",
              borderBottom: "1px solid #f0f0f0",
              background: selectedTypeId === t.id ? "#edf2fa" : "transparent",
              fontWeight: selectedTypeId === t.id ? 600 : 400,
              cursor: "pointer",
            }}
          >
            {t.name}
          </button>
        ))}
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        {selectedTypeId && (
          <AssetTypeAttributesPanel
            key={`${selectedTypeId}:${refreshKey}`}
            assetTypeId={selectedTypeId}
            searchTerm={searchTerm}
          />
        )}
      </div>
    </div>
  );
}

function AssetTypeAttributesPanel({
  assetTypeId,
  searchTerm,
}: {
  assetTypeId: string;
  searchTerm: string;
}) {
  const { data: attributes, isLoading, isError, error } = useAssetTypeAttributes(assetTypeId);

  if (isLoading) {
    return (
      <div className="cg-placeholder">
        <p>Loading attributes…</p>
      </div>
    );
  }

  if (isError) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load attributes"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        lowContrast
        hideCloseButton
        style={{ marginBottom: "1rem", maxWidth: "100%" }}
      />
    );
  }

  if (!attributes || attributes.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No custom attributes defined for this type yet. Use "New Attribute" to add one.</p>
      </div>
    );
  }

  const filteredAttributes = attributes.filter((attr) =>
    matches(searchTerm, attr.name, attr.data_type),
  );

  if (filteredAttributes.length === 0) {
    return (
      <div className="cg-placeholder">
        <p>No attributes match "{searchTerm}".</p>
      </div>
    );
  }

  return (
    <div className="cg-section" style={{ margin: 0 }}>
      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr>
            <th>Field label</th>
            <th>Data type</th>
            <th>Required</th>
            <th>Options</th>
            <th>Order</th>
          </tr>
        </thead>
        <tbody>
          {filteredAttributes.map((attr) => (
            <tr key={attr.id}>
              <td style={{ fontWeight: 500 }}>{attr.name}</td>
              <td>
                <Tag type={DATA_TYPE_COLOR[attr.data_type]}>{attr.data_type}</Tag>
              </td>
              <td className="cg-table__muted">{attr.is_required ? "Required" : "Optional"}</td>
              <td className="cg-table__muted">{attr.select_options?.join(", ") ?? "—"}</td>
              <td className="cg-table__muted">{attr.display_order}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p style={{ padding: "0.75rem 1rem", fontSize: "0.75rem", color: "#525252", background: "#fafafa", margin: 0 }}>
        These rows are AssetAttributeDefinitions — the registration form renders exactly this list, in this
        order.
      </p>
    </div>
  );
}
