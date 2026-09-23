import { fetchAllPages } from "@/shared/lib/apiClient";

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

// backend/Features/Shared/Paging/PagedResult.cs
interface PagedResult<T> {
  items: T[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
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

// Retrieves all tasks for a campaign across paginated results.
export async function listCampaignTasks(
  campaignId: string,
  accessToken: string
): Promise<VerificationTask[]> {
  return fetchAllPages((page) =>
    fetch(`${API_URL}/verification-tasks?campaignId=${campaignId}&page=${page}&pageSize=100`, {
      headers: authHeaders(accessToken),
    }).then((response) => handle<PagedResult<VerificationTask>>(response, "Could not load verification tasks.")),
  );
}
