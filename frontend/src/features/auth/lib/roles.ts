// Mirrors backend/Domain/CoreGridRole.cs. Role comes from GET /api/me
// (CoreGrid's own database), not from a ThunderID token claim — see
// backend/Features/Me and doc/setup/ThunderID.md.
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

export function getRoleLandingRoute(role: CoreGridRole | undefined): string {
  return role ? ROLE_LANDING_ROUTE[role] : "/access-restricted";
}

export function getRoleLabel(role: CoreGridRole): string {
  return ROLE_LABEL[role];
}
