import { describe, expect, it } from "vitest";
import { isWithinDayRange, toDateOnly, validateDateRange } from "./dates";

const today = "2026-09-26";

describe("validateDateRange", () => {
  it("accepts an empty or open-ended range", () => {
    expect(validateDateRange({}, { today })).toEqual({});
    expect(validateDateRange({ from: "2026-09-01" }, { today })).toEqual({});
    expect(validateDateRange({ to: "2026-09-01" }, { today })).toEqual({});
  });

  it("accepts a same-day range and a range ending today", () => {
    expect(validateDateRange({ from: "2026-09-10", to: "2026-09-10" }, { today })).toEqual({});
    expect(validateDateRange({ from: "2026-09-10", to: today }, { today })).toEqual({});
  });

  it("rejects an inverted range on the end date", () => {
    expect(validateDateRange({ from: "2026-09-10", to: "2026-09-01" }, { today })).toEqual({
      to: "Must be on or after the start date.",
    });
  });

  it("rejects future dates unless allowed", () => {
    expect(validateDateRange({ from: "2026-10-01" }, { today }).from).toBe("Can't be in the future.");
    expect(validateDateRange({ to: "2026-10-01" }, { today }).to).toBe("Can't be in the future.");
    expect(validateDateRange({ from: "2026-10-01", to: "2026-10-05" }, { today, allowFuture: true })).toEqual({});
  });

  it("rejects malformed and impossible dates", () => {
    expect(validateDateRange({ from: "2026-9-1" }, { today }).from).toBe("Enter a valid date (yyyy-mm-dd).");
    expect(validateDateRange({ to: "2026-02-30" }, { today }).to).toBe("Enter a valid date (yyyy-mm-dd).");
  });
});

describe("isWithinDayRange", () => {
  it("includes the whole of the end day", () => {
    const lateOnEndDay = new Date(2026, 8, 10, 23, 30).toISOString();
    expect(isWithinDayRange(lateOnEndDay, "2026-09-01", "2026-09-10")).toBe(true);
    const nextDay = new Date(2026, 8, 11, 0, 5).toISOString();
    expect(isWithinDayRange(nextDay, "2026-09-01", "2026-09-10")).toBe(false);
  });
});

describe("toDateOnly", () => {
  it("uses the local calendar day, not UTC", () => {
    expect(toDateOnly(new Date(2026, 8, 5, 0, 15))).toBe("2026-09-05");
  });
});
