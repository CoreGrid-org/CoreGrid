import { describe, expect, it } from "vitest";
import { formatStatusLabel, statusTagColor } from "./statusTag";

describe("formatStatusLabel", () => {
  it("splits a SCREAMING_SNAKE_CASE status into title case words", () => {
    expect(formatStatusLabel("UNDER_MAINTENANCE")).toBe("Under Maintenance");
  });

  it("splits a PascalCase status the same way", () => {
    expect(formatStatusLabel("UnderMaintenance")).toBe("Under Maintenance");
  });

  it("title-cases a single word", () => {
    expect(formatStatusLabel("ACTIVE")).toBe("Active");
  });
});

describe("statusTagColor", () => {
  it("maps a known status to its colour", () => {
    expect(statusTagColor("ACTIVE")).toBe("green");
    expect(statusTagColor("CONDEMNED")).toBe("red");
  });

  it("normalises PascalCase input the same as SCREAMING_SNAKE_CASE", () => {
    expect(statusTagColor("UnderMaintenance")).toBe(statusTagColor("UNDER_MAINTENANCE"));
  });

  it("falls back to gray for an unrecognised status", () => {
    expect(statusTagColor("SOMETHING_UNEXPECTED")).toBe("gray");
  });
});
