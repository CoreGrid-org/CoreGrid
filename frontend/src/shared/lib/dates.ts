// Calendar dates are passed around as "YYYY-MM-DD" strings in the user's
// own timezone. Never build one with toISOString(): that converts to UTC
// first, so in Sri Lanka (UTC+5:30) a date picked before 05:30 would become
// the previous day.

/** A Date's local calendar day as "YYYY-MM-DD". */
export function toDateOnly(date: Date = new Date()): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** Local midnight at the start of a "YYYY-MM-DD" day, as an ISO timestamp. */
export function startOfDayIso(dateOnly: string): string {
  return new Date(`${dateOnly}T00:00:00`).toISOString();
}

/** The last millisecond of a "YYYY-MM-DD" day (local time), as an ISO timestamp. */
export function endOfDayIso(dateOnly: string): string {
  return new Date(`${dateOnly}T23:59:59.999`).toISOString();
}

/** Whether an ISO timestamp falls within an inclusive local-day range. */
export function isWithinDayRange(iso: string, from?: string, to?: string): boolean {
  const time = new Date(iso).getTime();
  if (from && time < new Date(startOfDayIso(from)).getTime()) return false;
  if (to && time > new Date(endOfDayIso(to)).getTime()) return false;
  return true;
}

export interface DateRange {
  from?: string;
  to?: string;
}

export interface DateRangeErrors {
  from?: string;
  to?: string;
}

const DATE_ONLY_RE = /^\d{4}-\d{2}-\d{2}$/;

function isRealDate(value: string): boolean {
  if (!DATE_ONLY_RE.test(value)) return false;
  const date = new Date(`${value}T00:00:00`);
  return !isNaN(date.getTime()) && toDateOnly(date) === value;
}

// Both ends optional. Checks each is a real date, not in the future (unless
// allowed), and that the range isn't inverted.
export function validateDateRange(
  { from, to }: DateRange,
  { allowFuture = false, today = toDateOnly() }: { allowFuture?: boolean; today?: string } = {},
): DateRangeErrors {
  const errors: DateRangeErrors = {};

  if (from && !isRealDate(from)) errors.from = "Enter a valid date (yyyy-mm-dd).";
  else if (from && !allowFuture && from > today) errors.from = "Can't be in the future.";

  if (to && !isRealDate(to)) errors.to = "Enter a valid date (yyyy-mm-dd).";
  else if (to && !allowFuture && to > today) errors.to = "Can't be in the future.";
  else if (from && to && !errors.from && to < from) errors.to = "Must be on or after the start date.";

  return errors;
}

export const hasDateRangeErrors = (errors: DateRangeErrors) => Boolean(errors.from || errors.to);

/**
 * Display format for dates across the app: "YYYY-MM-DD" in the user's local
 * day. A plain "YYYY-MM-DD" (a DateOnly from the API) is returned as-is so
 * it can't shift a day through timezone conversion; "-" for no date.
 */
export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return "-";
  if (typeof value === "string" && DATE_ONLY_RE.test(value)) return value;
  const date = typeof value === "string" ? new Date(value) : value;
  return isNaN(date.getTime()) ? "-" : toDateOnly(date);
}

/** "YYYY-MM-DD HH:mm" in local time, for timestamps. */
export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) return "-";
  const date = typeof value === "string" ? new Date(value) : value;
  if (isNaN(date.getTime())) return "-";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${toDateOnly(date)} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
