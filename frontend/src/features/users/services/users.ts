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
  is_active: boolean;
  created_at: string;
}

export type CreateUserResponse = CoreGridUser;

// Administrator-only (backend/Features/Users/UsersController.cs) — accessToken
// is the caller's own ThunderID access token, obtained via useThunderID().
export async function listUsers(accessToken: string): Promise<CoreGridUser[]> {
  const response = await fetch(`${API_URL}/users`, {
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
