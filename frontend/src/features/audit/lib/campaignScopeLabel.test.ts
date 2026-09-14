import { describe, expect, it } from "vitest";
import { campaignScopeLabel } from "./campaignScopeLabel";

const NO_SCOPE = {
  scope_department_name: null,
  scope_location_name: null,
  scope_asset_category_name: null,
  scope_asset_type_name: null,
};

describe("campaignScopeLabel", () => {
  it("returns 'Whole register' when no scope is set", () => {
    expect(campaignScopeLabel(NO_SCOPE)).toBe("Whole register");
  });

  it("joins a single set scope field on its own", () => {
    expect(campaignScopeLabel({ ...NO_SCOPE, scope_department_name: "Radiology" })).toBe("Radiology");
  });

  it("joins multiple set scope fields with a middle dot, in declaration order", () => {
    expect(
      campaignScopeLabel({
        scope_department_name: "Radiology",
        scope_location_name: "Building A",
        scope_asset_category_name: null,
        scope_asset_type_name: "MRI Scanner",
      }),
    ).toBe("Radiology · Building A · MRI Scanner");
  });
});
