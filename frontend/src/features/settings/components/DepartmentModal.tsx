import { useState } from "react";
import { Modal, TextInput, InlineNotification } from "@carbon/react";
import { useCreateDepartment, useUpdateDepartment } from "../hooks/useOrgConfig";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Department } from "@/features/assets/types/asset";

interface DepartmentModalProps {
  department?: Department;
  /** Other departments' codes, to catch a duplicate before saving. */
  existingCodes: string[];
  onClose: () => void;
  onSaved: (department: Department) => void;
}

type Touched = Partial<Record<"code" | "name", boolean>>;

// Create or amend a department; a fresh instance mounts each time it opens.
export default function DepartmentModal({ department, existingCodes, onClose, onSaved }: DepartmentModalProps) {
  const [code, setCode] = useState(department?.code ?? "");
  const [name, setName] = useState(department?.name ?? "");
  const [touched, setTouched] = useState<Touched>({});

  const markTouched = (field: keyof Touched) => setTouched((t) => ({ ...t, [field]: true }));

  const createDepartment = useCreateDepartment();
  const updateDepartment = useUpdateDepartment();
  const mutation = department ? updateDepartment : createDepartment;

  // Mirrors Create/UpdateDepartmentRequest's [Required]/[MaxLength]
  // (backend/Features/OrgConfig/DTOs).
  const trimmedCode = code.trim();
  const codeError = !trimmedCode
    ? "Code is required."
    : existingCodes.some((c) => c.toLowerCase() === trimmedCode.toLowerCase())
      ? "Another department already uses this code."
      : undefined;
  const nameError = !name.trim() ? "Name is required." : undefined;
  const isUnchanged = Boolean(department) && trimmedCode === department?.code && name.trim() === department?.name;

  const handleSubmit = () => {
    setTouched({ code: true, name: true });
    if (codeError || nameError || isUnchanged || mutation.isPending) return;
    const payload = { code: trimmedCode, name: name.trim() };
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
      primaryButtonText={mutation.isPending ? "Saving…" : department ? "Save changes" : "Add department"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={mutation.isPending || isUnchanged}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <div className="cg-modal-form">
        {mutation.isError && (
          <InlineNotification
            kind="error"
            title="Could not save department"
            subtitle={getErrorMessage(mutation.error, "Something went wrong. Please try again.")}
            hideCloseButton
            lowContrast
            style={{ maxWidth: "100%" }}
          />
        )}
        <div className="cg-modal-form__row cg-department-fields">
          <TextInput
            id="department-code"
            labelText="Code"
            placeholder="e.g. FLT"
            helperText="Short and unique. Shown in capitals."
            maxLength={20}
            value={code}
            // Codes are identifiers; keep them consistently upper case.
            onChange={(e) => setCode(e.target.value.toUpperCase().replace(/\s+/g, ""))}
            onBlur={() => markTouched("code")}
            invalid={Boolean(touched.code && codeError)}
            invalidText={codeError}
          />
          <TextInput
            id="department-name"
            labelText="Name"
            placeholder="e.g. Fleet Operations"
            maxLength={200}
            value={name}
            onChange={(e) => setName(e.target.value)}
            onBlur={() => markTouched("name")}
            invalid={Boolean(touched.name && nameError)}
            invalidText={nameError}
          />
        </div>
      </div>
    </Modal>
  );
}
