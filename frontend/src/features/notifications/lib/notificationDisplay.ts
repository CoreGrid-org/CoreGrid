import type { Notification } from "../api/notifications";

// Every type backend/Domain/Notifications/NotificationTypes.cs can send.
const TYPE_LABEL: Record<string, string> = {
  MAINTENANCE_ASSIGNED: "Assigned to you",
  MAINTENANCE_COMPLETED: "Completed",
  MAINTENANCE_CANCELLED: "Cancelled",
  MAINTENANCE_STATUS_CHANGED: "Status update",
};

type TagColor = "blue" | "green" | "red" | "purple" | "gray";
const TYPE_TAG: Record<string, TagColor> = {
  MAINTENANCE_ASSIGNED: "blue",
  MAINTENANCE_COMPLETED: "green",
  MAINTENANCE_CANCELLED: "red",
  MAINTENANCE_STATUS_CHANGED: "purple",
};

export const notificationLabel = (type: string) => TYPE_LABEL[type] ?? "Notification";
export const notificationTag = (type: string): TagColor => TYPE_TAG[type] ?? "gray";

// Where clicking a notification should take the user, under their role's
// route prefix (e.g. "/admin"); undefined when it isn't about a record.
export function notificationLink(n: Notification, homeTo: string): string | undefined {
  if (!n.related_entity_id) return undefined;
  switch (n.related_entity_type) {
    case "MaintenanceRecord":
      return `${homeTo}/maintenance/${n.related_entity_id}`;
    default:
      return undefined;
  }
}

export function timeAgo(isoDate: string, now = Date.now()): string {
  const seconds = Math.floor((now - new Date(isoDate).getTime()) / 1000);
  if (seconds < 60) return "just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return days < 7 ? `${days}d ago` : `${Math.floor(days / 7)}w ago`;
}
