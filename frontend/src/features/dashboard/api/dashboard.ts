export interface DashboardSummary {
  total_assets: number;
  active_assets: number;
  assets_under_maintenance: number;
  pending_transfers: number;
  pending_disposals: number;
  open_discrepancies: number;
  workflows_awaiting_approval: number;
}

const API_URL = import.meta.env.VITE_API_URL;

// backend/Features/Dashboard/DashboardController.cs — any authenticated
// user, organisation-scoped (FR-081). FR-082's charts (assets by
// department/condition, maintenance cost by month) have no backend yet —
// only these summary counts do.
export async function getDashboardSummary(accessToken: string): Promise<DashboardSummary> {
  const response = await fetch(`${API_URL}/dashboard/summary`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not load dashboard summary (${response.status}).`);
  }
  return response.json();
}
