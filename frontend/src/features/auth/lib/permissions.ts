import type { CoreGridRole } from "./roles";

// The one place the web client decides what a role may do. Mirrors the
// backend's grants (backend/Features/Shared/Auth/RoleGroups.cs plus the few
// role-string [Authorize] attributes) so a button only shows when the API
// would accept the request. Change a grant here only alongside the backend.
// Staff never reach these checks — they have no web portal (roles.ts).
export type Permission =
  | "asset:manage" // create, amend, record condition (CanManageAssets)
  | "maintenance:report-fault" // CanRequestMaintenance
  | "maintenance:create-direct" // POST /api/maintenance — Officer only (plan §5.4)
  | "maintenance:manage" // approve, start, cancel, amend (CanManageMaintenance)
  | "maintenance:complete" // POST /api/maintenance/{id}/complete — Officer only
  | "transfer:request" // initiate transfer, condemn, submit disposal (CanRequestTransfer/CanRequestDisposal)
  | "transfer:approve" // approve/reject transfers and disposals (CanApproveTransfer/CanApproveDisposal)
  | "transfer:confirm-receipt" // CanConfirmReceipt (Officer: destination department only)
  | "audit:review" // read-only audit trail and compliance detail on Transfers & Disposals
  | "report:audit" // Reports > Audit tab (AuditReportController)
  | "workflow:initiate" // CanInitiateWorkflow
  | "workflow:approve"; // CanApproveWorkflow

const GRANTS: Record<Permission, readonly CoreGridRole[]> = {
  "asset:manage": ["InventoryOfficer", "Administrator"],
  "maintenance:report-fault": ["Staff", "InventoryOfficer", "Administrator"],
  "maintenance:create-direct": ["InventoryOfficer"],
  "maintenance:manage": ["InventoryOfficer", "Administrator"],
  "maintenance:complete": ["InventoryOfficer"],
  "transfer:request": ["InventoryOfficer", "Administrator"],
  "transfer:approve": ["Administrator"],
  "transfer:confirm-receipt": ["InventoryOfficer", "Administrator"],
  "audit:review": ["Auditor"],
  "report:audit": ["Auditor", "Administrator"],
  "workflow:initiate": ["InventoryOfficer", "Administrator"],
  "workflow:approve": ["Administrator"],
};

export function hasPermission(role: CoreGridRole | undefined, permission: Permission): boolean {
  return role !== undefined && GRANTS[permission].includes(role);
}
