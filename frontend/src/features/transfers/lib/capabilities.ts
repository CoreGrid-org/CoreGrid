import type { CoreGridRole } from "@/features/auth/lib/roles";
import { hasPermission } from "@/features/auth/lib/permissions";

// What each role can do on the Transfers & Disposals page, read from the
// shared permission map (features/auth/lib/permissions.ts): requesting and
// confirming receipt include Administrator alongside InventoryOfficer;
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
    canCreate: hasPermission(role, "transfer:request"),
    canApprove: hasPermission(role, "transfer:approve"),
    canConfirmReceipt: hasPermission(role, "transfer:confirm-receipt"),
    isAuditView: hasPermission(role, "audit:review"),
  };
}

// Appendix B: an Inventory Officer may only confirm receipt of a transfer
// into their own department; Administrator is exempt (plan §4.4).
export function canConfirmReceiptOf(
  role: CoreGridRole,
  callerDepartmentId: string | null | undefined,
  toDepartmentId: string,
): boolean {
  if (!hasPermission(role, "transfer:confirm-receipt")) return false;
  return role !== "InventoryOfficer" || callerDepartmentId === toDepartmentId;
}
