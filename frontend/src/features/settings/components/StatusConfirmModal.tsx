import { Modal, InlineNotification } from "@carbon/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";

// Confirms deactivating (or reactivating) a department or location before
// it happens: deactivating hides it from pickers across the app.
export default function StatusConfirmModal({
  noun,
  name,
  isActive,
  consequence,
  isPending,
  error,
  onConfirm,
  onClose,
}: {
  /** "department" / "location". */
  noun: string;
  name: string;
  isActive: boolean;
  /** What deactivating does, in plain words. */
  consequence: string;
  isPending: boolean;
  error: unknown;
  onConfirm: () => void;
  onClose: () => void;
}) {
  return (
    <Modal
      open
      size="sm"
      danger={isActive}
      modalLabel="Organisation Settings"
      modalHeading={`${isActive ? "Deactivate" : "Reactivate"} ${noun}?`}
      primaryButtonText={isPending ? "Saving…" : isActive ? "Deactivate" : "Reactivate"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={isPending}
      onRequestClose={onClose}
      onRequestSubmit={onConfirm}
    >
      <div className="cg-modal-form">
        <p className="cg-modal-form__intro">
          <strong>{name}</strong>{" "}
          {isActive ? consequence : "will be available again everywhere it was before. Nothing else changes."}
        </p>
        {Boolean(error) && (
          <InlineNotification
            kind="error"
            title={`Could not ${isActive ? "deactivate" : "reactivate"} ${noun}`}
            subtitle={getErrorMessage(error, "It may still have active assets assigned to it.")}
            lowContrast
            hideCloseButton
            style={{ maxWidth: "100%" }}
          />
        )}
      </div>
    </Modal>
  );
}
