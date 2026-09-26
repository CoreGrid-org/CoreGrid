import { useState } from "react";
import { ComposedModal, ModalHeader, ModalBody, ModalFooter, Button, InlineNotification, ComboBox } from "@carbon/react";
import { ArrowRight } from "@carbon/icons-react";
import { useInitiateTransfer } from "../hooks/useTransfers";
import { useAssetsList, useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import type { Asset, Department, Location } from "@/features/assets/types/asset";
import AssetSummary from "@/features/assets/components/AssetSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";

const locationLabel = (l: Location | null) => (l ? (l.type ? `${l.name} (${l.type})` : l.name) : "");

// Provides the asset transfer initiation form. Only ACTIVE assets can be
// transferred (the backend enforces the same rule).
export default function InitiateTransferModal({ onClose, onInitiated }: { onClose: () => void; onInitiated: () => void }) {
  const [assetId, setAssetId] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [submitted, setSubmitted] = useState(false);

  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = (assetsData?.items ?? []).filter((a) => a.status === "ACTIVE");
  const { data: departments, isLoading: isLoadingDepartments } = useDepartments();
  const { data: locations } = useLocations(departmentId || undefined);

  const selectedAsset = assets.find((a) => a.id === assetId) ?? null;
  const selectedDepartment = departments?.find((d) => d.id === departmentId) ?? null;
  // The asset's current location isn't a real destination.
  const destinationLocations = (locations ?? []).filter((l) => l.id !== selectedAsset?.location_id);
  const selectedLocation = destinationLocations.find((l) => l.id === locationId) ?? null;

  const initiateTransfer = useInitiateTransfer();

  const errors = {
    asset: !assetId ? "Choose the asset to transfer." : null,
    department: !departmentId ? "Choose the destination department." : null,
    location: !locationId ? "Choose the destination location." : null,
  };

  const handleSubmit = () => {
    setSubmitted(true);
    if (Object.values(errors).some(Boolean) || initiateTransfer.isPending) return;
    initiateTransfer.mutate(
      { asset_id: assetId, to_department_id: departmentId, to_location_id: locationId },
      { onSuccess: onInitiated },
    );
  };

  const handleClose = () => {
    if (!initiateTransfer.isPending) onClose();
  };

  return (
    <ComposedModal open size="sm" onClose={handleClose} preventCloseOnClickOutside aria-label="Initiate asset transfer">
      <ModalHeader label="Transfers" title="Initiate asset transfer" closeModal={handleClose} />
      <ModalBody hasScrollingContent>
        <div className="cg-modal-form">
          <p className="cg-modal-form__intro">
            Move an active asset to another department or location. The request goes for approval before the asset moves.
          </p>

          {initiateTransfer.isError && (
            <InlineNotification
              kind="error"
              title="Could not initiate transfer"
              subtitle={getErrorMessage(initiateTransfer.error, "Transfer initiation failed.")}
              lowContrast
              hideCloseButton
              style={{ maxWidth: "100%" }}
            />
          )}

          <div>
            <ComboBox<Asset>
              id="transfer-asset-select"
              titleText="Asset"
              helperText={selectedAsset ? undefined : "Only active assets can be transferred."}
              placeholder={isLoadingAssets ? "Loading assets…" : "Type to search by code or name…"}
              disabled={isLoadingAssets}
              autoAlign
              items={assets}
              itemToString={(item) => (item ? `${item.asset_code} - ${item.name}` : "")}
              selectedItem={selectedAsset}
              shouldFilterItem={comboBoxFilter(selectedAsset)}
              onChange={({ selectedItem }) => {
                setAssetId(selectedItem?.id ?? "");
                setLocationId("");
              }}
              invalid={submitted && Boolean(errors.asset)}
              invalidText={errors.asset ?? undefined}
            />
            {selectedAsset && (
              <AssetSummary
                items={[
                  { label: "Type", value: selectedAsset.asset_type_name },
                  { label: "Department", value: selectedAsset.department_name },
                  { label: "Location", value: selectedAsset.location_name },
                ]}
              />
            )}
          </div>

          <ComboBox<Department>
            id="transfer-dept-select"
            titleText="Destination department"
            placeholder={isLoadingDepartments ? "Loading departments…" : "Type to search departments…"}
            disabled={isLoadingDepartments}
            autoAlign
            items={departments ?? []}
            itemToString={(item) => item?.name ?? ""}
            selectedItem={selectedDepartment}
            shouldFilterItem={comboBoxFilter(selectedDepartment)}
            onChange={({ selectedItem }) => {
              setDepartmentId(selectedItem?.id ?? "");
              setLocationId("");
            }}
            invalid={submitted && Boolean(errors.department)}
            invalidText={errors.department ?? undefined}
          />

          <ComboBox<Location>
            id="transfer-loc-select"
            titleText="Destination location"
            placeholder={departmentId ? "Type to search locations…" : "Choose a department first"}
            disabled={!departmentId}
            autoAlign
            items={destinationLocations}
            itemToString={locationLabel}
            selectedItem={selectedLocation}
            shouldFilterItem={comboBoxFilter(selectedLocation)}
            onChange={({ selectedItem }) => setLocationId(selectedItem?.id ?? "")}
            invalid={submitted && Boolean(errors.location)}
            invalidText={errors.location ?? undefined}
          />

          {selectedAsset && selectedDepartment && selectedLocation && (
            <div className="cg-modal-form__route" aria-label="Transfer route">
              <div>
                <span className="cg-modal-form__route-label">From</span>
                <span className="cg-modal-form__route-dept">{selectedAsset.department_name}</span>
                <span className="cg-modal-form__route-loc">{selectedAsset.location_name}</span>
              </div>
              <ArrowRight size={20} />
              <div>
                <span className="cg-modal-form__route-label">To</span>
                <span className="cg-modal-form__route-dept">{selectedDepartment.name}</span>
                <span className="cg-modal-form__route-loc">{selectedLocation.name}</span>
              </div>
            </div>
          )}
        </div>
      </ModalBody>
      <ModalFooter>
        <Button kind="secondary" onClick={handleClose} disabled={initiateTransfer.isPending}>
          Cancel
        </Button>
        <Button kind="primary" onClick={handleSubmit} disabled={initiateTransfer.isPending}>
          {initiateTransfer.isPending ? "Submitting…" : "Submit request"}
        </Button>
      </ModalFooter>
    </ComposedModal>
  );
}
