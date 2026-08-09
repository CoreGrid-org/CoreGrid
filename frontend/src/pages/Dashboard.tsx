import { Navigate } from "react-router-dom";
import { SignOutButton, useThunderID } from "@thunderid/react";
import { Button, Tile } from "@carbon/react";

export default function Dashboard() {
  const { isSignedIn, isLoading, user } = useThunderID();

  if (isLoading) return <div style={{ minHeight: "100vh" }} />;
  if (!isSignedIn) return <Navigate to="/signin" replace />;

  return (
    <div className="cg-topnav-content">
      <div className="cg-page">
        <div className="cg-page__header">
          <div className="cg-page__header-left">
            <h1 className="cg-page__title">Dashboard</h1>
            <p className="cg-page__subtitle">
              {user?.given_name ? `Welcome, ${user.given_name}.` : "Welcome."}
            </p>
          </div>
          <SignOutButton>
            {({ signOut, isLoading: signOutLoading }) => (
              <Button kind="tertiary" onClick={() => signOut()} disabled={signOutLoading}>
                {signOutLoading ? "Signing out…" : "Sign out"}
              </Button>
            )}
          </SignOutButton>
        </div>

        <Tile>
          <p>
            This is a placeholder. CoreGrid's asset registry, maintenance, transfer, disposal and
            agentic-workflow screens (SRS §6) aren't built yet — this page only exists to give the
            sign-in and setup flow somewhere real to land.
          </p>
        </Tile>
      </div>
    </div>
  );
}
