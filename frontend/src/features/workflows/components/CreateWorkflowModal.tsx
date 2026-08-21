import { useState } from "react";
import { Modal, ComboBox, TextArea, InlineNotification } from "@carbon/react";
import { useCreateWorkflow } from "../hooks/useWorkflows";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { AgentWorkflow } from "../api/workflows";

interface CreateWorkflowModalProps {
  onClose: () => void;
  onCreated: (workflow: AgentWorkflow) => void;
}

// FR-067/FR-068: initiate an asset lifecycle evaluation. Returns immediately
// with a workflow id — see EvaluatePolicyModal for the next step, since the
// Planner/Maintenance/Budget agents that would normally run automatically
// between here and there don't exist yet (SRS §7.2 nodes 1-3).
export default function CreateWorkflowModal({ onClose, onCreated }: CreateWorkflowModalProps) {
  const createWorkflow = useCreateWorkflow();
  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = assetsData?.items ?? [];

  const [assetId, setAssetId] = useState("");
  const [objective, setObjective] = useState("");

  const canSubmit = assetId.length > 0 && objective.trim().length > 0;

  const handleSubmit = () => {
    if (!canSubmit || createWorkflow.isPending) return;
    createWorkflow.mutate(
      { asset_id: assetId, objective: objective.trim() },
      { onSuccess: onCreated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Agentic Workflows"
      modalHeading="New asset lifecycle evaluation"
      primaryButtonText={createWorkflow.isPending ? "Starting…" : "Start evaluation"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || createWorkflow.isPending}
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
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <ComboBox
          id="workflow-asset"
          titleText="Asset"
          placeholder={isLoadingAssets ? "Loading assets…" : "Choose an asset…"}
          items={assets}
          itemToString={(item) => (item ? `${item.asset_code} - ${item.name}` : "")}
          selectedItem={assets.find((a) => a.id === assetId) ?? null}
          onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
        />
        <TextArea
          id="workflow-objective"
          labelText="Objective"
          placeholder="e.g. Should this asset be repaired, replaced or disposed of?"
          value={objective}
          onChange={(e) => setObjective(e.target.value)}
          rows={3}
        />
      </div>
    </Modal>
  );
}
