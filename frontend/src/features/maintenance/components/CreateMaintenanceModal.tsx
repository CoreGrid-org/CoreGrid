import { useState, type RefObject } from "react";
import {
  ComposedModal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  TextArea,
  Button,
  InlineNotification,
  ComboBox,
  TileGroup,
  RadioTile,
  Tag,
} from "@carbon/react";
import { Tools, Calendar } from "@carbon/icons-react";
import { useCreateMaintenance } from "../hooks/useMaintenance";
import type { MaintenanceRecord, MaintenanceType, MaintenancePriority } from "../types/maintenance";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import type { Asset } from "@/features/assets/types/asset";
import AssetSummary from "@/features/assets/components/AssetSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";

const DESCRIPTION_MAX = 2000;

interface Option<T extends string> {
  value: T;
  label: string;
}

const TYPE_OPTIONS: { value: MaintenanceType; title: string; description: string; icon: typeof Tools }[] = [
  { value: "CORRECTIVE", title: "Corrective", description: "Repair a fault or restore the asset to working order.", icon: Tools },
  { value: "PREVENTIVE", title: "Preventive", description: "Scheduled servicing to keep the asset from failing.", icon: Calendar },
];

const PRIORITY_OPTIONS: Option<MaintenancePriority>[] = [
  { value: "LOW", label: "Low" },
  { value: "MEDIUM", label: "Medium" },
  { value: "HIGH", label: "High" },
  { value: "CRITICAL", label: "Critical" },
];

const CONDITION_OPTIONS: Option<string>[] = [
  { value: "GOOD", label: "Good" },
  { value: "FAIR", label: "Fair" },
  { value: "POOR", label: "Poor" },
  { value: "UNSERVICEABLE", label: "Unserviceable" },
];

interface CreateMaintenanceModalProps {
  onClose: () => void;
  onCreated: (record: MaintenanceRecord) => void;
  launcherButtonRef?: RefObject<HTMLButtonElement | null>;
}

// POST /api/maintenance — a direct work order (not via a fault report).
// Mount it only while it's open so every open starts from a blank form.
export default function CreateMaintenanceModal({ onClose, onCreated, launcherButtonRef }: CreateMaintenanceModalProps) {
  const createMaintenance = useCreateMaintenance();

  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = assetsData?.items || [];

  const [assetId, setAssetId] = useState("");
  const [type, setType] = useState<MaintenanceType | "">("");
  const [priority, setPriority] = useState<MaintenancePriority | "">("");
  const [observedCondition, setCondition] = useState("");
  const [description, setDescription] = useState("");
  const [submitted, setSubmitted] = useState(false);

  const selectedAsset = assets.find((a) => a.id === assetId) ?? null;
  const selectedPriority = PRIORITY_OPTIONS.find((o) => o.value === priority) ?? null;
  const selectedCondition = CONDITION_OPTIONS.find((o) => o.value === observedCondition) ?? null;

  const errors = {
    asset: !assetId ? "Choose the asset this work is for." : null,
    type: !type ? "Choose the type of maintenance." : null,
    priority: !priority ? "Choose a priority." : null,
    condition: !observedCondition ? "Choose the asset's current condition." : null,
    description: !description.trim() ? "Describe the work to be done." : null,
  };
  const hasErrors = Object.values(errors).some(Boolean);

  const handleSubmit = () => {
    setSubmitted(true);
    if (hasErrors || !type || !priority || createMaintenance.isPending) return;
    createMaintenance.mutate(
      {
        asset_id: assetId,
        type,
        priority,
        description: description.trim(),
        observed_condition: observedCondition,
      },
      { onSuccess: onCreated },
    );
  };

  const handleClose = () => {
    if (createMaintenance.isPending) return;
    onClose();
  };

  return (
    <ComposedModal
      open
      size="md"
      onClose={handleClose}
      preventCloseOnClickOutside
      launcherButtonRef={launcherButtonRef}
      aria-label="New maintenance record"
      className="cg-fault-modal"
    >
      <ModalHeader label="Maintenance" title="New maintenance record" closeModal={handleClose} />
      <ModalBody hasScrollingContent>
        <form
          className="cg-fault-modal__form"
          noValidate
          onSubmit={(e) => {
            e.preventDefault();
            handleSubmit();
          }}
        >
          <p className="cg-fault-modal__intro">
            Create a corrective repair or preventive work order directly for a registered asset.
          </p>

          {createMaintenance.isError && (
            <InlineNotification
              kind="error"
              title="Could not create the record"
              subtitle={getErrorMessage(createMaintenance.error, "Something went wrong. Please try again.")}
              lowContrast
              hideCloseButton
            />
          )}

          <div className="cg-fault-modal__field">
            <ComboBox<Asset>
              id="create-maintenance-asset"
              titleText="Asset"
              helperText={selectedAsset ? undefined : "Search by asset code or name."}
              placeholder={isLoadingAssets ? "Loading assets…" : "Type to search assets…"}
              disabled={isLoadingAssets}
              autoAlign
              items={assets}
              itemToString={(item) => (item ? `${item.asset_code} - ${item.name}` : "")}
              selectedItem={selectedAsset}
              shouldFilterItem={comboBoxFilter(selectedAsset)}
              onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
              invalid={submitted && Boolean(errors.asset)}
              invalidText={errors.asset ?? undefined}
            />

            {selectedAsset && (
              <AssetSummary
                items={[
                  { label: "Type", value: selectedAsset.asset_type_name },
                  { label: "Location", value: selectedAsset.location_name },
                  { label: "Department", value: selectedAsset.department_name },
                  {
                    label: "Recorded condition",
                    value: (
                      <Tag type={statusTagColor(selectedAsset.condition)} size="sm">
                        {formatStatusLabel(selectedAsset.condition)}
                      </Tag>
                    ),
                  },
                ]}
              />
            )}
          </div>

          <fieldset className="cg-fault-modal__field cg-fault-modal__severity">
            <legend className="cds--label">Maintenance type</legend>
            <TileGroup
              name="create-maintenance-type"
              legend=""
              valueSelected={type}
              onChange={(value) => setType(value as MaintenanceType)}
            >
              {TYPE_OPTIONS.map(({ value, title, description: text, icon: Icon }) => (
                <RadioTile key={value} id={`create-maintenance-type-${value}`} value={value}>
                  <span className="cg-fault-modal__tile cg-fault-modal__tile--accent">
                    <Icon size={20} />
                    <span>
                      <span className="cg-fault-modal__tile-title">{title}</span>
                      <span className="cg-fault-modal__tile-text">{text}</span>
                    </span>
                  </span>
                </RadioTile>
              ))}
            </TileGroup>
            {submitted && errors.type && <p className="cg-fault-modal__error">{errors.type}</p>}
          </fieldset>

          <div className="cg-grid-2">
            <ComboBox<Option<MaintenancePriority>>
              id="create-maintenance-priority"
              titleText="Priority"
              placeholder="Choose priority…"
              autoAlign
              items={PRIORITY_OPTIONS}
              itemToString={(item) => item?.label ?? ""}
              selectedItem={selectedPriority}
              shouldFilterItem={comboBoxFilter(selectedPriority)}
              onChange={({ selectedItem }) => setPriority(selectedItem?.value ?? "")}
              invalid={submitted && Boolean(errors.priority)}
              invalidText={errors.priority ?? undefined}
            />
            <ComboBox<Option<string>>
              id="create-maintenance-condition"
              titleText="Observed condition"
              placeholder="Choose condition…"
              autoAlign
              items={CONDITION_OPTIONS}
              itemToString={(item) => item?.label ?? ""}
              selectedItem={selectedCondition}
              shouldFilterItem={comboBoxFilter(selectedCondition)}
              onChange={({ selectedItem }) => setCondition(selectedItem?.value ?? "")}
              invalid={submitted && Boolean(errors.condition)}
              invalidText={errors.condition ?? undefined}
            />
          </div>

          <TextArea
            id="create-maintenance-description"
            labelText="Description"
            placeholder="What needs to be done, and why?"
            rows={4}
            maxCount={DESCRIPTION_MAX}
            enableCounter
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            invalid={submitted && Boolean(errors.description)}
            invalidText={errors.description ?? undefined}
          />
        </form>
      </ModalBody>
      <ModalFooter>
        <Button kind="secondary" onClick={handleClose} disabled={createMaintenance.isPending}>
          Cancel
        </Button>
        <Button kind="primary" onClick={handleSubmit} disabled={createMaintenance.isPending}>
          {createMaintenance.isPending ? "Creating…" : "Create record"}
        </Button>
      </ModalFooter>
    </ComposedModal>
  );
}
