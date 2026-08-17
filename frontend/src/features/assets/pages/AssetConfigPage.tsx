import { useEffect, useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, InlineNotification, Button, Search } from "@carbon/react";
import { Add, Edit, TrashCan } from "@carbon/icons-react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import {
  useActivateAssetAttributeDefinition,
  useActivateAssetCategory,
  useActivateAssetType,
  useAssetCategories,
  useAssetTypeAttributes,
  useAssetTypes,
  useDeleteAssetAttributeDefinition,
  useDeleteAssetCategory,
  useDeleteAssetType,
} from "../hooks/useAssets";
import CreateAssetCategoryModal from "../components/CreateAssetCategoryModal";
import EditAssetCategoryModal from "../components/EditAssetCategoryModal";
import CreateAssetTypeModal from "../components/CreateAssetTypeModal";
import EditAssetTypeModal from "../components/EditAssetTypeModal";
import CreateAssetAttributeModal from "../components/CreateAssetAttributeModal";
import EditAssetAttributeModal from "../components/EditAssetAttributeModal";
import ConfirmDeleteModal from "../components/ConfirmDeleteModal";
import type { AssetAttributeDefinition, AssetCategory, AssetType } from "../types/asset";

const DATA_TYPE_COLOR: Record<string, "gray" | "blue" | "purple" | "teal" | "magenta"> = {
  TEXT: "gray",
  NUMBER: "blue",
  DATE: "purple",
  BOOLEAN: "teal",
  SELECT: "magenta",
};

type ModalState =
  | { kind: "create-category" }
  | { kind: "edit-category"; category: AssetCategory }
  | { kind: "delete-category"; category: AssetCategory }
  | { kind: "create-type" }
  | { kind: "edit-type"; assetType: AssetType }
  | { kind: "delete-type"; assetType: AssetType }
  | { kind: "create-attribute" }
  | { kind: "edit-attribute"; assetTypeId: string; assetTypeName: string; attribute: AssetAttributeDefinition }
  | {
      kind: "delete-attribute";
      assetTypeId: string;
      assetTypeName: string;
      attribute: AssetAttributeDefinition;
    }
  | null;

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
  const [modalState, setModalState] = useState<ModalState>(null);
  const [searchTerm, setSearchTerm] = useState("");

  const categories = useAssetCategories();
  const types = useAssetTypes();

  const deleteCategory = useDeleteAssetCategory();
  const activateCategory = useActivateAssetCategory();
  const deleteAssetType = useDeleteAssetType();
  const activateAssetType = useActivateAssetType();
  const deleteAttribute = useDeleteAssetAttributeDefinition();
  const activateAttribute = useActivateAssetAttributeDefinition();

  const selectedType = types.data?.find((t) => t.id === selectedTypeId);

  const closeModal = () => setModalState(null);

  const editAttribute = (attribute: AssetAttributeDefinition) => {
    if (!selectedType) return;
    setModalState({
      kind: "edit-attribute",
      assetTypeId: selectedType.id,
      assetTypeName: selectedType.name,
      attribute,
    });
  };

  const deleteAttributeFor = (attribute: AssetAttributeDefinition) => {
    if (!selectedType) return;
    setModalState({
      kind: "delete-attribute",
      assetTypeId: selectedType.id,
      assetTypeName: selectedType.name,
      attribute,
    });
  };

  const handleReactivateCategory = (id: string) => {
    activateCategory.mutate(id, { onSuccess: () => categories.refetch() });
  };

  const handleReactivateType = (id: string) => {
    activateAssetType.mutate(id, {
      onSuccess: () => {
        types.refetch();
        categories.refetch();
      },
    });
  };

  const handleReactivateAttribute = (assetTypeId: string, attributeId: string) => {
    activateAttribute.mutate(
      { assetTypeId, attributeId },
      { onSuccess: () => setAttributesRefreshKey((n) => n + 1) },
    );
  };

  const headerAction =
    activeTab === 0
      ? { label: "New Category", onClick: () => setModalState({ kind: "create-category" }), disabled: false }
      : activeTab === 1
        ? { label: "New Type", onClick: () => setModalState({ kind: "create-type" }), disabled: false }
        : { label: "New Attribute", onClick: () => setModalState({ kind: "create-attribute" }), disabled: !selectedType };

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

      {activeTab !== 2 && (
        <Search
          id="asset-config-search"
          labelText="Search categories or types"
          placeholder={activeTab === 0 ? "Search categories…" : "Search types…"}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          onClear={() => setSearchTerm("")}
          size="lg"
          style={{ marginBottom: "1rem" }}
        />
      )}

      <Tabs selectedIndex={activeTab} onChange={({ selectedIndex }) => setActiveTab(selectedIndex)}>
        <TabList aria-label="Asset configuration sections">
          <Tab>Categories</Tab>
          <Tab>Types</Tab>
          <Tab>Attributes</Tab>
        </TabList>
        <TabPanels>
          <TabPanel>
            <CategoriesTab
              categories={categories}
              searchTerm={searchTerm}
              onEdit={(category) => setModalState({ kind: "edit-category", category })}
              onDelete={(category) => setModalState({ kind: "delete-category", category })}
              onReactivate={handleReactivateCategory}
            />
          </TabPanel>
          <TabPanel>
            <TypesTab
              types={types}
              searchTerm={searchTerm}
              onEdit={(assetType) => setModalState({ kind: "edit-type", assetType })}
              onDelete={(assetType) => setModalState({ kind: "delete-type", assetType })}
              onReactivate={handleReactivateType}
            />
          </TabPanel>
          <TabPanel>
            <AttributesTab
              types={types}
              selectedTypeId={selectedTypeId}
              onSelectType={setSelectedTypeId}
              refreshKey={attributesRefreshKey}
              onEditAttribute={editAttribute}
              onDeleteAttribute={deleteAttributeFor}
              onReactivateAttribute={handleReactivateAttribute}
            />
          </TabPanel>
        </TabPanels>
      </Tabs>

      {modalState?.kind === "create-category" && (
        <CreateAssetCategoryModal
          onClose={closeModal}
          onCreated={() => {
            closeModal();
            categories.refetch();
          }}
        />
      )}

      {modalState?.kind === "edit-category" && (
        <EditAssetCategoryModal
          category={modalState.category}
          onClose={closeModal}
          onUpdated={() => {
            closeModal();
            categories.refetch();
          }}
        />
      )}

      {modalState?.kind === "create-type" && (
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

      {modalState?.kind === "edit-type" && (
        <EditAssetTypeModal
          assetType={modalState.assetType}
          onClose={closeModal}
          onUpdated={() => {
            closeModal();
            types.refetch();
            categories.refetch();
          }}
        />
      )}

      {modalState?.kind === "create-attribute" && selectedType && (
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

      {modalState?.kind === "edit-attribute" && (
        <EditAssetAttributeModal
          assetTypeId={modalState.assetTypeId}
          assetTypeName={modalState.assetTypeName}
          attribute={modalState.attribute}
          onClose={closeModal}
          onUpdated={() => {
            closeModal();
            types.refetch();
            setAttributesRefreshKey((n) => n + 1);
          }}
        />
      )}

      {modalState?.kind === "delete-category" && (
        <ConfirmDeleteModal
          heading="Delete category"
          itemName={modalState.category.name}
          isPending={deleteCategory.isPending}
          isError={deleteCategory.isError}
          error={deleteCategory.error}
          onClose={closeModal}
          onConfirm={() => {
            deleteCategory.mutate(modalState.category.id, {
              onSuccess: () => {
                closeModal();
                categories.refetch();
              },
            });
          }}
        />
      )}

      {modalState?.kind === "delete-type" && (
        <ConfirmDeleteModal
          heading="Delete type"
          itemName={modalState.assetType.name}
          isPending={deleteAssetType.isPending}
          isError={deleteAssetType.isError}
          error={deleteAssetType.error}
          onClose={closeModal}
          onConfirm={() => {
            deleteAssetType.mutate(modalState.assetType.id, {
              onSuccess: () => {
                closeModal();
                types.refetch();
                categories.refetch();
              },
            });
          }}
        />
      )}

      {modalState?.kind === "delete-attribute" && (
        <ConfirmDeleteModal
          heading="Delete attribute"
          itemName={modalState.attribute.name}
          isPending={deleteAttribute.isPending}
          isError={deleteAttribute.isError}
          error={deleteAttribute.error}
          onClose={closeModal}
          onConfirm={() => {
            deleteAttribute.mutate(
              { assetTypeId: modalState.assetTypeId, attributeId: modalState.attribute.id },
              {
                onSuccess: () => {
                  closeModal();
                  types.refetch();
                  setAttributesRefreshKey((n) => n + 1);
                },
              },
            );
          }}
        />
      )}
    </div>
  );
}

function CategoriesTab({
  categories,
  searchTerm,
  onEdit,
  onDelete,
  onReactivate,
}: {
  categories: ReturnType<typeof useAssetCategories>;
  searchTerm: string;
  onEdit: (category: AssetCategory) => void;
  onDelete: (category: AssetCategory) => void;
  onReactivate: (id: string) => void;
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
        <div key={cat.id} className="cg-section" style={{ margin: 0, opacity: cat.is_active ? 1 : 0.65 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", gap: "0.75rem" }}>
            <p className="cg-section__title" style={{ margin: 0 }}>{cat.name}</p>
            <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <Tag type="blue">{cat.code}</Tag>
              {!cat.is_active && <Tag type="gray">Inactive</Tag>}
              <Button
                kind="ghost"
                size="sm"
                renderIcon={Edit}
                iconDescription="Edit category"
                hasIconOnly
                onClick={() => onEdit(cat)}
              />
              <Button
                kind="ghost"
                size="sm"
                renderIcon={TrashCan}
                iconDescription="Delete category"
                hasIconOnly
                onClick={() => onDelete(cat)}
              />
            </div>
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
          {!cat.is_active && (
            <Button kind="tertiary" size="sm" style={{ marginTop: "1rem" }} onClick={() => onReactivate(cat.id)}>
              Reactivate
            </Button>
          )}
        </div>
      ))}
    </div>
  );
}

function TypesTab({
  types,
  searchTerm,
  onEdit,
  onDelete,
  onReactivate,
}: {
  types: ReturnType<typeof useAssetTypes>;
  searchTerm: string;
  onEdit: (assetType: AssetType) => void;
  onDelete: (assetType: AssetType) => void;
  onReactivate: (id: string) => void;
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
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {filtered.map((t) => (
            <tr key={t.id} style={{ opacity: t.is_active ? 1 : 0.65 }}>
              <td style={{ fontWeight: 500 }}>{t.name}</td>
              <td className="cg-table__muted">{t.category_name}</td>
              <td className="cg-table__muted">{t.useful_life_years} years</td>
              <td className="cg-table__muted">
                {t.default_maintenance_interval_days ? `${t.default_maintenance_interval_days} days` : "Not scheduled"}
              </td>
              <td className="cg-table__muted">{t.attribute_count}</td>
              <td>
                {t.is_active ? (
                  <Tag type="green">Active</Tag>
                ) : (
                  <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
                    <Tag type="gray">Inactive</Tag>
                    <Button kind="tertiary" size="sm" onClick={() => onReactivate(t.id)}>
                      Reactivate
                    </Button>
                  </div>
                )}
              </td>
              <td>
                <div style={{ display: "flex" }}>
                  <Button
                    kind="ghost"
                    size="sm"
                    renderIcon={Edit}
                    iconDescription="Edit type"
                    hasIconOnly
                    onClick={() => onEdit(t)}
                  />
                  <Button
                    kind="ghost"
                    size="sm"
                    renderIcon={TrashCan}
                    iconDescription="Delete type"
                    hasIconOnly
                    onClick={() => onDelete(t)}
                  />
                </div>
              </td>
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
  onEditAttribute: (attribute: AssetAttributeDefinition) => void;
  onDeleteAttribute: (attribute: AssetAttributeDefinition) => void;
  onReactivateAttribute: (assetTypeId: string, attributeId: string) => void;
}

function AttributesTab({
  types,
  selectedTypeId,
  onSelectType,
  refreshKey,
  onEditAttribute,
  onDeleteAttribute,
  onReactivateAttribute,
}: AttributesTabProps) {
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
      onEditAttribute={onEditAttribute}
      onDeleteAttribute={onDeleteAttribute}
      onReactivateAttribute={onReactivateAttribute}
    />
  );
}

function AttributesTabBody({
  data,
  selectedTypeId,
  onSelectType,
  refreshKey,
  onEditAttribute,
  onDeleteAttribute,
  onReactivateAttribute,
}: {
  data: AssetType[];
  selectedTypeId: string | undefined;
  onSelectType: (id: string) => void;
  refreshKey: number;
  onEditAttribute: (attribute: AssetAttributeDefinition) => void;
  onDeleteAttribute: (attribute: AssetAttributeDefinition) => void;
  onReactivateAttribute: (assetTypeId: string, attributeId: string) => void;
}) {
  const [typeSearch, setTypeSearch] = useState("");

  const filteredTypes = data.filter((t) => matches(typeSearch, t.name, t.code, t.category_name));

  return (
    <div style={{ display: "flex", gap: "1.5rem", alignItems: "flex-start" }}>
      <div className="cg-section" style={{ width: "16rem", flex: "none", margin: 0, padding: 0 }}>
        <div style={{ padding: "0.75rem" }}>
          <Search
            id="attributes-type-search"
            labelText="Search types"
            placeholder="Search types…"
            size="sm"
            value={typeSearch}
            onChange={(e) => setTypeSearch(e.target.value)}
            onClear={() => setTypeSearch("")}
          />
        </div>
        {filteredTypes.length === 0 && (
          <p className="cg-table__muted" style={{ padding: "0.75rem 1rem" }}>
            No types match "{typeSearch}".
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
              opacity: t.is_active ? 1 : 0.65,
            }}
          >
            {t.name}
            {!t.is_active && (
              <span className="cg-table__muted" style={{ fontSize: "0.7rem" }}>
                {" "}
                (inactive)
              </span>
            )}
          </button>
        ))}
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        {selectedTypeId && (
          <AssetTypeAttributesPanel
            key={`${selectedTypeId}:${refreshKey}`}
            assetTypeId={selectedTypeId}
            onEditAttribute={onEditAttribute}
            onDeleteAttribute={onDeleteAttribute}
            onReactivateAttribute={onReactivateAttribute}
          />
        )}
      </div>
    </div>
  );
}

function AssetTypeAttributesPanel({
  assetTypeId,
  onEditAttribute,
  onDeleteAttribute,
  onReactivateAttribute,
}: {
  assetTypeId: string;
  onEditAttribute: (attribute: AssetAttributeDefinition) => void;
  onDeleteAttribute: (attribute: AssetAttributeDefinition) => void;
  onReactivateAttribute: (assetTypeId: string, attributeId: string) => void;
}) {
  const [attributeSearch, setAttributeSearch] = useState("");
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
    matches(attributeSearch, attr.name, attr.data_type),
  );

  return (
    <div className="cg-section" style={{ margin: 0 }}>
      <div style={{ padding: "0.75rem 1rem" }}>
        <Search
          id="attributes-attribute-search"
          labelText="Search attributes"
          placeholder="Search attributes…"
          size="sm"
          value={attributeSearch}
          onChange={(e) => setAttributeSearch(e.target.value)}
          onClear={() => setAttributeSearch("")}
        />
      </div>
      {filteredAttributes.length === 0 ? (
        <p className="cg-table__muted" style={{ padding: "0 1rem 0.75rem" }}>
          No attributes match "{attributeSearch}".
        </p>
      ) : (
        <>
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Field label</th>
                <th>Data type</th>
                <th>Required</th>
                <th>Options</th>
                <th>Order</th>
                <th>Status</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {filteredAttributes.map((attr) => (
                <tr key={attr.id} style={{ opacity: attr.is_active ? 1 : 0.65 }}>
                  <td style={{ fontWeight: 500 }}>{attr.name}</td>
                  <td>
                    <Tag type={DATA_TYPE_COLOR[attr.data_type]}>{attr.data_type}</Tag>
                  </td>
                  <td className="cg-table__muted">{attr.is_required ? "Required" : "Optional"}</td>
                  <td className="cg-table__muted">{attr.select_options?.join(", ") ?? "—"}</td>
                  <td className="cg-table__muted">{attr.display_order}</td>
                  <td>
                    {attr.is_active ? (
                      <Tag type="green">Active</Tag>
                    ) : (
                      <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
                        <Tag type="gray">Inactive</Tag>
                        <Button
                          kind="tertiary"
                          size="sm"
                          onClick={() => onReactivateAttribute(assetTypeId, attr.id)}
                        >
                          Reactivate
                        </Button>
                      </div>
                    )}
                  </td>
                  <td>
                    <div style={{ display: "flex" }}>
                      <Button
                        kind="ghost"
                        size="sm"
                        renderIcon={Edit}
                        iconDescription="Edit attribute"
                        hasIconOnly
                        onClick={() => onEditAttribute(attr)}
                      />
                      <Button
                        kind="ghost"
                        size="sm"
                        renderIcon={TrashCan}
                        iconDescription="Delete attribute"
                        hasIconOnly
                        onClick={() => onDeleteAttribute(attr)}
                      />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <p style={{ padding: "0.75rem 1rem", fontSize: "0.75rem", color: "#525252", background: "#fafafa", margin: 0 }}>
            These rows are AssetAttributeDefinitions — the registration form renders exactly this list, in this
            order.
          </p>
        </>
      )}
    </div>
  );
}
