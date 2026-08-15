import { useState } from "react";
import { Modal, TextInput, InlineNotification } from "@carbon/react";
import { useCreateAssetCategory } from "../hooks/useAssets";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { AssetCategory } from "../types/asset";

interface CreateAssetCategoryModalProps {
  onClose: () => void;
  onCreated: (category: AssetCategory) => void;
}

// POST /api/asset-categories.
export default function CreateAssetCategoryModal({ onClose, onCreated }: CreateAssetCategoryModalProps) {
  const createCategory = useCreateAssetCategory();

  const [code, setCode] = useState("");
  const [name, setName] = useState("");

  const canSubmit = code.trim().length > 0 && code.trim().length <= 20 && name.trim().length > 0;

  const handleSubmit = () => {
    if (!canSubmit || createCategory.isPending) return;
    createCategory.mutate(
      { code: code.trim(), name: name.trim() },
      { onSuccess: onCreated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Asset Config"
      modalHeading="New category"
      primaryButtonText={createCategory.isPending ? "Creating…" : "Create category"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || createCategory.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {createCategory.isError && (
        <InlineNotification
          kind="error"
          title="Could not create category"
          subtitle={getErrorMessage(createCategory.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput
          id="create-category-code"
          labelText="Code"
          helperText="Short uppercase abbreviation, e.g. IT, FU, VH. Max 20 characters."
          value={code}
          onChange={(e) => setCode(e.target.value)}
          invalid={code.trim().length > 20}
          invalidText="Code cannot be longer than 20 characters."
        />
        <TextInput
          id="create-category-name"
          labelText="Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </div>
    </Modal>
  );
}
