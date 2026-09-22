import { useLocation } from "react-router-dom";

// Returns the current role route prefix.
export function useRolePrefix(): string {
  const { pathname } = useLocation();
  return `/${pathname.split("/")[1]}`;
}
