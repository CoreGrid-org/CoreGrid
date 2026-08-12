// Mirrors backend/Domain/CoreGridRole.cs — these strings are also the
// literal `roles` claim values configured in ThunderID (doc/setup/ThunderID.md).
export type CoreGridRole = "Administrator" | "InventoryOfficer" | "Auditor" | "Staff";

const ROLE_LANDING_ROUTE: Record<CoreGridRole, string> = {
  Administrator: "/admin",
  InventoryOfficer: "/inventory",
  Auditor: "/audit",
  Staff: "/staff",
};

const ROLE_LABEL: Record<CoreGridRole, string> = {
  Administrator: "Administrator",
  InventoryOfficer: "Inventory Officer",
  Auditor: "Auditor",
  Staff: "Staff",
};

function isCoreGridRole(value: string): value is CoreGridRole {
  return value in ROLE_LANDING_ROUTE;
}

// ThunderID's `roles` claim is a JSON array, but @thunderid/react's `user`
// object is loosely typed — read it defensively rather than assuming shape.
export function getUserRoles(user: unknown): CoreGridRole[] {
  const raw = (user as { roles?: unknown } | null | undefined)?.roles;
  const values = Array.isArray(raw) ? raw : typeof raw === "string" ? [raw] : [];
  return values.filter((value): value is CoreGridRole => typeof value === "string" && isCoreGridRole(value));
}

// A user only ever holds one CoreGrid role today (backend/Domain/User.cs),
// but the claim is an array — this picks the first one CoreGrid recognises.
export function getPrimaryRole(user: unknown): CoreGridRole | undefined {
  return getUserRoles(user)[0];
}

export function getRoleLandingRoute(role: CoreGridRole | undefined): string {
  return role ? ROLE_LANDING_ROUTE[role] : "/access-restricted";
}

export function getRoleLabel(role: CoreGridRole): string {
  return ROLE_LABEL[role];
}
