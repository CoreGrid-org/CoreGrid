import { useEffect } from "react";
import { Link } from "react-router-dom";
import { Button, InlineNotification } from "@carbon/react";
import { ArrowRight, Email, Password, Locked } from "@carbon/icons-react";
import { thunderIdRecoveryUrl } from "../lib/recovery";

const STEPS = [
  { icon: Email, title: "Enter your email", text: "Use the email address you sign in to CoreGrid with." },
  { icon: Locked, title: "Open the reset link", text: "We'll email you a secure, single-use link." },
  { icon: Password, title: "Choose a new password", text: "Then sign in again with your new password." },
];

// Password recovery is ThunderID's own hosted flow (SRS §4: CoreGrid never
// handles credentials) — this page explains it and hands off to it. It works
// the same for every role, Administrators included. Enabled per ThunderID
// instance by scripts/thunderid/enable-password-recovery.sh.
export default function ForgotPassword() {
  const recoveryUrl = thunderIdRecoveryUrl();

  useEffect(() => {
    document.title = "Forgot password · CoreGrid";
    return () => {
      document.title = "CoreGrid";
    };
  }, []);

  return (
    <div className="cg-signin-wrapper">
      <div className="cg-setup-card cg-recovery">
        <img src="/CoreGrid.png" alt="CoreGrid" width={40} height={40} className="cg-signin-card__logo" />
        <h1 className="cg-setup-card__title">Forgot your password?</h1>
        <p className="cg-setup-card__subtitle">
          Reset it securely through CoreGrid's sign-in service. It takes about a minute.
        </p>

        <ol className="cg-recovery__steps">
          {STEPS.map(({ icon: Icon, title, text }, i) => (
            <li key={title} className="cg-recovery__step">
              <span className="cg-recovery__step-icon" aria-hidden="true">
                <Icon size={20} />
              </span>
              <span>
                <span className="cg-recovery__step-title">
                  {i + 1}. {title}
                </span>
                <span className="cg-recovery__step-text">{text}</span>
              </span>
            </li>
          ))}
        </ol>

        {recoveryUrl ? (
          <Button as="a" href={recoveryUrl} renderIcon={ArrowRight} className="cg-full-width-btn">
            Reset my password
          </Button>
        ) : (
          <InlineNotification
            kind="info"
            title="Use the sign-in page"
            subtitle='Select "Sign In", then "Forgot password?" under the password field.'
            lowContrast
            hideCloseButton
            style={{ maxWidth: "100%" }}
          />
        )}

        <p className="cg-recovery__help">
          Didn't get an email, or no longer have access to it? Ask a CoreGrid Administrator to reset your password from{" "}
          <strong>Users &amp; Roles</strong>.
        </p>

        <p className="cg-recovery__back">
          <Link to="/signin">Back to sign in</Link>
        </p>
      </div>
    </div>
  );
}
