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

export type VerificationTaskStatus = "Pending" | "Completed";

export interface VerificationTask {
  id: string;
  campaign_id: string;
  campaign_name: string;
  asset_id: string;
  asset_code: string;
  asset_name: string;
  assigned_to_user_id: string | null;
  assigned_to_email: string | null;
  due_date: string;
  status: VerificationTaskStatus;
  asserted_present: boolean | null;
  asserted_location_id: string | null;
  asserted_location_name: string | null;
  asserted_condition: string | null;
  completed_at: string | null;
}

export async function listCampaignTasks(
  campaignId: string,
  accessToken: string
): Promise<VerificationTask[]> {
  const response = await fetch(`${API_URL}/verification-tasks?campaignId=${campaignId}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load verification tasks.");
}
