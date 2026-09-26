import type { CoreGridRole } from "@/features/auth/lib/roles";

// What each role can do on the Transfers & Disposals page. Mirrors the
// backend's RoleGroups: transfer:request / disposal:request and
// confirm-receipt include Administrator alongside InventoryOfficer;
// approving transfers and disposals is Administrator-only; Auditor is
// read-only but sees the full audit trail and compliance detail.
export interface TransferCapabilities {
  canCreate: boolean;
  canApprove: boolean;
  canConfirmReceipt: boolean;
  isAuditView: boolean;
}

export function transferCapabilities(role: CoreGridRole): TransferCapabilities {
  return {
    canCreate: role === "Administrator" || role === "InventoryOfficer",
    canApprove: role === "Administrator",
    canConfirmReceipt: role === "Administrator" || role === "InventoryOfficer",
    isAuditView: role === "Auditor",
  };
}
