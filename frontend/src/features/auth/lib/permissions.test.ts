import { describe, expect, it } from "vitest";
import { hasPermission } from "./permissions";
import { canConfirmReceiptOf, transferCapabilities } from "@/features/transfers/lib/capabilities";

describe("hasPermission", () => {
  it("keeps Auditor read-only on assets, maintenance, transfers and workflows", () => {
    for (const permission of [
      "asset:manage",
      "maintenance:report-fault",
      "maintenance:manage",
      "maintenance:complete",
      "transfer:request",
      "transfer:approve",
      "transfer:confirm-receipt",
      "workflow:initiate",
      "workflow:approve",
    ] as const) {
      expect(hasPermission("Auditor", permission)).toBe(false);
    }
    expect(hasPermission("Auditor", "report:audit")).toBe(true);
    expect(hasPermission("Auditor", "audit:review")).toBe(true);
  });

  it("gives Inventory Officer operations but no approvals or audit report", () => {
    expect(hasPermission("InventoryOfficer", "asset:manage")).toBe(true);
    expect(hasPermission("InventoryOfficer", "maintenance:create-direct")).toBe(true);
    expect(hasPermission("InventoryOfficer", "maintenance:complete")).toBe(true);
    expect(hasPermission("InventoryOfficer", "transfer:request")).toBe(true);
    expect(hasPermission("InventoryOfficer", "workflow:initiate")).toBe(true);
    expect(hasPermission("InventoryOfficer", "transfer:approve")).toBe(false);
    expect(hasPermission("InventoryOfficer", "workflow:approve")).toBe(false);
    expect(hasPermission("InventoryOfficer", "report:audit")).toBe(false);
  });

  it("denies everything while the role is still unknown", () => {
    expect(hasPermission(undefined, "asset:manage")).toBe(false);
  });
});

describe("transfer capabilities", () => {
  it("matches each web role's Transfers & Disposals page", () => {
    expect(transferCapabilities("Administrator")).toEqual({ canCreate: true, canApprove: true, canConfirmReceipt: true, isAuditView: false });
    expect(transferCapabilities("InventoryOfficer")).toEqual({ canCreate: true, canApprove: false, canConfirmReceipt: true, isAuditView: false });
    expect(transferCapabilities("Auditor")).toEqual({ canCreate: false, canApprove: false, canConfirmReceipt: false, isAuditView: true });
  });

  it("lets an Officer confirm receipt only into their own department", () => {
    expect(canConfirmReceiptOf("InventoryOfficer", "dept-a", "dept-a")).toBe(true);
    expect(canConfirmReceiptOf("InventoryOfficer", "dept-a", "dept-b")).toBe(false);
    expect(canConfirmReceiptOf("InventoryOfficer", null, "dept-b")).toBe(false);
    expect(canConfirmReceiptOf("Administrator", null, "dept-b")).toBe(true);
    expect(canConfirmReceiptOf("Auditor", "dept-b", "dept-b")).toBe(false);
  });
});
