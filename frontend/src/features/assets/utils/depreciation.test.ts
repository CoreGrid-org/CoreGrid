import { describe, expect, it } from "vitest";
import { localTodayIso, previewDepreciation } from "./depreciation";

describe("previewDepreciation", () => {
  const asOf = new Date("2026-01-01T00:00:00Z");

  it("writes off cost ÷ useful life per year", () => {
    // Exactly 2 × 365.25 days after purchase → 2 years into a 5-year life.
    const twoYearsLater = new Date(new Date("2024-01-01").getTime() + 2 * 365.25 * 86_400_000);
    const result = previewDepreciation(100_000, "2024-01-01", 5, twoYearsLater);

    expect(result.annualDepreciation).toBe(20_000);
    expect(result.accumulatedDepreciation).toBe(40_000);
    expect(result.residualValue).toBe(60_000);
    expect(result.depreciatedFraction).toBeCloseTo(0.4);
  });

  it("returns the full cost for a purchase made today", () => {
    expect(previewDepreciation(50_000, "2026-01-01", 4, asOf).residualValue).toBe(50_000);
  });

  it("never goes below zero past the end of the useful life", () => {
    const result = previewDepreciation(10_000, "2010-01-01", 3, asOf);
    expect(result.residualValue).toBe(0);
    expect(result.accumulatedDepreciation).toBe(10_000);
  });

  it("does not depreciate when the type has no useful life", () => {
    expect(previewDepreciation(10_000, "2020-01-01", 0, asOf).residualValue).toBe(10_000);
  });
});

describe("localTodayIso", () => {
  it("formats the local calendar date", () => {
    expect(localTodayIso(new Date(2026, 8, 5, 23, 30))).toBe("2026-09-05");
  });
});
