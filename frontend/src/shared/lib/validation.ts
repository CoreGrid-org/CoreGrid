// Mirrors the [EmailAddress] check ASP.NET Core applies server-side —
// good enough to catch typos client-side; the backend remains the source of truth.
export const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function isValidEmail(value: string): boolean {
  return EMAIL_RE.test(value.trim());
}
