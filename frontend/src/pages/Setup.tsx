import { useState } from "react";
import { Navigate } from "react-router-dom";
import { SignInButton } from "@thunderid/react";
import {
  Button,
  TextInput,
  PasswordInput,
  InlineNotification,
  ProgressIndicator,
  ProgressStep,
  Stack,
} from "@carbon/react";
import { CheckmarkFilled, WarningAltFilled } from "@carbon/icons-react";
import { useSetupStatus, useCompleteSetup } from "../queries/useSetup";
import { getErrorMessage } from "../lib/errorMessage";
import { silentProgressStatus } from "../lib/carbonA11y";
import StatusView from "../components/common/StatusView";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

type Touched = Partial<Record<"givenName" | "familyName" | "email" | "password" | "confirmPassword" | "orgName", boolean>>;

function Header() {
  return (
    <div className="cg-setup-header">
      <img src="/CoreGrid.png" alt="" width={44} height={44} className="cg-setup-header__logo" />
      <div>
        <p className="cg-setup-header__title">CoreGrid</p>
        <p className="cg-setup-header__subtitle">Asset lifecycle management, self-hosted</p>
      </div>
    </div>
  );
}

export default function Setup() {
  const { data: status, isLoading: statusLoading, isError: statusError, error: statusErrorDetail, refetch } = useSetupStatus();
  const completeSetup = useCompleteSetup();

  const [step, setStep] = useState(0);
  const [givenName, setGivenName] = useState("");
  const [familyName, setFamilyName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [orgName, setOrgName] = useState("");
  const [touched, setTouched] = useState<Touched>({});
  const [done, setDone] = useState(false);

  const markTouched = (field: keyof Touched) => setTouched((t) => ({ ...t, [field]: true }));

  if (statusLoading) return <div style={{ minHeight: "100vh" }} />;

  if (statusError && !done) {
    return (
      <StatusView
        variant="fullscreen"
        icon={WarningAltFilled}
        title="Can't reach CoreGrid"
        subtitle={getErrorMessage(statusErrorDetail, "The backend isn't responding. Make sure it's running, then try again.")}
        actions={<Button onClick={refetch}>Try again</Button>}
      />
    );
  }

  if (!done && status && !status.needs_setup) {
    return <Navigate to="/signin" replace />;
  }

  const givenNameInvalid = touched.givenName && !givenName.trim();
  const familyNameInvalid = touched.familyName && !familyName.trim();
  const emailInvalid = touched.email && !EMAIL_RE.test(email.trim());
  const passwordInvalid = touched.password && password.length < 8;
  const confirmPasswordInvalid = touched.confirmPassword && confirmPassword !== password;
  const orgNameInvalid = touched.orgName && !orgName.trim();

  const canContinueStep1 =
    givenName.trim().length > 0 &&
    familyName.trim().length > 0 &&
    EMAIL_RE.test(email.trim()) &&
    password.length >= 8 &&
    confirmPassword === password;

  const canSubmit = canContinueStep1 && orgName.trim().length > 0;

  const handleContinue = () => {
    setTouched((t) => ({ ...t, givenName: true, familyName: true, email: true, password: true, confirmPassword: true }));
    if (!canContinueStep1) return;
    setStep(1);
  };

  const handleSubmit = () => {
    setTouched((t) => ({ ...t, orgName: true }));
    if (!canSubmit) return;

    completeSetup.mutate(
      {
        admin: {
          given_name: givenName.trim(),
          family_name: familyName.trim(),
          email: email.trim(),
          password,
        },
        organisation: { name: orgName.trim() },
      },
      { onSuccess: () => setDone(true) }
    );
  };

  const errorMessage = completeSetup.error
    ? getErrorMessage(completeSetup.error, "Something went wrong. Please try again.")
    : null;

  if (done) {
    return (
      <div className="cg-signin-wrapper">
        <div className="cg-setup-card" style={{ textAlign: "center" }}>
          <div className="cg-setup-success-icon">
            <CheckmarkFilled size={28} />
          </div>
          <h1 className="cg-setup-card__title">Organisation created</h1>
          <p className="cg-setup-card__subtitle">
            {orgName.trim()} is ready. Sign in with the admin account you just created to open the dashboard.
          </p>
          <SignInButton>
            {({ signIn, isLoading: signInLoading }) => (
              <Button onClick={() => signIn()} disabled={signInLoading} className="cg-full-width-btn">
                {signInLoading ? "Signing in…" : "Continue to Sign In"}
              </Button>
            )}
          </SignInButton>
        </div>
      </div>
    );
  }

  return (
    <div className="cg-signin-wrapper">
      <div className="cg-setup-card">
        <Header />

        <ProgressIndicator currentIndex={step} spaceEqually style={{ marginBottom: "1.75rem" }}>
          <ProgressStep label="Admin account" description="Create the first admin" translateWithId={silentProgressStatus} />
          <ProgressStep label="Organisation" description="Register your organisation" translateWithId={silentProgressStatus} />
        </ProgressIndicator>

        <p className="cg-setup-card__intro">
          This looks like a new instance. Create the admin account and your organisation to get started —{" "}
          <strong>this can only be done once.</strong>
        </p>

        {errorMessage && (
          <InlineNotification
            kind="error"
            title={step === 0 ? "Could not continue" : "Could not create organisation"}
            subtitle={errorMessage}
            hideCloseButton
            lowContrast
            style={{ marginBottom: "1rem", maxWidth: "100%" }}
          />
        )}

        {step === 0 ? (
          <Stack gap={5}>
            <div className="cg-setup-name-grid">
              <TextInput
                id="setup-given-name"
                labelText="First Name"
                value={givenName}
                onChange={(e) => setGivenName(e.target.value)}
                onBlur={() => markTouched("givenName")}
                invalid={!!givenNameInvalid}
                invalidText="First name is required."
              />
              <TextInput
                id="setup-family-name"
                labelText="Last Name"
                value={familyName}
                onChange={(e) => setFamilyName(e.target.value)}
                onBlur={() => markTouched("familyName")}
                invalid={!!familyNameInvalid}
                invalidText="Last name is required."
              />
            </div>

            <TextInput
              id="setup-email"
              labelText="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => markTouched("email")}
              invalid={!!emailInvalid}
              invalidText="Enter a valid email address."
            />

            <PasswordInput
              id="setup-password"
              labelText="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onBlur={() => markTouched("password")}
              invalid={!!passwordInvalid}
              invalidText="Password must be at least 8 characters."
              helperText="At least 8 characters."
            />
            <PasswordInput
              id="setup-confirm-password"
              labelText="Confirm Password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              onBlur={() => markTouched("confirmPassword")}
              invalid={!!confirmPasswordInvalid}
              invalidText="Passwords do not match."
            />

            <Button onClick={handleContinue} className="cg-full-width-btn">
              Continue
            </Button>
          </Stack>
        ) : (
          <Stack gap={5}>
            <TextInput
              id="setup-org-name"
              labelText="Organisation Name"
              helperText="The tenant institution this deployment serves, e.g. “Ministry of Transport”."
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              onBlur={() => markTouched("orgName")}
              invalid={!!orgNameInvalid}
              invalidText="Organisation name is required."
            />

            <Button onClick={handleSubmit} disabled={completeSetup.isPending} className="cg-full-width-btn">
              {completeSetup.isPending ? "Creating organisation…" : "Create Organisation"}
            </Button>

            <Button kind="ghost" onClick={() => setStep(0)} className="cg-full-width-btn">
              Back
            </Button>
          </Stack>
        )}
      </div>
    </div>
  );
}
