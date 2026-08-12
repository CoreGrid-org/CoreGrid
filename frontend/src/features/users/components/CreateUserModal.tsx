import { useState } from "react";
import { Modal, TextInput, PasswordInput, Select, SelectItem, InlineNotification } from "@carbon/react";
import { useCreateUser } from "../hooks/useUsers";
import { getRoleLabel, type CoreGridRole } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { CreateUserResponse } from "../services/users";

const ASSIGNABLE_ROLES: CoreGridRole[] = ["InventoryOfficer", "Auditor", "Staff", "Administrator"];

interface CreateUserModalProps {
  onClose: () => void;
  onCreated: (user: CreateUserResponse) => void;
}

// Mounted only while the "Add user" modal is open (see AdminDashboard), so a
// fresh instance — and fresh form/mutation state — is what you get each time.
export default function CreateUserModal({ onClose, onCreated }: CreateUserModalProps) {
  const createUser = useCreateUser();

  const [givenName, setGivenName] = useState("");
  const [familyName, setFamilyName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<CoreGridRole>("Staff");

  const canSubmit =
    givenName.trim().length > 0 && familyName.trim().length > 0 && email.trim().length > 0 && password.length >= 8;

  const handleSubmit = () => {
    if (!canSubmit || createUser.isPending) return;
    createUser.mutate(
      {
        given_name: givenName.trim(),
        family_name: familyName.trim(),
        email: email.trim(),
        password,
        role,
      },
      { onSuccess: onCreated },
    );
  };

  return (
    <Modal
      open
      modalLabel="CoreGrid"
      modalHeading="Add user"
      primaryButtonText={createUser.isPending ? "Creating…" : "Create user"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || createUser.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {createUser.isError && (
        <InlineNotification
          kind="error"
          title="Could not create user"
          subtitle={getErrorMessage(createUser.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <TextInput
            id="create-user-given-name"
            labelText="First Name"
            value={givenName}
            onChange={(e) => setGivenName(e.target.value)}
          />
          <TextInput
            id="create-user-family-name"
            labelText="Last Name"
            value={familyName}
            onChange={(e) => setFamilyName(e.target.value)}
          />
        </div>
        <TextInput
          id="create-user-email"
          labelText="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <PasswordInput
          id="create-user-password"
          labelText="Temporary Password"
          helperText="At least 8 characters. Share this with the user directly — CoreGrid doesn't email it."
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          invalid={password.length > 0 && password.length < 8}
          invalidText="Must be at least 8 characters."
        />
        <Select
          id="create-user-role"
          labelText="Role"
          value={role}
          onChange={(e) => setRole(e.target.value as CoreGridRole)}
        >
          {ASSIGNABLE_ROLES.map((r) => (
            <SelectItem key={r} value={r} text={getRoleLabel(r)} />
          ))}
        </Select>
      </div>
    </Modal>
  );
}
