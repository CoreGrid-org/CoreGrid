import { SignOutButton, useThunderID } from "@thunderid/react";
import { Button, Tag, Tile } from "@carbon/react";
import { getRoleLabel } from "@/features/auth/lib/roles";

export default function AdminDashboard() {
  const { user } = useThunderID();

  return (
    <div className="cg-topnav-content">
      <div className="cg-page">
        <div className="cg-page__header">
          <div className="cg-page__header-left">
            <div className="cg-page__title-row">
              <h1 className="cg-page__title">Admin Dashboard</h1>
              <Tag type="blue">{getRoleLabel("Administrator")}</Tag>
            </div>
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
