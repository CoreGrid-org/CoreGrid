import { useState } from "react";
import { Modal, FormGroup, TextArea } from "@carbon/react";
import { useCancelMaintenance } from "../hooks/useMaintenance";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { MaintenanceRecord } from "../types/maintenance";

interface CancelMaintenanceModalProps {
  isOpen: boolean;
  onClose: () => void;
  record: MaintenanceRecord;
  onSuccess: () => void;
}

export default function CancelMaintenanceModal({
  isOpen,
  onClose,
  record,
  onSuccess,
}: CancelMaintenanceModalProps) {
  const cancelMaintenance = useCancelMaintenance();

  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = () => {
    if (!reason.trim()) {
      setError("Please provide a cancellation reason.");
      return;
    }
    setError(null);

    cancelMaintenance.mutate(
      {
        id: record.id,
        payload: { reason },
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
      modalHeading="Cancel Maintenance"
      primaryButtonText={cancelMaintenance.isPending ? "Cancelling..." : "Confirm Cancellation"}
      secondaryButtonText="Keep Record"
      primaryButtonDisabled={cancelMaintenance.isPending}
      danger={true}
    >
      <p style={{ marginBottom: "1rem" }}>
        Are you sure you want to cancel this maintenance record? This action cannot be undone.
      </p>
      {(error || cancelMaintenance.isError) && (
        <p style={{ color: "#da1e28", marginBottom: "1rem" }}>
          {error || getErrorMessage(cancelMaintenance.error, "Failed to cancel maintenance.")}
        </p>
      )}

      <FormGroup legendText="">
        <TextArea
          id="cancelReason"
          labelText="Cancellation Reason"
          placeholder="Why is this record being cancelled?"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={3}
          required
        />
      </FormGroup>
    </Modal>
  );
}
