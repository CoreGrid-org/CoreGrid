import type { Department, Location } from "@/features/assets/types/asset";

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

export interface SaveDepartmentRequest {
  code: string;
  name: string;
}

export interface SaveLocationRequest {
  name: string;
  type: string;
  department_id: string;
}

export interface OrganizationPolicy {
  id: string;
  asset_type_id: string | null;
  asset_type_name: string | null;
  repair_to_replace_cost_threshold: number;
  minimum_service_life_years: number;
  max_acceptable_failure_frequency: number;
  valuation_validity_window_days: number;
  confidence_floor: number;
  cost_variance_tolerance_percent: number;
  outstanding_transfer_days: number;
  approval_overdue_period_hours: number;
}

export type SaveOrganizationPolicyRequest = Omit<OrganizationPolicy, "id" | "asset_type_name">;

// backend/Features/OrgConfig/Controllers/DepartmentsController.cs — any
// authenticated user. GET is already covered by
// features/assets/api/assets.ts#listDepartments (reused by asset-creation
// pickers); these are the mutations that don't exist there.
export async function createDepartment(payload: SaveDepartmentRequest, accessToken: string): Promise<Department> {
  const response = await fetch(`${API_URL}/departments`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create department.");
}

export async function updateDepartment(
  id: string,
  payload: SaveDepartmentRequest,
  accessToken: string,
): Promise<Department> {
  const response = await fetch(`${API_URL}/departments/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update department.");
}

export async function setDepartmentActive(
  id: string,
  isActive: boolean,
  accessToken: string,
): Promise<Department> {
  const response = await fetch(`${API_URL}/departments/${id}/${isActive ? "activate" : "deactivate"}`, {
    method: "PATCH",
    headers: authHeaders(accessToken),
  });
  return handle(response, `Could not ${isActive ? "activate" : "deactivate"} department.`);
}

// backend/Features/OrgConfig/Controllers/LocationsController.cs
export async function createLocation(payload: SaveLocationRequest, accessToken: string): Promise<Location> {
  const response = await fetch(`${API_URL}/locations`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create location.");
}

export async function updateLocation(
  id: string,
  payload: SaveLocationRequest,
  accessToken: string,
): Promise<Location> {
  const response = await fetch(`${API_URL}/locations/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update location.");
}

export async function setLocationActive(id: string, isActive: boolean, accessToken: string): Promise<Location> {
  const response = await fetch(`${API_URL}/locations/${id}/${isActive ? "activate" : "deactivate"}`, {
    method: "PATCH",
    headers: authHeaders(accessToken),
  });
  return handle(response, `Could not ${isActive ? "activate" : "deactivate"} location.`);
}

// backend/Features/OrgConfig/Controllers/OrganizationPoliciesController.cs
// — Administrator only (FR-015).
export async function listOrganizationPolicies(accessToken: string): Promise<OrganizationPolicy[]> {
  const response = await fetch(`${API_URL}/organization-policies`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load policy parameters.");
}

export async function createOrganizationPolicy(
  payload: SaveOrganizationPolicyRequest,
  accessToken: string,
): Promise<OrganizationPolicy> {
  const response = await fetch(`${API_URL}/organization-policies`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create policy.");
}

export async function updateOrganizationPolicy(
  id: string,
  payload: SaveOrganizationPolicyRequest,
  accessToken: string,
): Promise<OrganizationPolicy> {
  const response = await fetch(`${API_URL}/organization-policies/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update policy.");
}
