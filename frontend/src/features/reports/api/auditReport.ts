const API_URL = import.meta.env.VITE_API_URL;

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

export interface AuditReportClassificationRow {
  classification: string;
  raised: number;
  resolved: number;
}

export interface AuditReport {
  from: string | null;
  to: string | null;
  campaigns_in_period: number;
  assets_verified: number;
  assets_in_scope: number;
  open_discrepancies: number;
  by_classification: AuditReportClassificationRow[];
  generated_at: string;
}

export interface AuditReportQuery {
  from?: string;
  to?: string;
  departmentId?: string;
  categoryId?: string;
  status?: string;
}

function toSearchParams(query: AuditReportQuery): URLSearchParams {
  const search = new URLSearchParams();
  if (query.from) search.set("from", query.from);
  if (query.to) search.set("to", query.to);
  if (query.departmentId) search.set("departmentId", query.departmentId);
  if (query.categoryId) search.set("categoryId", query.categoryId);
  if (query.status) search.set("status", query.status);
  return search;
}

// backend/Features/Verification/Controllers/AuditReportController.cs —
// Auditor/Administrator only (FR-084, FR-085, FR-086).
export async function getAuditReport(query: AuditReportQuery, accessToken: string): Promise<AuditReport> {
  const qs = toSearchParams(query).toString();
  const response = await fetch(`${API_URL}/reports/audit${qs ? `?${qs}` : ""}`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not load the audit report (${response.status}).`);
  }
  return response.json();
}

export async function downloadAuditReportExport(
  query: AuditReportQuery,
  format: "pdf" | "csv",
  accessToken: string,
): Promise<void> {
  const search = toSearchParams(query);
  search.set("format", format);
  const response = await fetch(`${API_URL}/reports/audit/export?${search.toString()}`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not export the report (${response.status}).`);
  }

  const disposition = response.headers.get("Content-Disposition");
  const filename = disposition?.match(/filename="?([^"]+)"?/)?.[1] ?? `audit-report.${format}`;

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
