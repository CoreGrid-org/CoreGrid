import type { CoreGridRole } from "@/features/auth/lib/roles";

export const ASSIGNABLE_ROLES: CoreGridRole[] = ["Administrator", "InventoryOfficer", "Auditor", "Staff"];

type TagColor = "purple" | "blue" | "teal" | "gray";

// What each role is for, shown when choosing one and in the roles guide.
export const ROLE_INFO: Record<CoreGridRole, { summary: string; tag: TagColor }> = {
  Administrator: {
    summary: "Full access: users, settings, approvals for transfers and disposals, reports and audit.",
    tag: "purple",
  },
  InventoryOfficer: {
    summary: "Runs the register: assets, maintenance, transfers, condemning and disposal requests.",
    tag: "blue",
  },
  Auditor: {
    summary: "Read-only oversight: verification campaigns, discrepancies, audit log and reports.",
    tag: "teal",
  },
  Staff: {
    summary: "Mobile app only: scan assets, report faults and complete verification tasks.",
    tag: "gray",
  },
};

const PASSWORD_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%*?";

// A random temporary password (no look-alike characters such as O/0, l/1).
export function generatePassword(length = 14): string {
  const values = crypto.getRandomValues(new Uint32Array(length));
  return Array.from(values, (v) => PASSWORD_CHARS[v % PASSWORD_CHARS.length]).join("");
}

export const initials = (givenName: string, familyName: string) =>
  `${givenName.trim()[0] ?? ""}${familyName.trim()[0] ?? ""}`.toUpperCase() || "?";
