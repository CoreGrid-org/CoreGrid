export interface DashboardSummary {
  total_assets: number;
  active_assets: number;
  assets_under_maintenance: number;
  pending_transfers: number;
  pending_disposals: number;
  open_discrepancies: number;
  workflows_awaiting_approval: number;
}

export interface ChartDatum {
  label: string;
  value: number;
}

export interface DashboardCharts {
  assets_by_department: ChartDatum[];
  assets_by_condition: ChartDatum[];
  maintenance_cost_by_month: ChartDatum[];
}

const API_URL = import.meta.env.VITE_API_URL;

// Retrieves organisation-scoped dashboard summary data.
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

// Retrieves dashboard chart data.
export async function getDashboardCharts(accessToken: string): Promise<DashboardCharts> {
  const response = await fetch(`${API_URL}/dashboard/charts`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not load dashboard charts (${response.status}).`);
  }
  return response.json();
}
