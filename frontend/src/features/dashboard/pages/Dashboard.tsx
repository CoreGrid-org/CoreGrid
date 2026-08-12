import { Navigate } from "react-router-dom";
import { useThunderID } from "@thunderid/react";
import { getPrimaryRole, getRoleLandingRoute } from "@/features/auth/lib/roles";

// The generic post-sign-in landing spot. It never renders anything itself —
// it just works out which dashboard the signed-in user's role means they
// should be looking at, and sends them there.
export default function Dashboard() {
  const { isSignedIn, isLoading, user } = useThunderID();

  if (isLoading) return <div style={{ minHeight: "100vh" }} />;
  if (!isSignedIn) return <Navigate to="/signin" replace />;

  return <Navigate to={getRoleLandingRoute(getPrimaryRole(user))} replace />;
}
