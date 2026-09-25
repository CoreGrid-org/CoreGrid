import { useState } from "react";
import { Modal, TextInput, PasswordInput, Select, SelectItem, InlineNotification } from "@carbon/react";
import { useCreateUser } from "../hooks/useUsers";
import { getRoleLabel, type CoreGridRole } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { isValidEmail } from "@/shared/lib/validation";
import type { CreateUserResponse } from "../services/users";

const ASSIGNABLE_ROLES: CoreGridRole[] = ["InventoryOfficer", "Auditor", "Staff", "Administrator"];

interface CreateUserModalProps {
  onClose: () => void;
  onCreated: (user: CreateUserResponse) => void;
}

type Touched = Partial<Record<"givenName" | "familyName" | "email" | "password", boolean>>;

// Mounted only while the "Add user" modal is open (see AdminDashboard), so a
// fresh instance — and fresh form/mutation state — is what you get each time.
export default function CreateUserModal({ onClose, onCreated }: CreateUserModalProps) {
  const createUser = useCreateUser();

  const [givenName, setGivenName] = useState("");
  const [familyName, setFamilyName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<CoreGridRole>("Staff");
  const [touched, setTouched] = useState<Touched>({});

  const markTouched = (field: keyof Touched) => setTouched((t) => ({ ...t, [field]: true }));

  // Mirrors CreateUserRequest's [Required]/[MaxLength]/[MinLength] (backend/Features/Users/UsersModels.cs).
  const givenNameValid = givenName.trim().length > 0 && givenName.trim().length <= 100;
  const familyNameValid = familyName.trim().length > 0 && familyName.trim().length <= 100;
  const emailValid = isValidEmail(email) && email.trim().length <= 256;
  const passwordValid = password.length >= 8 && password.length <= 200;

  const givenNameInvalid = touched.givenName && !givenNameValid;
  const familyNameInvalid = touched.familyName && !familyNameValid;
  const emailInvalid = touched.email && !emailValid;
  const passwordInvalid = touched.password && !passwordValid;

  const canSubmit = givenNameValid && familyNameValid && emailValid && passwordValid;

  const handleSubmit = () => {
    setTouched({ givenName: true, familyName: true, email: true, password: true });
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
            maxLength={100}
            value={givenName}
            onChange={(e) => setGivenName(e.target.value)}
            onBlur={() => markTouched("givenName")}
            invalid={!!givenNameInvalid}
            invalidText="First name is required."
          />
          <TextInput
            id="create-user-family-name"
            labelText="Last Name"
            maxLength={100}
            value={familyName}
            onChange={(e) => setFamilyName(e.target.value)}
            onBlur={() => markTouched("familyName")}
            invalid={!!familyNameInvalid}
            invalidText="Last name is required."
          />
        </div>
        <TextInput
          id="create-user-email"
          labelText="Email"
          type="email"
          maxLength={256}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          onBlur={() => markTouched("email")}
          invalid={!!emailInvalid}
          invalidText="Enter a valid email address."
        />
        <PasswordInput
          id="create-user-password"
          labelText="Temporary Password"
          helperText="At least 8 characters. Share this with the user directly — CoreGrid doesn't email it."
          maxLength={200}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          onBlur={() => markTouched("password")}
          invalid={!!passwordInvalid}
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
