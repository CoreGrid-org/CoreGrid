import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useThunderID } from "@thunderid/react";
import { getPrimaryRole, getRoleLandingRoute, type CoreGridRole } from "../lib/roles";

interface RoleRouteProps {
  role: CoreGridRole;
  children: ReactNode;
}

// Guards a per-role dashboard route: unauthenticated users go to sign-in,
// users holding a different recognised role are sent to their own dashboard
// (not an error — just the wrong door), and users with no recognised role
// land on AccessRestricted.
export default function RoleRoute({ role, children }: RoleRouteProps) {
  const { isSignedIn, isLoading, user } = useThunderID();

  if (isLoading) return <div style={{ minHeight: "100vh" }} />;
  if (!isSignedIn) return <Navigate to="/signin" replace />;

  const primaryRole = getPrimaryRole(user);
  if (primaryRole !== role) return <Navigate to={getRoleLandingRoute(primaryRole)} replace />;

  return <>{children}</>;
}
