import { useState } from "react";
import {
  Modal,
  FormGroup,
  TextInput,
  TextArea,
  Select,
  SelectItem,
} from "@carbon/react";
import { useCompleteMaintenance } from "../hooks/useMaintenance";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { MaintenanceRecord } from "../types/maintenance";

interface CompleteMaintenanceModalProps {
  isOpen: boolean;
  onClose: () => void;
  record: MaintenanceRecord;
  onSuccess: () => void;
}

export default function CompleteMaintenanceModal({
  isOpen,
  onClose,
  record,
  onSuccess,
}: CompleteMaintenanceModalProps) {
  const completeMaintenance = useCompleteMaintenance();

  const [actualCost, setActualCost] = useState("");
  const [workPerformed, setWorkPerformed] = useState("");
  const [completionDate, setCompletionDate] = useState(
    new Date().toISOString().split("T")[0]
  );
  const [resultingCondition, setCondition] = useState("");
  const [overspendJustification, setOverspendJustification] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = () => {
    if (!actualCost || isNaN(Number(actualCost)) || Number(actualCost) < 0) {
      setError("Please enter a valid actual cost.");
      return;
    }
    if (!workPerformed || !resultingCondition || !completionDate) {
      setError("Please fill in all required fields.");
      return;
    }
    setError(null);

    completeMaintenance.mutate(
      {
        id: record.id,
        payload: {
          actual_cost: Number(actualCost),
          work_performed: workPerformed,
          completion_date: completionDate,
          resulting_condition: resultingCondition,
          overspend_justification: overspendJustification || undefined,
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
      modalHeading="Complete Maintenance"
      primaryButtonText={completeMaintenance.isPending ? "Completing..." : "Complete"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={completeMaintenance.isPending}
    >
      <p className="cg-modal-intro">
        Record the actual cost, work performed, and the resulting asset condition.
      </p>
      {(error || completeMaintenance.isError) && (
        <p className="cg-text-error">
          {error || getErrorMessage(completeMaintenance.error, "Failed to complete maintenance.")}
        </p>
      )}

      <FormGroup legendText="">
        <div className="cg-stack">
          <TextInput
            id="actualCost"
            labelText="Actual Cost (LKR)"
            placeholder="0.00"
            value={actualCost}
            onChange={(e) => setActualCost(e.target.value)}
            type="number"
            min="0"
            step="0.01"
          />

          <TextArea
            id="workPerformed"
            labelText="Work Performed"
            placeholder="Describe the work done..."
            value={workPerformed}
            onChange={(e) => setWorkPerformed(e.target.value)}
            rows={3}
          />

          <TextInput
            id="completionDate"
            labelText="Completion Date"
            type="date"
            value={completionDate}
            onChange={(e) => setCompletionDate(e.target.value)}
          />

          <Select
            id="resultingCondition"
            labelText="Resulting Condition"
            value={resultingCondition}
            onChange={(e) => setCondition(e.target.value)}
          >
            <SelectItem value="" text="Choose condition..." disabled hidden />
            <SelectItem value="GOOD" text="Good" />
            <SelectItem value="FAIR" text="Fair" />
            <SelectItem value="POOR" text="Poor" />
            <SelectItem value="UNSERVICEABLE" text="Unserviceable" />
          </Select>

          <TextArea
            id="overspendJustification"
            labelText="Overspend Justification (Optional)"
            placeholder="Required if cost greatly exceeds estimate..."
            value={overspendJustification}
            onChange={(e) => setOverspendJustification(e.target.value)}
            rows={2}
          />
        </div>
      </FormGroup>
    </Modal>
  );
}
