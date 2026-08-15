import { useState } from "react";
import { Modal, Select, SelectItem, InlineNotification } from "@carbon/react";
import { useUpdateUser } from "../hooks/useUsers";
import { getRoleLabel, type CoreGridRole } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { CoreGridUser } from "../services/users";
import type { Department } from "@/features/assets/types/asset";

const ASSIGNABLE_ROLES: CoreGridRole[] = ["InventoryOfficer", "Auditor", "Staff", "Administrator"];

interface EditUserModalProps {
  user: CoreGridUser;
  departments: Department[];
  onClose: () => void;
  onSaved: () => void;
}

// FR-014: change a user's role or department assignment.
export default function EditUserModal({ user, departments, onClose, onSaved }: EditUserModalProps) {
  const [role, setRole] = useState<CoreGridRole>(user.role);
  const [departmentId, setDepartmentId] = useState(user.department_id ?? "");

  const updateUser = useUpdateUser();

  const handleSubmit = () => {
    if (updateUser.isPending) return;
    updateUser.mutate(
      { id: user.id, payload: { role, department_id: departmentId || null } },
      { onSuccess: onSaved },
    );
  };

  return (
    <Modal
      open
      modalLabel="Users & Roles"
      modalHeading={`Edit ${user.given_name} ${user.family_name}`}
      primaryButtonText={updateUser.isPending ? "Saving…" : "Save"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={updateUser.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {updateUser.isError && (
        <InlineNotification
          kind="error"
          title="Could not update user"
          subtitle={getErrorMessage(updateUser.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <Select
          id="edit-user-role"
          labelText="Role"
          value={role}
          onChange={(e) => setRole(e.target.value as CoreGridRole)}
        >
          {ASSIGNABLE_ROLES.map((r) => (
            <SelectItem key={r} value={r} text={getRoleLabel(r)} />
          ))}
        </Select>
        <Select
          id="edit-user-department"
          labelText="Department"
          value={departmentId}
          onChange={(e) => setDepartmentId(e.target.value)}
        >
          <SelectItem value="" text="Unassigned" />
          {departments.map((d) => (
            <SelectItem key={d.id} value={d.id} text={d.name} />
          ))}
        </Select>
      </div>
    </Modal>
  );
}
