import { useState } from "react";
import { Modal, ComboBox, TextArea, InlineNotification, Tag } from "@carbon/react";
import { useCreateWorkflow } from "../hooks/useWorkflows";
import { useAssetCategories, useAssetTypes, useAssetsList } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import type { AgentWorkflow } from "../api/workflows";
import type { Asset, AssetCategory, AssetType } from "@/features/assets/types/asset";

interface CreateWorkflowModalProps {
  onClose: () => void;
  onCreated: (workflow: AgentWorkflow) => void;
}

const OBJECTIVE_PRESETS = [
  "Evaluate whether to repair, replace or dispose of these assets.",
  "Evaluate maintenance cost against residual value and recommend a lifecycle action.",
  "Evaluate which assets are due for replacement or disposal.",
];

// Optional narrowing to one asset of the chosen type. Mounted only once a
// type is picked, so the asset list is fetched for that type alone.
function AssetPicker({ assetTypeId, assetId, onChange }: { assetTypeId: string; assetId: string; onChange: (id: string) => void }) {
  const { data, isLoading } = useAssetsList({ assetTypeId, pageSize: 100, sortBy: "assetCode" });
  const assets = (data?.items ?? []).filter((a) => a.status !== "DISPOSED");
  const selected = assets.find((a) => a.id === assetId) ?? null;

  return (
    <ComboBox<Asset>
      id="workflow-asset"
      titleText="Single asset (optional)"
      helperText="Leave empty to evaluate every active asset of this type."
      placeholder={isLoading ? "Loading assets…" : "Type to search by code or name…"}
      items={assets}
      itemToString={(item) => (item ? `${item.asset_code} - ${item.name}` : "")}
      selectedItem={selected}
      shouldFilterItem={comboBoxFilter(selected)}
      onChange={({ selectedItem }) => onChange(selectedItem?.id ?? "")}
    />
  );
}

// Initiates a lifecycle evaluation. The target is an asset type (its whole
// active fleet); a category narrows the type list, and a single asset is optional.
export default function CreateWorkflowModal({ onClose, onCreated }: CreateWorkflowModalProps) {
  const createWorkflow = useCreateWorkflow();
  const { data: categories, isLoading: isLoadingCategories } = useAssetCategories();
  const { data: assetTypes, isLoading: isLoadingTypes } = useAssetTypes();

  const [categoryId, setCategoryId] = useState("");
  const [assetTypeId, setAssetTypeId] = useState("");
  const [assetId, setAssetId] = useState("");
  const [objective, setObjective] = useState(OBJECTIVE_PRESETS[0]);

  const activeCategories = (categories ?? []).filter((c) => c.is_active);
  const typeOptions = (assetTypes ?? []).filter((t) => t.is_active && (!categoryId || t.asset_category_id === categoryId));
  const selectedCategory = activeCategories.find((c) => c.id === categoryId) ?? null;
  const selectedType = typeOptions.find((t) => t.id === assetTypeId) ?? null;

  const canSubmit = assetTypeId.length > 0 && objective.trim().length > 0;

  const handleSubmit = () => {
    if (!canSubmit || createWorkflow.isPending) return;
    createWorkflow.mutate(
      { asset_type_id: assetTypeId, asset_id: assetId || undefined, objective: objective.trim() },
      { onSuccess: onCreated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Agentic Workflows"
      modalHeading="New asset lifecycle evaluation"
      primaryButtonText={createWorkflow.isPending ? "Agents running…" : "Start evaluation"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || createWorkflow.isPending}
      preventCloseOnClickOutside
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {createWorkflow.isError && (
        <InlineNotification
          kind="error"
          title="Could not start the evaluation"
          subtitle={getErrorMessage(createWorkflow.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          className="cg-page-notification"
        />
      )}
      <div className="cg-stack">
        <div className="cg-modal-form__row">
          <ComboBox<AssetCategory>
            id="workflow-category"
            titleText="Category (optional)"
            placeholder={isLoadingCategories ? "Loading…" : "All categories"}
            items={activeCategories}
            itemToString={(item) => item?.name ?? ""}
            selectedItem={selectedCategory}
            shouldFilterItem={comboBoxFilter(selectedCategory)}
            onChange={({ selectedItem }) => {
              const next = selectedItem?.id ?? "";
              setCategoryId(next);
              // Keep the type only if it still belongs to the chosen category.
              if (next && selectedType && selectedType.asset_category_id !== next) {
                setAssetTypeId("");
                setAssetId("");
              }
            }}
          />
          <ComboBox<AssetType>
            id="workflow-asset-type"
            titleText="Asset type"
            placeholder={isLoadingTypes ? "Loading…" : "Type to search asset types…"}
            items={typeOptions}
            itemToString={(item) => (item ? `${item.name} (${item.category_name})` : "")}
            selectedItem={selectedType}
            shouldFilterItem={comboBoxFilter(selectedType)}
            onChange={({ selectedItem }) => {
              setAssetTypeId(selectedItem?.id ?? "");
              setAssetId("");
              if (selectedItem) setCategoryId(selectedItem.asset_category_id);
            }}
          />
        </div>

        {assetTypeId ? (
          <AssetPicker key={assetTypeId} assetTypeId={assetTypeId} assetId={assetId} onChange={setAssetId} />
        ) : (
          <p className="cg-table__muted cg-text-small">Choose an asset type first. You can then narrow it to one asset.</p>
        )}

        <div>
          <TextArea
            id="workflow-objective"
            labelText="Objective"
            value={objective}
            onChange={(e) => setObjective(e.target.value)}
            rows={2}
          />
          <div className="cg-objective-presets">
            {OBJECTIVE_PRESETS.map((preset) => (
              <Tag
                key={preset}
                type={preset === objective ? "blue" : "gray"}
                size="sm"
                as="button"
                onClick={() => setObjective(preset)}
              >
                {preset}
              </Tag>
            ))}
          </div>
        </div>
      </div>
    </Modal>
  );
}
