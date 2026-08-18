import type {
  MaintenanceRecord,
  MaintenanceQueryParameters,
  ReportFaultRequest,
  CreateMaintenanceRequest,
  ApproveMaintenanceRequest,
  CompleteMaintenanceRequest,
  CancelMaintenanceRequest,
} from "../types/maintenance";

const API_URL = import.meta.env.VITE_API_URL;

async function handle<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || fallback);
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

function buildQuery(params: MaintenanceQueryParameters): string {
  const search = new URLSearchParams();
  if (params.asset_id) search.set("assetId", params.asset_id);
  if (params.status) search.set("status", params.status);
  if (params.type) search.set("type", params.type);
  if (params.priority) search.set("priority", params.priority);
  const query = search.toString();
  return query ? `?${query}` : "";
}

export async function listMaintenanceRecords(
  params: MaintenanceQueryParameters,
  accessToken: string,
): Promise<MaintenanceRecord[]> {
  const response = await fetch(`${API_URL}/maintenance${buildQuery(params)}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load maintenance records.");
}

export async function getMaintenanceRecord(
  id: string,
  accessToken: string,
): Promise<MaintenanceRecord> {
  const response = await fetch(`${API_URL}/maintenance/${id}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load maintenance record.");
}

export async function reportFault(
  payload: ReportFaultRequest,
  accessToken: string,
): Promise<MaintenanceRecord> {
  const response = await fetch(`${API_URL}/maintenance/faults`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not report fault.");
}

export async function createMaintenance(
  payload: CreateMaintenanceRequest,
  accessToken: string,
): Promise<MaintenanceRecord> {
  const response = await fetch(`${API_URL}/maintenance`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create maintenance record.");
}

export async function approveMaintenance(
  id: string,
  payload: ApproveMaintenanceRequest,
  accessToken: string,
): Promise<MaintenanceRecord> {
  const response = await fetch(`${API_URL}/maintenance/${id}/approve`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not approve maintenance record.");
}

export async function startMaintenance(
  id: string,
  accessToken: string,
): Promise<MaintenanceRecord> {
  const response = await fetch(`${API_URL}/maintenance/${id}/start`, {
    method: "POST",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not start maintenance.");
}

export async function completeMaintenance(
  id: string,
  payload: CompleteMaintenanceRequest,
  accessToken: string,
): Promise<MaintenanceRecord> {
  const response = await fetch(`${API_URL}/maintenance/${id}/complete`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not complete maintenance.");
}

export async function cancelMaintenance(
  id: string,
  payload: CancelMaintenanceRequest,
  accessToken: string,
): Promise<MaintenanceRecord> {
  const response = await fetch(`${API_URL}/maintenance/${id}/cancel`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not cancel maintenance.");
}
