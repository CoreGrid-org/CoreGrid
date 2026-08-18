import { useState } from "react";
import {
  Modal,
  FormGroup,
  TextInput,
  ComboBox,
} from "@carbon/react";
import { useApproveMaintenance } from "../hooks/useMaintenance";
import { useUsersList } from "@/features/users/hooks/useUsers";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { MaintenanceRecord } from "../types/maintenance";

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
      <p style={{ marginBottom: "1rem" }}>
        Approving this record will transition it from REQUESTED to APPROVED.
      </p>
      {(error || approveMaintenance.isError) && (
        <p style={{ color: "#da1e28", marginBottom: "1rem" }}>
          {error || getErrorMessage(approveMaintenance.error, "Failed to approve maintenance.")}
        </p>
      )}
      
      <FormGroup legendText="">
        <ComboBox
          id="assignee"
          titleText="Assign to Officer"
          placeholder={isLoadingUsers ? "Loading users..." : "Select an officer"}
          items={users ?? []}
          itemToString={(item) => (item ? `${item.given_name} ${item.family_name} (${item.email})` : "")}
          selectedItem={users?.find((o) => o.id === assigneeId) ?? null}
          onChange={({ selectedItem }) => setAssigneeId(selectedItem?.id ?? "")}
          style={{ marginBottom: "1rem" }}
        />
        
        <TextInput
          id="estimatedCost"
          labelText="Estimated Cost (LKR)"
          placeholder="0.00"
          value={estimatedCost}
          onChange={(e) => setEstimatedCost(e.target.value)}
          type="number"
          min="0"
          step="0.01"
        />
      </FormGroup>
    </Modal>
  );
}
