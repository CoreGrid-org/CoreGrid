import { useEffect, useRef, useState, type RefObject } from "react";
import { Button, InlineLoading, Tag } from "@carbon/react";
import { ChevronRight, NotificationOff } from "@carbon/icons-react";
import { formatDateTime } from "@/shared/lib/dates";
import type { Notification } from "../api/notifications";
import { notificationLabel, notificationLink, notificationTag, timeAgo } from "../lib/notificationDisplay";

interface NotificationPanelProps {
  notifications: Notification[];
  isLoading: boolean;
  isError: boolean;
  /** The bell and panel together; clicks inside it don't count as "outside". */
  containerRef: RefObject<HTMLElement | null>;
  homeTo: string;
  onClose: () => void;
  onRetry: () => void;
  onOpenNotification: (notification: Notification, link: string | undefined) => void;
  onMarkAllAsRead: () => void;
}

export default function NotificationPanel({
  notifications,
  isLoading,
  isError,
  containerRef,
  homeTo,
  onClose,
  onRetry,
  onOpenNotification,
  onMarkAllAsRead,
}: NotificationPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  // Relative times are measured from when the panel opened.
  const [now] = useState(() => Date.now());

  // Close on a click outside the bell + panel, or on Escape. The bell is
  // excluded so clicking it toggles the panel shut instead of closing and
  // immediately reopening it.
  useEffect(() => {
    const onMouseDown = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) onClose();
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("mousedown", onMouseDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onMouseDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [containerRef, onClose]);

  useEffect(() => {
    panelRef.current?.focus();
  }, []);

  const unread = notifications.filter((n) => !n.is_read).length;
  // Only a first load shows the spinner; a background reload keeps the list.
  const showSpinner = isLoading && notifications.length === 0;

  return (
    <div ref={panelRef} role="dialog" aria-label="Notifications" tabIndex={-1} className="cg-notifications">
      <header className="cg-notifications__header">
        <div>
          <p className="cg-notifications__title">Notifications</p>
          <p className="cg-notifications__subtitle">{unread > 0 ? `${unread} unread` : "All caught up"}</p>
        </div>
        {unread > 0 && (
          <Button kind="ghost" size="sm" onClick={onMarkAllAsRead}>
            Mark all as read
          </Button>
        )}
      </header>

      <div className="cg-notifications__body">
        {showSpinner && <InlineLoading description="Loading notifications…" className="cg-notifications__state" />}

        {isError && !isLoading && (
          <div className="cg-notifications__state cg-notifications__state--error">
            <p>Couldn't load notifications.</p>
            <Button kind="ghost" size="sm" onClick={onRetry}>
              Try again
            </Button>
          </div>
        )}

        {!showSpinner && !isError && notifications.length === 0 && (
          <div className="cg-notifications__empty">
            <NotificationOff size={32} />
            <p>No notifications yet.</p>
            <span>You'll see maintenance assignments and fault report updates here.</span>
          </div>
        )}

        {!showSpinner && notifications.length > 0 && (
          <ul className="cg-notifications__list">
            {notifications.map((n) => {
              const link = notificationLink(n, homeTo);
              return (
                <li key={n.id}>
                  <button
                    type="button"
                    className={`cg-notifications__item${n.is_read ? "" : " is-unread"}`}
                    onClick={() => onOpenNotification(n, link)}
                    aria-label={`${n.is_read ? "" : "Unread: "}${n.title}`}
                  >
                    <span className="cg-notifications__dot" aria-hidden="true" />
                    <span className="cg-notifications__content">
                      <span className="cg-notifications__meta">
                        <Tag type={notificationTag(n.type)} size="sm">
                          {notificationLabel(n.type)}
                        </Tag>
                        <time dateTime={n.created_at} title={formatDateTime(n.created_at)}>
                          {timeAgo(n.created_at, now)}
                        </time>
                      </span>
                      <span className="cg-notifications__item-title">{n.title}</span>
                      <span className="cg-notifications__message">{n.message}</span>
                    </span>
                    {link && <ChevronRight size={16} className="cg-notifications__chevron" />}
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </div>
  );
}
