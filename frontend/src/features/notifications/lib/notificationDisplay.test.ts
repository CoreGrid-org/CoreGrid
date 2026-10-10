import { describe, expect, it } from "vitest";
import type { Notification } from "../api/notifications";
import { notificationLabel, notificationLink, notificationTag, timeAgo } from "./notificationDisplay";

const base: Notification = {
  id: "n1",
  type: "MAINTENANCE_STATUS_CHANGED",
  title: "Fault report status updated",
  message: "Your fault report for AST-1 is now APPROVED.",
  related_entity_type: "MaintenanceRecord",
  related_entity_id: "rec-1",
  is_read: false,
  created_at: "2026-09-26T08:00:00Z",
};

describe("notificationDisplay", () => {
  it("labels every type the backend sends, including status changes", () => {
    expect(notificationLabel("MAINTENANCE_STATUS_CHANGED")).toBe("Status update");
    expect(notificationLabel("SOMETHING_NEW")).toBe("Notification");
  });

  it("links a maintenance notification to the record under the user's role prefix", () => {
    expect(notificationLink(base, "/inventory")).toBe("/inventory/maintenance/rec-1");
    expect(notificationLink({ ...base, related_entity_id: undefined }, "/admin")).toBeUndefined();
  });

  it("formats relative times", () => {
    const now = new Date("2026-09-26T10:00:00Z").getTime();
    expect(timeAgo("2026-09-26T09:59:30Z", now)).toBe("just now");
    expect(timeAgo("2026-09-26T08:00:00Z", now)).toBe("2h ago");
    expect(timeAgo("2026-09-10T10:00:00Z", now)).toBe("2w ago");
  });
});

describe("notificationDisplay — every backend type", () => {
  it.each([
    ["MAINTENANCE_ASSIGNED", "Assigned to you", "blue"],
    ["MAINTENANCE_COMPLETED", "Completed", "green"],
    ["MAINTENANCE_CANCELLED", "Cancelled", "red"],
    ["MAINTENANCE_STATUS_CHANGED", "Status update", "purple"],
  ])("%s has its own label and tag colour", (type, label, tag) => {
    expect(notificationLabel(type)).toBe(label);
    expect(notificationTag(type)).toBe(tag);
  });

  it("falls back to a gray tag for a type it doesn't know", () => {
    expect(notificationTag("SOMETHING_NEW")).toBe("gray");
  });

  it("doesn't link a notification about anything other than a maintenance record", () => {
    expect(notificationLink({ ...base, related_entity_type: "AssetTransfer" }, "/admin")).toBeUndefined();
    expect(notificationLink({ ...base, related_entity_type: undefined }, "/admin")).toBeUndefined();
  });

  it("rolls over at each unit boundary", () => {
    const now = new Date("2026-09-26T10:00:00Z").getTime();
    const ago = (seconds: number) => new Date(now - seconds * 1000).toISOString();
    expect(timeAgo(ago(59), now)).toBe("just now");
    expect(timeAgo(ago(60), now)).toBe("1m ago");
    expect(timeAgo(ago(59 * 60), now)).toBe("59m ago");
    expect(timeAgo(ago(60 * 60), now)).toBe("1h ago");
    expect(timeAgo(ago(24 * 3600 - 1), now)).toBe("23h ago");
    expect(timeAgo(ago(24 * 3600), now)).toBe("1d ago");
    expect(timeAgo(ago(6 * 86400), now)).toBe("6d ago");
    expect(timeAgo(ago(7 * 86400), now)).toBe("1w ago");
  });
});
