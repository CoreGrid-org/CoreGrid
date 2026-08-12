import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useThunderID } from "@thunderid/react";
import { useMe } from "../hooks/useMe";
import { getRoleLandingRoute, type CoreGridRole } from "../lib/roles";

interface RoleRouteProps {
  role: CoreGridRole;
  children: ReactNode;
}

// Guards a per-role dashboard route: unauthenticated users go to sign-in,
// users holding a different recognised role are sent to their own dashboard
// (not an error — just the wrong door), and users with no recognised role
// land on AccessRestricted. Role comes from CoreGrid's own database
// (useMe), not from ThunderID's token/profile — see backend/Features/Me.
export default function RoleRoute({ role, children }: RoleRouteProps) {
  const { isSignedIn, isLoading: isAuthLoading } = useThunderID();
  const { data: me, isLoading: isMeLoading } = useMe();

  if (isAuthLoading) return <div style={{ minHeight: "100vh" }} />;
  if (!isSignedIn) return <Navigate to="/signin" replace />;
  if (isMeLoading) return <div style={{ minHeight: "100vh" }} />;

  if (me?.role !== role) return <Navigate to={getRoleLandingRoute(me?.role)} replace />;

  return <>{children}</>;
}
