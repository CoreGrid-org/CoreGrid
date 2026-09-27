import { useCallback } from "react";
import { useMe } from "./useMe";
import { hasPermission, type Permission } from "../lib/permissions";

// The signed-in user's permissions (lib/permissions.ts). `can` is false
// while /api/me is still loading, so actions never flash in and then vanish.
export function usePermissions() {
  const { data: me, isLoading } = useMe();
  const can = useCallback((permission: Permission) => hasPermission(me?.role, permission), [me?.role]);
  return { me, can, isLoading };
}
