export interface AuditLogEntry {
  id: string;
  actor_user_id: string | null;
  actor_email: string | null;
  entity_type: string;
  entity_id: string | null;
  operation: "Create" | "Update" | "Delete";
  changes: string | null;
  correlation_id: string;
  created_at: string;
}

export interface PagedResult<T> {
  items: T[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
}

export interface AuditLogQuery {
  entityType?: string;
  operation?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

const API_URL = import.meta.env.VITE_API_URL;

// backend/Features/Audit/AuditLogController.cs — Auditor/Administrator only,
// read-only (FR-064): no create/update/delete route exists for this at all.
export async function listAuditLog(query: AuditLogQuery, accessToken: string): Promise<PagedResult<AuditLogEntry>> {
  const search = new URLSearchParams();
  if (query.entityType) search.set("entityType", query.entityType);
  if (query.operation) search.set("operation", query.operation);
  if (query.from) search.set("from", query.from);
  if (query.to) search.set("to", query.to);
  if (query.page) search.set("page", String(query.page));
  if (query.pageSize) search.set("pageSize", String(query.pageSize));
  const qs = search.toString();

  const response = await fetch(`${API_URL}/audit-log${qs ? `?${qs}` : ""}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not load the audit log (${response.status}).`);
  }
  return response.json();
}
