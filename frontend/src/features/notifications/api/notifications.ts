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

// backend/Features/Notifications/Controllers/NotificationsController.cs
export async function listNotifications(onlyUnread: boolean, accessToken: string): Promise<Notification[]> {
  const qs = onlyUnread ? "?onlyUnread=true" : "";
  const response = await fetch(`${API_URL}/notifications${qs}`, { headers: authHeaders(accessToken) });
  return handle(response, "Could not load notifications.");
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
