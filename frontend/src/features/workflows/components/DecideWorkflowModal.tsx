import { useState } from "react";
import { Modal, TextArea, InlineNotification } from "@carbon/react";
import { useDecideWorkflow } from "../hooks/useWorkflows";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { AgentWorkflow } from "../api/workflows";

interface DecideWorkflowModalProps {
  workflow: AgentWorkflow;
  decision: "APPROVE" | "REJECT" | "REVISE";
  onClose: () => void;
  onDecided: () => void;
}

const HEADINGS: Record<string, string> = {
  APPROVE: "Approve recommendation",
  REJECT: "Reject recommendation",
  REVISE: "Request revision",
};

// AI-14/AI-16: Administrator-only; a decision reason of at least 10
// characters is required and is captured with the decider, the timestamp
// and a snapshot of workflow state at the moment of decision.
export default function DecideWorkflowModal({ workflow, decision, onClose, onDecided }: DecideWorkflowModalProps) {
  const decide = useDecideWorkflow();
  const [reason, setReason] = useState("");

  const canSubmit = reason.trim().length >= 10;

  const handleSubmit = () => {
    if (!canSubmit || decide.isPending) return;
    decide.mutate({ id: workflow.id, payload: { decision, reason: reason.trim() } }, { onSuccess: onDecided });
  };

  return (
    <Modal
      open
      modalLabel={`${workflow.asset_code} — ${workflow.recommendation ?? "recommendation"}`}
      modalHeading={HEADINGS[decision]}
      primaryButtonText={decide.isPending ? "Recording…" : "Confirm"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || decide.isPending}
      danger={decision === "REJECT"}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {decide.isError && (
        <InlineNotification
          kind="error"
          title="Could not record this decision"
          subtitle={getErrorMessage(decide.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <TextArea
        id="decide-reason"
        labelText="Reason"
        helperText="At least 10 characters — recorded with your name and the timestamp."
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        rows={3}
        invalid={reason.length > 0 && reason.trim().length < 10}
        invalidText="Must be at least 10 characters."
      />
    </Modal>
  );
}
