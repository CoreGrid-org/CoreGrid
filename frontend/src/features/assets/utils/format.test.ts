import { describe, expect, it } from "vitest";
import { formatCurrency, formatDate, formatAttributeValue } from "./format";

describe("formatCurrency", () => {
  it("formats positive amounts with 2 decimal places and LKR prefix", () => {
    expect(formatCurrency(1500)).toBe("LKR 1,500.00");
    expect(formatCurrency(250000.5)).toBe("LKR 250,000.50");
  });

  it("formats zero amount correctly", () => {
    expect(formatCurrency(0)).toBe("LKR 0.00");
  });
});

describe("formatDate", () => {
  it("formats valid ISO date string", () => {
    const formatted = formatDate("2026-09-15T00:00:00Z");
    expect(formatted).toBeTruthy();
    expect(typeof formatted).toBe("string");
  });
});

describe("formatAttributeValue", () => {
  it("formats NUMBER attribute data type", () => {
    expect(formatAttributeValue({
      data_type: "NUMBER",
      value_text: null,
      value_number: 1250,
      value_date: null,
      value_boolean: null,
    })).toBe("1,250");

    expect(formatAttributeValue({
      data_type: "NUMBER",
      value_text: null,
      value_number: null,
      value_date: null,
      value_boolean: null,
    })).toBe("—");
  });

  it("formats DATE attribute data type", () => {
    const result = formatAttributeValue({
      data_type: "DATE",
      value_text: null,
      value_number: null,
      value_date: "2026-01-10",
      value_boolean: null,
    });
    expect(result).not.toBe("—");

    expect(formatAttributeValue({
      data_type: "DATE",
      value_text: null,
      value_number: null,
      value_date: null,
      value_boolean: null,
    })).toBe("—");
  });

  it("formats BOOLEAN attribute data type", () => {
    expect(formatAttributeValue({
      data_type: "BOOLEAN",
      value_text: null,
      value_number: null,
      value_date: null,
      value_boolean: true,
    })).toBe("Yes");

    expect(formatAttributeValue({
      data_type: "BOOLEAN",
      value_text: null,
      value_number: null,
      value_date: null,
      value_boolean: false,
    })).toBe("No");

    expect(formatAttributeValue({
      data_type: "BOOLEAN",
      value_text: null,
      value_number: null,
      value_date: null,
      value_boolean: null,
    })).toBe("—");
  });

  it("formats TEXT and default attribute data types", () => {
    expect(formatAttributeValue({
      data_type: "TEXT",
      value_text: "Dell XPS 15",
      value_number: null,
      value_date: null,
      value_boolean: null,
    })).toBe("Dell XPS 15");

    expect(formatAttributeValue({
      data_type: "TEXT",
      value_text: null,
      value_number: null,
      value_date: null,
      value_boolean: null,
    })).toBe("—");
  });
});
