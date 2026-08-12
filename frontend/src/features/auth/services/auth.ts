export type SelfServiceResetRole = "department-staff" | "inventory-officer" | "auditor";

export interface ForgotPasswordRequest {
  role: SelfServiceResetRole;
  identifier: string;
  secret: string;
}

export interface ForgotPasswordResponse {
  reset_token: string;
}

export interface ResetPasswordRequest {
  token: string;
  new_password: string;
}

// The local-fallback identity API (SRS §4.10) has not been built yet — CoreGrid
// authenticates through ThunderID. These calls are stubs until that fallback exists.
export async function forgotPassword(_payload: ForgotPasswordRequest): Promise<ForgotPasswordResponse> {
  throw new Error("Password reset is not available yet.");
}

export async function resetPassword(_payload: ResetPasswordRequest): Promise<void> {
  throw new Error("Password reset is not available yet.");
}
