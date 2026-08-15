import { useState } from "react";
import { Modal, TextInput, InlineNotification } from "@carbon/react";
import { useCreateDepartment, useUpdateDepartment } from "../hooks/useOrgConfig";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Department } from "@/features/assets/types/asset";

interface DepartmentModalProps {
  department?: Department;
  onClose: () => void;
  onSaved: () => void;
}

// Create or amend a department (FR-010) — same modal either way, following
// CreateUserModal's pattern; a fresh instance mounts each time it opens.
export default function DepartmentModal({ department, onClose, onSaved }: DepartmentModalProps) {
  const [code, setCode] = useState(department?.code ?? "");
  const [name, setName] = useState(department?.name ?? "");

  const createDepartment = useCreateDepartment();
  const updateDepartment = useUpdateDepartment();
  const mutation = department ? updateDepartment : createDepartment;

  const canSubmit = code.trim().length > 0 && name.trim().length > 0;

  const handleSubmit = () => {
    if (!canSubmit || mutation.isPending) return;
    const payload = { code: code.trim(), name: name.trim() };
    if (department) {
      updateDepartment.mutate({ id: department.id, payload }, { onSuccess: onSaved });
    } else {
      createDepartment.mutate(payload, { onSuccess: onSaved });
    }
  };

  return (
    <Modal
      open
      modalLabel="Organisation Settings"
      modalHeading={department ? "Edit department" : "Add department"}
      primaryButtonText={mutation.isPending ? "Saving…" : "Save"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || mutation.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {mutation.isError && (
        <InlineNotification
          kind="error"
          title="Could not save department"
          subtitle={getErrorMessage(mutation.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput
          id="department-code"
          labelText="Code"
          helperText="A short, unique identifier — e.g. FLT for Fleet Operations."
          value={code}
          onChange={(e) => setCode(e.target.value)}
        />
        <TextInput id="department-name" labelText="Name" value={name} onChange={(e) => setName(e.target.value)} />
      </div>
    </Modal>
  );
}
