import { SignInButton, useThunderID } from "@thunderid/react";
import { Button } from "@carbon/react";

export default function SignIn() {
  const { isLoading } = useThunderID();

  if (isLoading) return <div style={{ minHeight: "100vh" }} />;

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
