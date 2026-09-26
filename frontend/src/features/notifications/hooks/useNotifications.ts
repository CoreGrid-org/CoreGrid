import { useCallback, useEffect, useRef, useState } from "react";
import { useThunderID } from "@thunderid/react";
import {
  getUnreadCount,
  listNotifications,
  markAllNotificationsAsRead,
  markNotificationAsRead,
} from "../api/notifications";
import type { Notification } from "../api/notifications";

const POLL_INTERVAL_MS = 30_000;

// Unread-count polling plus the header panel's open/close and list state.
// Polling pauses while the tab is hidden and catches up as soon as it's
// visible again; an open panel reloads its list when new ones arrive.
export function useNotificationCenter() {
  const { getAccessToken, isSignedIn } = useThunderID();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<unknown>(undefined);
  const isOpenRef = useRef(false);
  const unreadRef = useRef(0);

  useEffect(() => {
    isOpenRef.current = isOpen;
  }, [isOpen]);

  const refreshList = useCallback(async () => {
    setIsLoading(true);
    setError(undefined);
    try {
      const token = await getAccessToken();
      setNotifications(await listNotifications(false, token));
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  }, [getAccessToken]);

  const refreshUnreadCount = useCallback(async () => {
    try {
      const token = await getAccessToken();
      const { count } = await getUnreadCount(token);
      // Something new arrived while the panel is open: show it.
      if (isOpenRef.current && count > unreadRef.current) void refreshList();
      unreadRef.current = count;
      setUnreadCount(count);
    } catch {
      // Silent: the bell just won't update this cycle; no need to interrupt
      // whatever page the user is actually looking at.
    }
  }, [getAccessToken, refreshList]);

  useEffect(() => {
    if (!isSignedIn) return;
    // First check right after mount, then every POLL_INTERVAL_MS.
    const initial = setTimeout(() => void refreshUnreadCount(), 0);

    const interval = setInterval(() => {
      if (document.visibilityState === "visible") void refreshUnreadCount();
    }, POLL_INTERVAL_MS);
    const onVisible = () => {
      if (document.visibilityState === "visible") void refreshUnreadCount();
    };
    document.addEventListener("visibilitychange", onVisible);
    window.addEventListener("focus", onVisible);

    return () => {
      clearTimeout(initial);
      clearInterval(interval);
      document.removeEventListener("visibilitychange", onVisible);
      window.removeEventListener("focus", onVisible);
    };
  }, [isSignedIn, refreshUnreadCount]);

  const open = useCallback(() => {
    setIsOpen(true);
    void refreshList();
  }, [refreshList]);

  const close = useCallback(() => setIsOpen(false), []);

  const toggle = useCallback(() => {
    if (isOpenRef.current) close();
    else open();
  }, [open, close]);

  const markAsRead = useCallback(
    async (id: string) => {
      const target = notifications.find((n) => n.id === id);
      if (!target || target.is_read) return;
      setNotifications((current) => current.map((n) => (n.id === id ? { ...n, is_read: true } : n)));
      setUnreadCount((count) => {
        unreadRef.current = Math.max(0, count - 1);
        return unreadRef.current;
      });
      try {
        const token = await getAccessToken();
        await markNotificationAsRead(id, token);
      } catch {
        void refreshList();
        void refreshUnreadCount();
      }
    },
    [notifications, getAccessToken, refreshList, refreshUnreadCount],
  );

  const markAllAsRead = useCallback(async () => {
    setNotifications((current) => current.map((n) => ({ ...n, is_read: true })));
    unreadRef.current = 0;
    setUnreadCount(0);
    try {
      const token = await getAccessToken();
      await markAllNotificationsAsRead(token);
    } catch {
      void refreshList();
      void refreshUnreadCount();
    }
  }, [getAccessToken, refreshList, refreshUnreadCount]);

  return {
    notifications,
    unreadCount,
    isOpen,
    isLoading,
    isError: error !== undefined,
    error,
    toggle,
    close,
    retry: refreshList,
    markAsRead,
    markAllAsRead,
  };
}
