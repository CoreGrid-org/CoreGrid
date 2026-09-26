import { useState } from "react";
import { Modal, ComboBox, InlineNotification } from "@carbon/react";
import { useUpdateUser } from "../hooks/useUsers";
import type { CoreGridRole } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import type { CoreGridUser } from "../services/users";
import type { Department } from "@/features/assets/types/asset";
import RolePicker from "./RolePicker";
import UserIdentity from "./UserIdentity";

interface EditUserModalProps {
  user: CoreGridUser;
  departments: Department[];
  isSelf: boolean;
  onClose: () => void;
  onSaved: () => void;
}

// Change a user's role or department assignment.
export default function EditUserModal({ user, departments, isSelf, onClose, onSaved }: EditUserModalProps) {
  const [role, setRole] = useState<CoreGridRole>(user.role);
  const [departmentId, setDepartmentId] = useState(user.department_id ?? "");

  const updateUser = useUpdateUser();
  const selectedDepartment = departments.find((d) => d.id === departmentId) ?? null;
  const isUnchanged = role === user.role && departmentId === (user.department_id ?? "");
  const leavingAdmin = user.role === "Administrator" && role !== "Administrator";

  const handleSubmit = () => {
    if (updateUser.isPending || isUnchanged) return;
    updateUser.mutate(
      { id: user.id, payload: { role, department_id: departmentId || null } },
      { onSuccess: onSaved },
    );
  };

  return (
    <Modal
      open
      size="md"
      modalLabel="Users & Roles"
      modalHeading="Edit user"
      primaryButtonText={updateUser.isPending ? "Saving…" : "Save changes"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={updateUser.isPending || isUnchanged}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <div className="cg-modal-form">
        <UserIdentity user={user} isSelf={isSelf} />

        {updateUser.isError && (
          <InlineNotification
            kind="error"
            title="Could not update user"
            subtitle={getErrorMessage(updateUser.error, "Something went wrong. Please try again.")}
            hideCloseButton
            lowContrast
            style={{ maxWidth: "100%" }}
          />
        )}

        {leavingAdmin && (
          <InlineNotification
            kind="warning"
            title={isSelf ? "You're removing your own admin access" : "This removes their admin access"}
            subtitle={
              isSelf
                ? "You'll lose access to Users & Roles and settings as soon as you save. The organisation must keep at least one active Administrator."
                : "The organisation must keep at least one active Administrator."
            }
            hideCloseButton
            lowContrast
            style={{ maxWidth: "100%" }}
          />
        )}

        <RolePicker name="edit-user-role" value={role} onChange={setRole} />

        <ComboBox<Department>
          id="edit-user-department"
          titleText="Department"
          helperText="Leave empty for no department."
          placeholder="Type to search departments…"
          autoAlign
          items={departments}
          itemToString={(d) => d?.name ?? ""}
          selectedItem={selectedDepartment}
          shouldFilterItem={comboBoxFilter(selectedDepartment)}
          onChange={({ selectedItem }) => setDepartmentId(selectedItem?.id ?? "")}
        />
      </div>
    </Modal>
  );
}
