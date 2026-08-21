import type { CampaignStatus } from "./campaigns";
import type { DiscrepancyStatus, DiscrepancyType } from "./discrepancies";

const API_URL = import.meta.env.VITE_API_URL;

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

export interface CampaignReportCount {
  label: string;
  count: number;
}

export interface CampaignReportTaskRow {
  asset_code: string;
  asset_name: string;
  status: "Pending" | "Completed";
  assigned_to_email: string | null;
  due_date: string;
  completed_at: string | null;
}

export interface CampaignReportDiscrepancyRow {
  asset_code: string;
  type: DiscrepancyType;
  status: DiscrepancyStatus;
  is_automatic: boolean;
  raised_by_email: string | null;
  description: string;
  resolution_type: string | null;
  resolved_at: string | null;
}

export interface CampaignReport {
  campaign_id: string;
  campaign_name: string;
  period_start: string;
  period_end: string;
  scope: string;
  status: CampaignStatus;
  assets_in_scope: number;
  verified: number;
  outstanding: number;
  discrepancies_by_classification: CampaignReportCount[];
  discrepancies_by_resolution_status: CampaignReportCount[];
  tasks: CampaignReportTaskRow[];
  discrepancies: CampaignReportDiscrepancyRow[];
  generated_at: string;
}

// FR-065 — Auditor/Administrator only, matching the backend.
export async function getCampaignReport(campaignId: string, accessToken: string): Promise<CampaignReport> {
  const response = await fetch(`${API_URL}/verification-campaigns/${campaignId}/report`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not load the campaign report (${response.status}).`);
  }
  return response.json();
}

// FR-084/FR-085 — same report rendered server-side as PDF or CSV; the
// filename comes from the server's Content-Disposition header (see the CORS
// exposed-headers config in backend/Program.cs).
export async function downloadCampaignReportExport(
  campaignId: string,
  format: "pdf" | "csv",
  accessToken: string,
): Promise<void> {
  const response = await fetch(
    `${API_URL}/verification-campaigns/${campaignId}/report/export?format=${format}`,
    { headers: authHeaders(accessToken) },
  );
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not export the report (${response.status}).`);
  }

  const disposition = response.headers.get("Content-Disposition");
  const filename = disposition?.match(/filename="?([^"]+)"?/)?.[1] ?? `campaign-report.${format}`;

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
