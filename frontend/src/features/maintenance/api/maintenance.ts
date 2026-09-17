import type {
  MaintenanceRecord,
  MaintenanceQueryParameters,
  PagedMaintenanceRecords,
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
  if (params.assetId) search.set("assetId", params.assetId);
  if (params.departmentId) search.set("departmentId", params.departmentId);
  if (params.assigneeId) search.set("assigneeId", params.assigneeId);
  if (params.status) search.set("status", params.status);
  if (params.type) search.set("type", params.type);
  if (params.priority) search.set("priority", params.priority);
  if (params.dateFrom) search.set("dateFrom", params.dateFrom);
  if (params.dateTo) search.set("dateTo", params.dateTo);
  if (params.sortBy) search.set("sortBy", params.sortBy);
  if (params.sortDirection) search.set("sortDirection", params.sortDirection);
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  const query = search.toString();
  return query ? `?${query}` : "";
}

// FR-042: GET /api/maintenance now returns a PagedResult<MaintenanceRecordDto>
// (server-side filter/sort/pagination), not a bare array.
export async function listMaintenanceRecords(
  params: MaintenanceQueryParameters,
  accessToken: string,
): Promise<PagedMaintenanceRecords> {
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

// backend/Features/Maintenance/Controllers/MaintenanceController.cs UploadPhoto
export async function uploadMaintenancePhoto(file: File, accessToken: string): Promise<string> {
  const formData = new FormData();
  formData.append("photo", file);

  const response = await fetch(`${API_URL}/maintenance/photos`, {
    method: "POST",
    headers: authHeaders(accessToken), // no Content-Type — the browser sets the multipart boundary
    body: formData,
  });
  const result = await handle<{ url: string }>(response, "Could not upload the photo.");
  return result.url;
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
