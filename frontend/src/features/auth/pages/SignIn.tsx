import { useEffect } from "react";
import { Navigate } from "react-router-dom";
import { SignInButton, useThunderID } from "@thunderid/react";
import { Button } from "@carbon/react";
import { WarningAltFilled } from "@carbon/icons-react";
import { useSetupStatus } from "@/features/setup/hooks/useSetup";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import StatusView from "@/shared/components/StatusView";

export default function SignIn() {
  const { isSignedIn, isLoading } = useThunderID();
  const { data: setupStatus, isLoading: setupLoading, isError: setupError, error, refetch } = useSetupStatus();

  useEffect(() => {
    document.title = "Sign In · CoreGrid";
    return () => {
      document.title = "CoreGrid";
    };
  }, []);

  if (isLoading || setupLoading) return <div style={{ minHeight: "100vh" }} />;

  if (setupError) {
    return (
      <StatusView
        variant="fullscreen"
        icon={WarningAltFilled}
        title="Can't reach CoreGrid"
        subtitle={getErrorMessage(error, "The backend isn't responding. Make sure it's running, then try again.")}
        actions={<Button onClick={refetch}>Try again</Button>}
      />
    );
  }

  if (isSignedIn) return <Navigate to="/" replace />;
  if (setupStatus?.needs_setup) return <Navigate to="/setup" replace />;

  return (
    <div className="cg-signin-wrapper">
      <div className="cg-signin-card">
        <img src="/CoreGrid.png" alt="CoreGrid" width={48} height={48} className="cg-signin-card__logo" />
        <h1 className="cg-signin-card__title">CoreGrid</h1>
        <p className="cg-signin-card__subtitle">Sign in to continue to your dashboard.</p>
        <SignInButton>
          {({ signIn, isLoading: signInLoading }) => (
            <Button onClick={() => signIn()} disabled={signInLoading} className="cg-full-width-btn">
              {signInLoading ? "Signing in…" : "Sign In"}
            </Button>
          )}
        </SignInButton>
      </div>
    </div>
  );
}
