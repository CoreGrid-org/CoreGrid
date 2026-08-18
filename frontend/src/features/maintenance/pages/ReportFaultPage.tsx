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
  FileUploader,
  ComboBox,
} from "@carbon/react";
import { useReportFault } from "../hooks/useMaintenance";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";

export default function ReportFaultPage() {
  const navigate = useNavigate();
  const reportFault = useReportFault();
  
  // Load assets to populate the ComboBox
  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = assetsData?.items || [];

  const [assetId, setAssetId] = useState("");
  const [description, setDescription] = useState("");
  const [observedCondition, setCondition] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!assetId || !description || !observedCondition) {
      setError("Please fill in all required fields.");
      return;
    }
    setError(null);
    reportFault.mutate(
      {
        asset_id: assetId,
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
          <h1 className="cg-page__title">Report a Fault</h1>
          <p className="cg-page__subtitle">Report an issue with an asset (FR-033).</p>
        </div>
      </div>

      <div className="cg-section" style={{ maxWidth: "600px" }}>
        {(error || reportFault.isError) && (
          <InlineNotification
            kind="error"
            title="Error"
            subtitle={error || getErrorMessage(reportFault.error, "Failed to report fault.")}
            lowContrast
            style={{ marginBottom: "1rem" }}
          />
        )}
        <Form onSubmit={handleSubmit}>
          <FormGroup legendText="Fault Details">
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

            <TextArea
              id="description"
              labelText="Fault Description"
              placeholder="Describe what is wrong with the asset..."
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
              <SelectItem value="POOR" text="Poor - Needs Repair" />
              <SelectItem value="UNSERVICEABLE" text="Unserviceable - Broken" />
            </Select>

            <div style={{ marginBottom: "1rem" }}>
              <FileUploader
                labelTitle="Attach a Photo (Optional)"
                labelDescription="Max file size 5MB"
                buttonLabel="Add file"
                buttonKind="ghost"
                size="md"
                filenameStatus="edit"
                accept={[".jpg", ".png", ".jpeg"]}
                multiple={false}
              />
            </div>
          </FormGroup>

          <div style={{ display: "flex", gap: "1rem", marginTop: "2rem" }}>
            <Button type="button" kind="secondary" onClick={() => navigate("..")} disabled={reportFault.isPending}>
              Cancel
            </Button>
            <Button type="submit" kind="primary" disabled={reportFault.isPending}>
              {reportFault.isPending ? "Submitting..." : "Report Fault"}
            </Button>
          </div>
        </Form>
      </div>
    </div>
  );
}
