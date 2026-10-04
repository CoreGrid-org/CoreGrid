import { useState } from "react";
import {
  Modal,
  FormGroup,
  TextInput,
  ComboBox,
  Button,
  SkeletonText,
  Tag,
} from "@carbon/react";
import { MachineLearningModel } from "@carbon/icons-react";
import { useApproveMaintenance, useCostSuggestion } from "../hooks/useMaintenance";
import { useUsersList } from "@/features/users/hooks/useUsers";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { MaintenanceCostSuggestion, MaintenanceRecord } from "../types/maintenance";

const CONFIDENCE_TAG: Record<MaintenanceCostSuggestion["confidence"], "green" | "blue" | "gray"> = {
  HIGH: "green",
  MEDIUM: "blue",
  LOW: "gray",
  NONE: "gray",
};

const formatLkr = (value: number) => `LKR ${value.toLocaleString()}`;

// The estimator's suggestion, with a one-click "Use"; the field itself stays
// free to edit, so the approver can always override it.
function CostSuggestion({
  suggestion,
  isLoading,
  onUse,
}: {
  suggestion: MaintenanceCostSuggestion | undefined;
  isLoading: boolean;
  onUse: (value: number) => void;
}) {
  if (isLoading) return <SkeletonText lineCount={2} paragraph />;
  if (!suggestion) return null;

  if (suggestion.suggested_cost === null) {
    return (
      <div className="cg-cost-suggestion cg-cost-suggestion--empty">
        <MachineLearningModel size={16} className="cg-cost-suggestion__icon" />
        <p className="cg-cost-suggestion__text">{suggestion.basis_label}. Enter the estimate manually.</p>
      </div>
    );
  }

  const suggested = suggestion.suggested_cost;
  return (
    <div className="cg-cost-suggestion">
      <MachineLearningModel size={16} className="cg-cost-suggestion__icon" />
      <div className="cg-cost-suggestion__body">
        <div className="cg-cost-suggestion__header">
          <span className="cg-cost-suggestion__value">Suggested {formatLkr(suggested)}</span>
          <Tag size="sm" type={CONFIDENCE_TAG[suggestion.confidence]}>
            {suggestion.confidence.toLowerCase()} confidence
          </Tag>
        </div>
        <p className="cg-cost-suggestion__text">
          {suggestion.low_cost !== null && suggestion.high_cost !== null && suggestion.low_cost !== suggestion.high_cost
            ? `Typical range ${formatLkr(suggestion.low_cost)} – ${formatLkr(suggestion.high_cost)}. `
            : ""}
          Based on {suggestion.sample_size} completed record{suggestion.sample_size === 1 ? "" : "s"} of {suggestion.basis_label}.
        </p>
      </div>
      <Button kind="ghost" size="sm" onClick={() => onUse(suggested)}>
        Use
      </Button>
    </div>
  );
}

interface ApproveMaintenanceModalProps {
  isOpen: boolean;
  onClose: () => void;
  record: MaintenanceRecord;
  onSuccess: () => void;
}

export default function ApproveMaintenanceModal({
  isOpen,
  onClose,
  record,
  onSuccess,
}: ApproveMaintenanceModalProps) {
  const approveMaintenance = useApproveMaintenance();
  const { data: users, isLoading: isLoadingUsers } = useUsersList();
  const costSuggestion = useCostSuggestion(record.id, isOpen);
  
  const [estimatedCost, setEstimatedCost] = useState("");
  const [assigneeId, setAssigneeId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = () => {
    if (!estimatedCost || isNaN(Number(estimatedCost)) || Number(estimatedCost) < 0) {
      setError("Please enter a valid estimated cost.");
      return;
    }
    if (!assigneeId) {
      setError("Please select an assignee.");
      return;
    }
    setError(null);

    approveMaintenance.mutate(
      {
        id: record.id,
        payload: {
          estimated_cost: Number(estimatedCost),
          assignee_id: assigneeId,
        },
      },
      {
        onSuccess: () => {
          onSuccess();
          onClose();
        },
      }
    );
  };

  return (
    <Modal
      open={isOpen}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
      modalHeading="Approve Maintenance"
      primaryButtonText={approveMaintenance.isPending ? "Approving..." : "Approve"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={approveMaintenance.isPending}
      danger={false}
    >
      <p className="cg-modal-intro">
        Approving this record will transition it from REQUESTED to APPROVED.
      </p>
      {(error || approveMaintenance.isError) && (
        <p className="cg-text-error">
          {error || getErrorMessage(approveMaintenance.error, "Failed to approve maintenance.")}
        </p>
      )}
      
      <FormGroup legendText="">
        <div className="cg-stack">
          <ComboBox
            id="assignee"
            titleText="Assign to Officer"
            placeholder={isLoadingUsers ? "Loading users..." : "Select an officer"}
            items={users ?? []}
            itemToString={(item) => (item ? `${item.given_name} ${item.family_name} (${item.email})` : "")}
            selectedItem={users?.find((o) => o.id === assigneeId) ?? null}
            onChange={({ selectedItem }) => setAssigneeId(selectedItem?.id ?? "")}
          />
        
          <CostSuggestion
            suggestion={costSuggestion.data}
            isLoading={costSuggestion.isLoading}
            onUse={(value) => setEstimatedCost(String(value))}
          />

          <TextInput
            id="estimatedCost"
            labelText="Estimated Cost (LKR)"
            helperText="Use the suggestion or enter your own figure."
            placeholder={costSuggestion.data?.suggested_cost != null ? String(costSuggestion.data.suggested_cost) : "0.00"}
            value={estimatedCost}
            onChange={(e) => setEstimatedCost(e.target.value)}
            type="number"
            min="0"
            step="0.01"
          />
        </div>
      </FormGroup>
    </Modal>
  );
}
