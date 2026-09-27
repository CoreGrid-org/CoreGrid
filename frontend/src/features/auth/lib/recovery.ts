// ThunderID's hosted password-recovery page for this application. Needs the
// ThunderID *Application ID* (not the OAuth Client ID) — the gate only accepts
// that. Returns null when it isn't configured, so callers can fall back to
// the "Forgot password?" link on ThunderID's own sign-in page.
export function thunderIdRecoveryUrl(): string | null {
  const baseUrl = import.meta.env.VITE_THUNDERID_BASE_URL;
  const applicationId = import.meta.env.VITE_THUNDERID_APPLICATION_ID;
  if (!baseUrl || !applicationId) return null;
  return `${baseUrl.replace(/\/$/, "")}/gate/recovery?applicationId=${encodeURIComponent(applicationId)}`;
}
