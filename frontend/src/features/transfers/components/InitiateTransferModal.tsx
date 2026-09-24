import { useState } from "react";
import { Modal, InlineNotification, ComboBox, Select, SelectItem } from "@carbon/react";
import { useInitiateTransfer } from "../hooks/useTransfers";
import { useAssetsList, useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";

// Provides the asset transfer initiation form.
export default function InitiateTransferModal({ onClose, onInitiated }: { onClose: () => void; onInitiated: () => void }) {
  const [assetId, setAssetId] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = (assetsData?.items ?? []).filter((a) => a.status === "ACTIVE");
  const departments = useDepartments();
  const locations = useLocations(departmentId || undefined);

  const initiateTransfer = useInitiateTransfer();

  const handleSubmit = () => {
    if (!assetId || !departmentId || !locationId) {
      setFormError("Please select an asset, destination department, and destination location.");
      return;
    }
    setFormError(null);
    initiateTransfer.mutate(
      { asset_id: assetId, to_department_id: departmentId, to_location_id: locationId },
      { onSuccess: onInitiated },
    );
  };

  return (
    <Modal
      open
      modalHeading="Initiate Asset Transfer (FR-043)"
      primaryButtonText="Submit Request"
      secondaryButtonText="Cancel"
      primaryButtonDisabled={initiateTransfer.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <p style={{ marginBottom: "1rem", fontSize: "0.875rem" }}>
        Select an active asset and designate the target destination department and location.
      </p>

      {formError && (
        <InlineNotification kind="error" title="Validation error" subtitle={formError} lowContrast hideCloseButton style={{ marginBottom: "1rem" }} />
      )}
      {initiateTransfer.isError && (
        <InlineNotification
          kind="error"
          title="Could not initiate transfer"
          subtitle={getErrorMessage(initiateTransfer.error, "Transfer initiation failed.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem" }}
        />
      )}

      <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
        <ComboBox
          id="transfer-asset-select"
          titleText="Asset *"
          placeholder={isLoadingAssets ? "Loading assets..." : "Select an asset to transfer"}
          items={assets}
          itemToString={(item) => (item ? `${item.asset_code} — ${item.name} (${item.department_name})` : "")}
          onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
        />

        <Select
          id="transfer-dept-select"
          labelText="Destination Department *"
          value={departmentId}
          onChange={(e) => {
            setDepartmentId(e.target.value);
            setLocationId("");
          }}
        >
          <SelectItem value="" text="Choose destination department" />
          {departments.data?.map((d) => (
            <SelectItem key={d.id} value={d.id} text={d.name} />
          ))}
        </Select>

        <Select
          id="transfer-loc-select"
          labelText="Destination Location *"
          value={locationId}
          disabled={!departmentId}
          onChange={(e) => setLocationId(e.target.value)}
        >
          <SelectItem value="" text={!departmentId ? "Select a destination department first" : "Choose destination location"} />
          {locations.data?.map((l) => (
            <SelectItem key={l.id} value={l.id} text={l.type ? `${l.name} (${l.type})` : l.name} />
          ))}
        </Select>
      </div>
    </Modal>
  );
}
