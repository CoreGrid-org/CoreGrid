import { describe, expect, it } from "vitest";
import { transferCapabilities } from "./capabilities";

describe("transferCapabilities", () => {
  it("grants full management and approval capabilities to Administrator", () => {
    const caps = transferCapabilities("Administrator");
    expect(caps).toEqual({
      canCreate: true,
      canApprove: true,
      canConfirmReceipt: true,
      isAuditView: false,
    });
  });

  it("grants creation and receipt confirmation to InventoryOfficer but blocks approval and audit view", () => {
    const caps = transferCapabilities("InventoryOfficer");
    expect(caps).toEqual({
      canCreate: true,
      canApprove: false,
      canConfirmReceipt: true,
      isAuditView: false,
    });
  });

  it("grants audit view to Auditor and blocks all mutating capabilities", () => {
    const caps = transferCapabilities("Auditor");
    expect(caps).toEqual({
      canCreate: false,
      canApprove: false,
      canConfirmReceipt: false,
      isAuditView: true,
    });
  });
});
