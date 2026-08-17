import { Modal, InlineNotification } from "@carbon/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";

interface ConfirmDeleteModalProps {
  heading: string;
  itemName: string;
  isPending: boolean;
  isError: boolean;
  error: unknown;
  onConfirm: () => void;
  onClose: () => void;
}

// Deletion is branch-y server-side (backend/Features/Assets/Services —
// AssetCategoryService.DeleteCategoryAsync / AssetTypeService.DeleteAssetTypeAsync
// / DeleteAttributeDefinitionAsync): permanently removed if nothing
// references it, otherwise deactivated instead. This dialog can't know
// which will happen ahead of the request, so it names both outcomes.
export default function ConfirmDeleteModal({
  heading,
  itemName,
  isPending,
  isError,
  error,
  onConfirm,
  onClose,
}: ConfirmDeleteModalProps) {
  return (
    <Modal
      open
      danger
      modalHeading={heading}
      primaryButtonText={isPending ? "Deleting…" : "Delete"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={isPending}
      onRequestClose={onClose}
      onRequestSubmit={onConfirm}
    >
      {isError && (
        <InlineNotification
          kind="error"
          title="Could not delete"
          subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <p>
        Delete <strong>{itemName}</strong>? If nothing else in the register references it, it's
        permanently removed. If it's still in use, it's deactivated instead — hidden from new
        selections but existing records keep working, and it can be reactivated later.
      </p>
    </Modal>
  );
}
