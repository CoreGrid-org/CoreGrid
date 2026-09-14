// Mirrors backend/Domain/CoreGridRole.cs. Role comes from GET /api/me
// (CoreGrid's own database), not from a ThunderID token claim — see
// backend/Features/Me and doc/setup/ThunderID.md.
export type CoreGridRole = "Administrator" | "InventoryOfficer" | "Auditor" | "Staff";

// 2026-09-14 team decision: Staff gets no web portal at all — Flutter only
// (FR-083's task-focused Flutter dashboard). This diverges from FR-081's
// literal "All users" wording for the React dashboard; the team chose to
// scope Staff out of the web client entirely rather than build a React
// dashboard for a role whose real workflow (scan, report a fault) is
// mobile-first anyway. A Staff account signing into the web app lands on
// AccessRestricted rather than a dead route.
const ROLE_LANDING_ROUTE: Record<CoreGridRole, string> = {
  Administrator: "/admin",
  InventoryOfficer: "/inventory",
  Auditor: "/audit",
  Staff: "/access-restricted",
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
