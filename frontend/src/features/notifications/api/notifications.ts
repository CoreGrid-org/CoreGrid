const API_URL = import.meta.env.VITE_API_URL;

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

async function handle<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || fallback);
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}

export interface Notification {
  id: string;
  type: string;
  title: string;
  message: string;
  related_entity_type?: string;
  related_entity_id?: string;
  is_read: boolean;
  created_at: string;
}

export interface UnreadCount {
  count: number;
}

// backend/Features/Shared/Paging/PagedResult.cs
interface PagedResult<T> {
  items: T[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
}

// backend/Features/Notifications/Controllers/NotificationsController.cs
// GetNotifications is paginated (§7 of the backend refactor plan), replacing
// its old hard Take(50) — the bell panel matches that same ceiling by
// requesting page 1 at pageSize=50 rather than every page (unlike a full
// list page, "every notification ever" isn't the right UX for a dropdown).
export async function listNotifications(onlyUnread: boolean, accessToken: string): Promise<Notification[]> {
  const search = new URLSearchParams({ page: "1", pageSize: "50" });
  if (onlyUnread) search.set("onlyUnread", "true");
  const response = await fetch(`${API_URL}/notifications?${search.toString()}`, { headers: authHeaders(accessToken) });
  const result = await handle<PagedResult<Notification>>(response, "Could not load notifications.");
  return result.items;
}

export async function getUnreadCount(accessToken: string): Promise<UnreadCount> {
  const response = await fetch(`${API_URL}/notifications/unread-count`, { headers: authHeaders(accessToken) });
  return handle(response, "Could not load the unread notification count.");
}

export async function markNotificationAsRead(id: string, accessToken: string): Promise<void> {
  const response = await fetch(`${API_URL}/notifications/${id}/read`, {
    method: "PATCH",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not mark this notification as read.");
}

export async function markAllNotificationsAsRead(accessToken: string): Promise<void> {
  const response = await fetch(`${API_URL}/notifications/read-all`, {
    method: "PATCH",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not mark all notifications as read.");
}
