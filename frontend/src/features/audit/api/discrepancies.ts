const API_URL = import.meta.env.VITE_API_URL;

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

async function handle<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || fallback);
  }
  return response.json();
}

export type DiscrepancyType = "Missing" | "Surplus" | "LocationMismatch" | "ConditionMismatch" | "DataMismatch" | "Other";
export type DiscrepancyStatus = "Open" | "Resolved";

// Only these two have a single, unambiguous register field to correct —
// see ResolveDiscrepancyRequest.ApplyCorrection on the backend.
export const CORRECTABLE_DISCREPANCY_TYPES: DiscrepancyType[] = ["ConditionMismatch", "LocationMismatch"];

export interface Discrepancy {
  id: string;
  campaign_id: string;
  verification_task_id: string;
  asset_id: string;
  asset_code: string;
  type: DiscrepancyType;
  is_automatic: boolean;
  raised_by_user_id: string | null;
  raised_by_email: string | null;
  description: string;
  photo_url: string | null;
  status: DiscrepancyStatus;
  resolution_type: string | null;
  resolution_explanation: string | null;
  corrective_action: string | null;
  register_corrected: boolean;
  resolved_by_user_id: string | null;
  resolved_at: string | null;
  created_at: string;
}

export interface ResolveDiscrepancyRequest {
  resolution_type: string;
  resolution_explanation: string;
  corrective_action?: string;
  apply_correction: boolean;
}

// backend/Features/Verification/Controllers/DiscrepanciesController.cs
export async function listDiscrepancies(
  params: { campaignId?: string; onlyOpen?: boolean },
  accessToken: string,
): Promise<Discrepancy[]> {
  const search = new URLSearchParams();
  if (params.campaignId) search.set("campaignId", params.campaignId);
  if (params.onlyOpen) search.set("onlyOpen", "true");
  const qs = search.toString();

  const response = await fetch(`${API_URL}/discrepancies${qs ? `?${qs}` : ""}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load discrepancies.");
}

// FR-062 — Auditor/Administrator only.
export async function resolveDiscrepancy(
  id: string,
  payload: ResolveDiscrepancyRequest,
  accessToken: string,
): Promise<Discrepancy> {
  const response = await fetch(`${API_URL}/discrepancies/${id}/resolve`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not resolve this discrepancy.");
}
