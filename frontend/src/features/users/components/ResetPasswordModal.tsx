import { useState } from "react";
import { Modal, PasswordInput, InlineNotification, Button, CopyButton } from "@carbon/react";
import { Renew } from "@carbon/icons-react";
import { useResetUserPassword } from "../hooks/useUsers";
import type { CoreGridUser } from "../services/users";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { generatePassword } from "../lib/roles";
import UserIdentity from "./UserIdentity";

interface ResetPasswordModalProps {
  user: CoreGridUser;
  isSelf: boolean;
  onClose: () => void;
  onReset: () => void;
}

// An Administrator sets a new temporary password for any user, themselves
// included. POST /api/users/{id}/reset-password → ThunderID update-credentials.
// Mounted only while open, so each open starts blank.
export default function ResetPasswordModal({ user, isSelf, onClose, onReset }: ResetPasswordModalProps) {
  const resetPassword = useResetUserPassword();

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [submitted, setSubmitted] = useState(false);

  // Mirrors ResetPasswordRequest's [MinLength(8)]/[MaxLength(200)] (backend/Features/Users/UsersModels.cs).
  const passwordError =
    password.length < 8 ? "Must be at least 8 characters." : password.length > 200 ? "Must be 200 characters or fewer." : null;
  const confirmError = confirm !== password ? "Passwords do not match." : null;

  const handleSubmit = () => {
    setSubmitted(true);
    if (passwordError || confirmError || resetPassword.isPending) return;
    resetPassword.mutate({ id: user.id, newPassword: password }, { onSuccess: onReset });
  };

  return (
    <Modal
      open
      size="sm"
      danger
      modalLabel="Users & Roles"
      modalHeading="Reset password"
      primaryButtonText={resetPassword.isPending ? "Resetting…" : "Reset password"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={resetPassword.isPending}
      onRequestClose={() => !resetPassword.isPending && onClose()}
      onRequestSubmit={handleSubmit}
    >
      <div className="cg-modal-form">
        <UserIdentity user={user} isSelf={isSelf} />
        <p className="cg-modal-form__intro">
          Their current password stops working immediately. Share the new one with them securely — they can change it
          any time with <strong>Forgot password?</strong> on the sign-in page.
        </p>

        {resetPassword.isError && (
          <InlineNotification
            kind="error"
            title="Could not reset the password"
            subtitle={getErrorMessage(resetPassword.error, "Something went wrong. Please try again.")}
            lowContrast
            hideCloseButton
          />
        )}

        <div className="cg-user-password">
          <PasswordInput
            id="reset-password-new"
            labelText="New password"
            helperText="At least 8 characters."
            maxLength={200}
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            invalid={Boolean((submitted || password) && passwordError)}
            invalidText={passwordError ?? undefined}
          />
          <div className="cg-user-password__actions">
            <Button
              kind="tertiary"
              size="md"
              renderIcon={Renew}
              onClick={() => {
                const generated = generatePassword();
                setPassword(generated);
                setConfirm(generated);
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

        <PasswordInput
          id="reset-password-confirm"
          labelText="Confirm new password"
          maxLength={200}
          autoComplete="new-password"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          invalid={Boolean((submitted || confirm) && confirmError)}
          invalidText={confirmError ?? undefined}
        />
      </div>
    </Modal>
  );
}
