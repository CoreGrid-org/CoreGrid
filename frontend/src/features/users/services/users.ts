import type { CoreGridRole } from "@/features/auth/lib/roles";

const API_URL = import.meta.env.VITE_API_URL;

export interface CreateUserRequest {
  email: string;
  given_name: string;
  family_name: string;
  password: string;
  role: CoreGridRole;
}

export interface CoreGridUser {
  id: string;
  email: string;
  given_name: string;
  family_name: string;
  role: CoreGridRole;
  department_id: string | null;
  is_active: boolean;
  created_at: string;
}

export type CreateUserResponse = CoreGridUser;

export interface UpdateUserRequest {
  role: CoreGridRole;
  department_id: string | null;
}

export interface PagedUsers {
  items: CoreGridUser[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
}

export interface UserQueryParameters {
  search?: string;
  page?: number;
  pageSize?: number;
}

// Administrator-only (backend/Features/Users/UsersController.cs) — accessToken
// is the caller's own ThunderID access token, obtained via useThunderID().
// GET /api/users now does server-side search + pagination.
export async function listUsers(accessToken: string, params: UserQueryParameters = {}): Promise<PagedUsers> {
  const search = new URLSearchParams();
  if (params.search) search.set("search", params.search);
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  const query = search.toString();

  const response = await fetch(`${API_URL}/users${query ? `?${query}` : ""}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not load users (${response.status}).`);
  }
  return response.json();
}

export async function createUser(payload: CreateUserRequest, accessToken: string): Promise<CreateUserResponse> {
  const response = await fetch(`${API_URL}/users`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not create user (${response.status}).`);
  }
  return response.json();
}

// FR-014: change a user's role or department assignment.
export async function updateUser(
  id: string,
  payload: UpdateUserRequest,
  accessToken: string,
): Promise<CoreGridUser> {
  const response = await fetch(`${API_URL}/users/${id}`, {
    method: "PATCH",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not update user (${response.status}).`);
  }
  return response.json();
}

// FR-014: deactivate/reactivate — never hard-deleted.
export async function setUserActive(id: string, isActive: boolean, accessToken: string): Promise<CoreGridUser> {
  const response = await fetch(`${API_URL}/users/${id}/${isActive ? "activate" : "deactivate"}`, {
    method: "PATCH",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not ${isActive ? "activate" : "deactivate"} user (${response.status}).`);
  }
  return response.json();
}
