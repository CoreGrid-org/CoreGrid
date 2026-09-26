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

export type VerificationOutcome =
  | "Pending"
  | "Verified"
  | "NotFound"
  | "LocationMismatch"
  | "ConditionMismatch"
  | "LocationAndConditionMismatch";

export interface CampaignReportTaskRow {
  asset_code: string;
  asset_name: string;
  asset_type: string | null;
  department: string | null;
  acquisition_cost: number;
  status: "Pending" | "Completed";
  outcome: VerificationOutcome;
  is_overdue: boolean;
  assigned_to_email: string | null;
  assigned_to_name: string | null;
  due_date: string;
  completed_at: string | null;
  completed_by_name: string | null;
  recorded_location: string | null;
  recorded_condition: string | null;
  asserted_present: boolean | null;
  asserted_location: string | null;
  asserted_condition: string | null;
}

export interface CampaignReportDiscrepancyRow {
  id: string;
  asset_code: string;
  asset_name: string | null;
  department: string | null;
  type: DiscrepancyType;
  status: DiscrepancyStatus;
  is_automatic: boolean;
  raised_by_email: string | null;
  raised_by_name: string | null;
  raised_at: string;
  description: string;
  resolution_type: string | null;
  resolution_explanation: string | null;
  corrective_action: string | null;
  register_corrected: boolean;
  resolved_by_name: string | null;
  resolved_at: string | null;
  /** Short-lived signed link; null when there's no photo or storage can't sign one. */
  photo_url: string | null;
  has_photo: boolean;
}

export interface CampaignReportDepartmentRow {
  department: string;
  assets_in_scope: number;
  verified: number;
  outstanding: number;
  discrepancies: number;
  completion_percent: number;
  value_in_scope: number;
}

export interface CampaignReportVerifierRow {
  name: string;
  email: string | null;
  assigned: number;
  completed: number;
  issues_found: number;
}

export interface CampaignReport {
  campaign_id: string;
  campaign_name: string;
  period_start: string;
  period_end: string;
  scope: string;
  status: CampaignStatus;
  organization_name: string | null;
  created_by_name: string | null;
  created_at: string;
  generated_by_name: string | null;
  assets_in_scope: number;
  verified: number;
  outstanding: number;
  overdue_tasks: number;
  completion_percent: number;
  found_as_recorded: number;
  not_found: number;
  location_mismatches: number;
  condition_mismatches: number;
  open_discrepancies: number;
  resolved_discrepancies: number;
  value_in_scope: number;
  value_not_found: number;
  discrepancies_by_classification: CampaignReportCount[];
  discrepancies_by_resolution_status: CampaignReportCount[];
  by_department: CampaignReportDepartmentRow[];
  by_verifier: CampaignReportVerifierRow[];
  tasks: CampaignReportTaskRow[];
  discrepancies: CampaignReportDiscrepancyRow[];
  generated_at: string;
}

//Auditor/Administrator only, matching the backend.
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

//same report rendered server-side as PDF or CSV; the
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
