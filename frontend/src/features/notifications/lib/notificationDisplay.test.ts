import { describe, expect, it } from "vitest";
import type { Notification } from "../api/notifications";
import { notificationLabel, notificationLink, timeAgo } from "./notificationDisplay";

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
