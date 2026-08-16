import { useState } from "react";
import { Modal, TextInput, InlineNotification } from "@carbon/react";
import { useUpdateAssetCategory } from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { AssetCategory } from "../types/asset";

interface EditAssetCategoryModalProps {
  category: AssetCategory;
  onClose: () => void;
  onUpdated: (category: AssetCategory) => void;
}

// PUT /api/asset-categories/{id}.
export default function EditAssetCategoryModal({ category, onClose, onUpdated }: EditAssetCategoryModalProps) {
  const updateCategory = useUpdateAssetCategory();

  const [code, setCode] = useState(category.code);
  const [name, setName] = useState(category.name);

  const canSubmit = code.trim().length > 0 && code.trim().length <= 20 && name.trim().length > 0;

  const handleSubmit = () => {
    if (!canSubmit || updateCategory.isPending) return;
    updateCategory.mutate(
      { id: category.id, payload: { code: code.trim(), name: name.trim() } },
      { onSuccess: onUpdated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Asset Config"
      modalHeading="Edit category"
      primaryButtonText={updateCategory.isPending ? "Saving…" : "Save changes"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || updateCategory.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {updateCategory.isError && (
        <InlineNotification
          kind="error"
          title="Could not update category"
          subtitle={getErrorMessage(updateCategory.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput
          id="edit-category-code"
          labelText="Code"
          helperText="Short uppercase abbreviation, e.g. IT, FU, VH. Max 20 characters."
          value={code}
          onChange={(e) => setCode(e.target.value)}
          invalid={code.trim().length > 20}
          invalidText="Code cannot be longer than 20 characters."
        />
        <TextInput
          id="edit-category-name"
          labelText="Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </div>
    </Modal>
  );
}
