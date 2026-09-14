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

// Backs the header bell: unread count polls in the background regardless of
// whether the panel is open; the list itself only loads when the panel opens
// (no point paying for it on every page when nobody's looking at it).
export function useNotificationCenter() {
  const { getAccessToken, isSignedIn } = useThunderID();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<unknown>(undefined);
  const isOpenRef = useRef(isOpen);
  isOpenRef.current = isOpen;

  const refreshUnreadCount = useCallback(async () => {
    try {
      const token = await getAccessToken();
      const { count } = await getUnreadCount(token);
      setUnreadCount(count);
    } catch {
      // Silent — the bell just won't update this cycle; no need to
      // interrupt whatever page the user is actually looking at.
    }
  }, [getAccessToken]);

  const refreshList = useCallback(async () => {
    setIsLoading(true);
    setError(undefined);
    try {
      const token = await getAccessToken();
      const result = await listNotifications(false, token);
      setNotifications(result);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  }, [getAccessToken]);

  useEffect(() => {
    if (!isSignedIn) return;
    refreshUnreadCount();
    const interval = setInterval(refreshUnreadCount, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [isSignedIn, refreshUnreadCount]);

  const toggle = useCallback(() => {
    setIsOpen((wasOpen) => {
      const willOpen = !wasOpen;
      if (willOpen) {
        refreshList();
      }
      return willOpen;
    });
  }, [refreshList]);

  const close = useCallback(() => setIsOpen(false), []);

  const markAsRead = useCallback(
    async (id: string) => {
      setNotifications((current) => current.map((n) => (n.id === id ? { ...n, is_read: true } : n)));
      setUnreadCount((count) => Math.max(0, count - 1));
      try {
        const token = await getAccessToken();
        await markNotificationAsRead(id, token);
      } catch {
        refreshList();
        refreshUnreadCount();
      }
    },
    [getAccessToken, refreshList, refreshUnreadCount],
  );

  const markAllAsRead = useCallback(async () => {
    setNotifications((current) => current.map((n) => ({ ...n, is_read: true })));
    setUnreadCount(0);
    try {
      const token = await getAccessToken();
      await markAllNotificationsAsRead(token);
    } catch {
      refreshList();
      refreshUnreadCount();
    }
  }, [getAccessToken, refreshList, refreshUnreadCount]);

  return { notifications, unreadCount, isOpen, isLoading, isError: error !== undefined, error, toggle, close, markAsRead, markAllAsRead };
}
