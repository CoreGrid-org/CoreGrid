import type { CoreGridRole } from "../lib/roles";

const API_URL = import.meta.env.VITE_API_URL;

export interface MeResponse {
  id: string;
  email: string;
  given_name: string;
  family_name: string;
  role: CoreGridRole;
  is_active: boolean;
}

// Backend-authoritative role resolution (backend/Features/Me/MeController.cs)
// — reads CoreGrid's own Users.Role column, not a `roles` claim off the
// ThunderID token, so it doesn't depend on which token that claim ends up on.
export async function getMe(accessToken: string): Promise<MeResponse> {
  const response = await fetch(`${API_URL}/me`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Could not load your profile (${response.status}).`);
  }
  return response.json();
}
