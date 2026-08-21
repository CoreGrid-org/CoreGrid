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

export type CampaignStatus = "Active" | "Completed" | "Cancelled";

export interface Campaign {
  id: string;
  name: string;
  period_start: string;
  period_end: string;
  scope_department_id: string | null;
  scope_department_name: string | null;
  scope_location_id: string | null;
  scope_location_name: string | null;
  scope_asset_category_id: string | null;
  scope_asset_category_name: string | null;
  scope_asset_type_id: string | null;
  scope_asset_type_name: string | null;
  status: CampaignStatus;
  task_count: number;
  completed_task_count: number;
  open_discrepancy_count: number;
  created_at: string;
}

export interface CreateCampaignRequest {
  name: string;
  period_start: string;
  period_end: string;
  scope_department_id?: string | null;
  scope_location_id?: string | null;
  scope_asset_category_id?: string | null;
  scope_asset_type_id?: string | null;
}

// backend/Features/Verification/Controllers/VerificationCampaignsController.cs
// — read is any authenticated org member, create is Auditor/Administrator
// (FR-056). Task generation + officer assignment happens synchronously on
// creation, so the returned campaign already has its task_count populated.
export async function listCampaigns(accessToken: string): Promise<Campaign[]> {
  const response = await fetch(`${API_URL}/verification-campaigns`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load verification campaigns.");
}

export async function createCampaign(payload: CreateCampaignRequest, accessToken: string): Promise<Campaign> {
  const response = await fetch(`${API_URL}/verification-campaigns`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create the campaign.");
}
