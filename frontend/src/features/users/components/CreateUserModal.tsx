import { useState } from "react";
import { Modal, TextInput, PasswordInput, InlineNotification, Button, CopyButton } from "@carbon/react";
import { Renew } from "@carbon/icons-react";
import { useCreateUser } from "../hooks/useUsers";
import type { CoreGridRole } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { isValidEmail } from "@/shared/lib/validation";
import type { CreateUserResponse } from "../services/users";
import { generatePassword } from "../lib/roles";
import RolePicker from "./RolePicker";

interface CreateUserModalProps {
  onClose: () => void;
  onCreated: (user: CreateUserResponse) => void;
}

type Touched = Partial<Record<"givenName" | "familyName" | "email" | "password", boolean>>;

// Mounted only while the "Add user" modal is open, so a fresh instance (and
// fresh form/mutation state) is what you get each time.
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
      size="md"
      modalLabel="Users & Roles"
      modalHeading="Add user"
      primaryButtonText={createUser.isPending ? "Creating…" : "Create user"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={createUser.isPending}
      preventCloseOnClickOutside
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <div className="cg-modal-form">
        <p className="cg-modal-form__intro">
          The user signs in with this email and the temporary password below. CoreGrid doesn't email it, so share it
          with them directly.
        </p>

        {createUser.isError && (
          <InlineNotification
            kind="error"
            title="Could not create user"
            subtitle={getErrorMessage(createUser.error, "Something went wrong. Please try again.")}
            hideCloseButton
            lowContrast
            style={{ maxWidth: "100%" }}
          />
        )}

        <div className="cg-modal-form__row">
          <TextInput
            id="create-user-given-name"
            labelText="First name"
            maxLength={100}
            value={givenName}
            onChange={(e) => setGivenName(e.target.value)}
            onBlur={() => markTouched("givenName")}
            invalid={Boolean(touched.givenName && !givenNameValid)}
            invalidText="First name is required."
          />
          <TextInput
            id="create-user-family-name"
            labelText="Last name"
            maxLength={100}
            value={familyName}
            onChange={(e) => setFamilyName(e.target.value)}
            onBlur={() => markTouched("familyName")}
            invalid={Boolean(touched.familyName && !familyNameValid)}
            invalidText="Last name is required."
          />
        </div>

        <TextInput
          id="create-user-email"
          labelText="Email"
          type="email"
          placeholder="name@organisation.gov.lk"
          maxLength={256}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          onBlur={() => markTouched("email")}
          invalid={Boolean(touched.email && !emailValid)}
          invalidText="Enter a valid email address."
        />

        <div className="cg-user-password">
          <PasswordInput
            id="create-user-password"
            labelText="Temporary password"
            helperText="At least 8 characters."
            maxLength={200}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            onBlur={() => markTouched("password")}
            invalid={Boolean(touched.password && !passwordValid)}
            invalidText="Must be at least 8 characters."
          />
          <div className="cg-user-password__actions">
            <Button
              kind="tertiary"
              size="md"
              renderIcon={Renew}
              onClick={() => {
                setPassword(generatePassword());
                markTouched("password");
              }}
            >
              Generate
            </Button>
            {password && (
              <CopyButton
                iconDescription="Copy password"
                feedback="Copied"
                onClick={() => void navigator.clipboard?.writeText(password)}
              />
            )}
          </div>
        </div>

        <RolePicker name="create-user-role" value={role} onChange={setRole} />
      </div>
    </Modal>
  );
}
