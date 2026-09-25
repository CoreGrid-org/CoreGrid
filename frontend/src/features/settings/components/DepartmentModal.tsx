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

type Touched = Partial<Record<"code" | "name", boolean>>;

// Create or amend a department  — same modal either way, following
// CreateUserModal's pattern; a fresh instance mounts each time it opens.
export default function DepartmentModal({ department, onClose, onSaved }: DepartmentModalProps) {
  const [code, setCode] = useState(department?.code ?? "");
  const [name, setName] = useState(department?.name ?? "");
  const [touched, setTouched] = useState<Touched>({});

  const markTouched = (field: keyof Touched) => setTouched((t) => ({ ...t, [field]: true }));

  const createDepartment = useCreateDepartment();
  const updateDepartment = useUpdateDepartment();
  const mutation = department ? updateDepartment : createDepartment;

  // Mirrors Create/UpdateDepartmentRequest's [Required]/[MaxLength]
  // (backend/Features/OrgConfig/DTOs).
  const codeValid = code.trim().length > 0 && code.trim().length <= 20;
  const nameValid = name.trim().length > 0 && name.trim().length <= 200;
  const codeInvalid = touched.code && !codeValid;
  const nameInvalid = touched.name && !nameValid;

  const canSubmit = codeValid && nameValid;

  const handleSubmit = () => {
    setTouched({ code: true, name: true });
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
          className="cg-panel-notification"
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput
          id="department-code"
          labelText="Code"
          helperText="A short, unique identifier — e.g. FLT for Fleet Operations."
          maxLength={20}
          value={code}
          onChange={(e) => setCode(e.target.value)}
          onBlur={() => markTouched("code")}
          invalid={!!codeInvalid}
          invalidText="Code is required."
        />
        <TextInput
          id="department-name"
          labelText="Name"
          maxLength={200}
          value={name}
          onChange={(e) => setName(e.target.value)}
          onBlur={() => markTouched("name")}
          invalid={!!nameInvalid}
          invalidText="Name is required."
        />
      </div>
    </Modal>
  );
}
