import { useLocation } from "react-router-dom";

// Every role's routes live under a role-prefixed path (/admin, /inventory,
// /audit - see App.tsx). React Router v6 resolves relative navigation
// (e.g. navigate("..")) against the *route config's* nesting, not the URL's
// path segments - and since maintenance/:id, maintenance/new etc. are flat
// sibling routes rather than nested under a shared "maintenance" route, ".."
// from one of those pages lands on the role's dashboard, not the list it
// came from. Building an explicit role-prefixed path sidesteps that.
export function useRolePrefix(): string {
  const { pathname } = useLocation();
  return `/${pathname.split("/")[1]}`;
}
