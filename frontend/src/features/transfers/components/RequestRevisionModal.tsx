import { useState } from "react";
import { Modal, TextArea, InlineNotification } from "@carbon/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useRequestDisposalRevision } from "../hooks/useDisposals";
import type { DisposalResponse } from "../types";

// Matches RequestDisposalRevisionRequest's [MaxLength(2000)] on the backend.
const COMMENTS_MAX = 2000;

// Returns a pending disposal request to its requester with comments.
export default function RequestRevisionModal({
  disposal,
  onClose,
  onRequested,
}: {
  disposal: DisposalResponse;
  onClose: () => void;
  onRequested: () => void;
}) {
  const [comments, setComments] = useState("");
  const requestRevision = useRequestDisposalRevision();

  const handleSubmit = () => {
    if (!comments.trim() || requestRevision.isPending) return;
    requestRevision.mutate({ id: disposal.id, payload: { comments: comments.trim() } }, { onSuccess: onRequested });
  };

  return (
    <Modal
      open
      modalLabel="Disposals"
      modalHeading={`Request revision: ${disposal.asset_code}`}
      primaryButtonText={requestRevision.isPending ? "Sending…" : "Send back for revision"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={requestRevision.isPending || !comments.trim()}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <div className="cg-modal-form">
        <p className="cg-modal-form__intro">
          The disposal request for <strong>{disposal.asset_name}</strong> goes back to its requester with your comments,
          marked as <strong>Revision requested</strong>.
        </p>

        {requestRevision.isError && (
          <InlineNotification
            kind="error"
            title="Could not request revision"
            subtitle={getErrorMessage(requestRevision.error, "An error occurred while returning the disposal for revision.")}
            lowContrast
            hideCloseButton
            style={{ maxWidth: "100%" }}
          />
        )}

        <TextArea
          id="revision-comments"
          labelText="What needs to change?"
          placeholder="e.g. Attach a second valuation quote and confirm the scrap dealer is licensed."
          rows={4}
          enableCounter
          maxCount={COMMENTS_MAX}
          value={comments}
          onChange={(e) => setComments(e.target.value)}
        />
      </div>
    </Modal>
  );
}
