const API_URL = import.meta.env.VITE_API_URL;

export interface SetupStatus {
  needs_setup: boolean;
}

export interface CompleteSetupRequest {
  admin: {
    email: string;
    given_name: string;
    family_name: string;
    password: string;
  };
  organisation: {
    name: string;
  };
}

export interface CompleteSetupResponse {
  organisation_id: string;
}

export async function getSetupStatus(): Promise<SetupStatus> {
  const response = await fetch(`${API_URL}/setup/status`);
  if (!response.ok) {
    throw new Error(`Could not reach the setup status endpoint (${response.status}).`);
  }
  return response.json();
}

// Real endpoint (POST /api/setup/complete) — SetupController calls
// ThunderIdIdentityDirectory, a real client against ThunderID's management
// API (see doc/setup/ThunderID.md), so this genuinely provisions a ThunderID
// account, not just the local Organization/User rows.
export async function completeSetup(payload: CompleteSetupRequest): Promise<CompleteSetupResponse> {
  const response = await fetch(`${API_URL}/setup/complete`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || `Setup failed (${response.status}).`);
  }
  return response.json();
}
