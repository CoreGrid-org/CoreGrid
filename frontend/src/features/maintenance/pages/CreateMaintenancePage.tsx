import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Form,
  FormGroup,
  TextArea,
  Select,
  SelectItem,
  Button,
  InlineNotification,
  ComboBox,
} from "@carbon/react";
import { useCreateMaintenance } from "../hooks/useMaintenance";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { MaintenanceType, MaintenancePriority } from "../types/maintenance";

export default function CreateMaintenancePage() {
  const navigate = useNavigate();
  const createMaintenance = useCreateMaintenance();
  
  // Load assets to populate the ComboBox
  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = assetsData?.items || [];

  const [assetId, setAssetId] = useState("");
  const [type, setType] = useState<MaintenanceType | "">("");
  const [priority, setPriority] = useState<MaintenancePriority | "">("");
  const [description, setDescription] = useState("");
  const [observedCondition, setCondition] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!assetId || !type || !priority || !description || !observedCondition) {
      setError("Please fill in all required fields.");
      return;
    }
    setError(null);
    createMaintenance.mutate(
      {
        asset_id: assetId,
        type,
        priority,
        description,
        observed_condition: observedCondition,
      },
      {
        onSuccess: () => navigate(".."),
      }
    );
  };

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Create Maintenance Record</h1>
          <p className="cg-page__subtitle">Directly create a corrective or preventive maintenance record (FR-035).</p>
        </div>
      </div>

      <div className="cg-section" style={{ maxWidth: "600px" }}>
        {(error || createMaintenance.isError) && (
          <InlineNotification
            kind="error"
            title="Error"
            subtitle={error || getErrorMessage(createMaintenance.error, "Failed to create maintenance record.")}
            lowContrast
            style={{ marginBottom: "1rem" }}
          />
        )}
        <Form onSubmit={handleSubmit}>
          <FormGroup legendText="Record Details">
            <ComboBox
              id="assetId"
              titleText="Select Asset"
              placeholder={isLoadingAssets ? "Loading assets..." : "Choose an asset..."}
              items={assets}
              itemToString={(item) => (item ? `${item.asset_code} - ${item.name}` : "")}
              selectedItem={assets.find((a) => a.id === assetId) ?? null}
              onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
              style={{ marginBottom: "1rem" }}
            />

            <Select
              id="type"
              labelText="Maintenance Type"
              value={type}
              onChange={(e) => setType(e.target.value as MaintenanceType)}
              required
              style={{ marginBottom: "1rem" }}
            >
              <SelectItem value="" text="Select type..." disabled hidden />
              <SelectItem value="CORRECTIVE" text="Corrective" />
              <SelectItem value="PREVENTIVE" text="Preventive" />
            </Select>

            <Select
              id="priority"
              labelText="Priority"
              value={priority}
              onChange={(e) => setPriority(e.target.value as MaintenancePriority)}
              required
              style={{ marginBottom: "1rem" }}
            >
              <SelectItem value="" text="Select priority..." disabled hidden />
              <SelectItem value="LOW" text="Low" />
              <SelectItem value="MEDIUM" text="Medium" />
              <SelectItem value="HIGH" text="High" />
              <SelectItem value="CRITICAL" text="Critical" />
            </Select>

            <TextArea
              id="description"
              labelText="Description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              required
              rows={4}
              style={{ marginBottom: "1rem" }}
            />

            <Select
              id="observedCondition"
              labelText="Observed Condition"
              value={observedCondition}
              onChange={(e) => setCondition(e.target.value)}
              required
              style={{ marginBottom: "1rem" }}
            >
              <SelectItem value="" text="Choose condition..." disabled hidden />
              <SelectItem value="GOOD" text="Good" />
              <SelectItem value="FAIR" text="Fair" />
              <SelectItem value="POOR" text="Poor" />
              <SelectItem value="UNSERVICEABLE" text="Unserviceable" />
            </Select>
          </FormGroup>

          <div style={{ display: "flex", gap: "1rem", marginTop: "2rem" }}>
            <Button type="button" kind="secondary" onClick={() => navigate("..")} disabled={createMaintenance.isPending}>
              Cancel
            </Button>
            <Button type="submit" kind="primary" disabled={createMaintenance.isPending}>
              {createMaintenance.isPending ? "Creating..." : "Create Record"}
            </Button>
          </div>
        </Form>
      </div>
    </div>
  );
}
