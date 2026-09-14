import { useEffect, useRef } from "react";
import { Button, InlineLoading, Tag } from "@carbon/react";
import type { Notification } from "../api/notifications";

const TYPE_LABEL: Record<string, string> = {
  MAINTENANCE_ASSIGNED: "Assigned",
  MAINTENANCE_COMPLETED: "Completed",
  MAINTENANCE_CANCELLED: "Cancelled",
};

function timeAgo(isoDate: string): string {
  const seconds = Math.floor((Date.now() - new Date(isoDate).getTime()) / 1000);
  if (seconds < 60) return "just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

interface NotificationPanelProps {
  notifications: Notification[];
  isLoading: boolean;
  isError: boolean;
  onClose: () => void;
  onMarkAsRead: (id: string) => void;
  onMarkAllAsRead: () => void;
}

export default function NotificationPanel({
  notifications,
  isLoading,
  isError,
  onClose,
  onMarkAsRead,
  onMarkAllAsRead,
}: NotificationPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (panelRef.current && !panelRef.current.contains(event.target as Node)) {
        onClose();
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [onClose]);

  const hasUnread = notifications.some((n) => !n.is_read);

  return (
    <div
      ref={panelRef}
      role="dialog"
      aria-label="Notifications"
      style={{
        position: "absolute",
        top: "3rem",
        right: "3rem",
        width: "22rem",
        maxHeight: "28rem",
        overflowY: "auto",
        background: "var(--cds-layer, #ffffff)",
        border: "1px solid var(--cds-border-subtle, #e0e0e0)",
        boxShadow: "0 4px 12px rgba(0, 0, 0, 0.15)",
        zIndex: 8000,
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "0.75rem 1rem",
          borderBottom: "1px solid var(--cds-border-subtle, #e0e0e0)",
        }}
      >
        <strong style={{ fontSize: "0.875rem" }}>Notifications</strong>
        {hasUnread && (
          <Button kind="ghost" size="sm" onClick={onMarkAllAsRead}>
            Mark all read
          </Button>
        )}
      </div>

      {isLoading && (
        <div style={{ padding: "1rem" }}>
          <InlineLoading description="Loading…" />
        </div>
      )}

      {isError && !isLoading && (
        <p style={{ padding: "1rem", fontSize: "0.8125rem", color: "#da1e28" }}>Could not load notifications.</p>
      )}

      {!isLoading && !isError && notifications.length === 0 && (
        <p style={{ padding: "1rem", fontSize: "0.8125rem", color: "#525252" }}>You're all caught up.</p>
      )}

      {!isLoading &&
        notifications.map((n) => (
          <button
            key={n.id}
            type="button"
            onClick={() => !n.is_read && onMarkAsRead(n.id)}
            style={{
              display: "block",
              width: "100%",
              textAlign: "left",
              padding: "0.75rem 1rem",
              border: "none",
              borderBottom: "1px solid var(--cds-border-subtle-01, #f4f4f4)",
              background: n.is_read ? "transparent" : "var(--cds-layer-selected, #e8e8e8)",
              cursor: n.is_read ? "default" : "pointer",
            }}
          >
            <div style={{ display: "flex", justifyContent: "space-between", gap: "0.5rem", marginBottom: "0.25rem" }}>
              <Tag type={n.is_read ? "gray" : "blue"} size="sm">
                {TYPE_LABEL[n.type] ?? n.type}
              </Tag>
              <span style={{ fontSize: "0.6875rem", color: "#8d8d8d", whiteSpace: "nowrap" }}>{timeAgo(n.created_at)}</span>
            </div>
            <p style={{ fontSize: "0.8125rem", fontWeight: 600, margin: "0 0 0.125rem" }}>{n.title}</p>
            <p style={{ fontSize: "0.8125rem", color: "#525252", margin: 0 }}>{n.message}</p>
          </button>
        ))}
    </div>
  );
}
