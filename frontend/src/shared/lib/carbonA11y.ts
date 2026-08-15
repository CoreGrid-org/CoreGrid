// Silences Carbon's default per-step assistive announcement on ProgressIndicator;
// paired with the assistive-text visually-hidden override in styles/index.scss.
export function silentProgressStatus(): string {
  return "";
}
