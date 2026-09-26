// Dates display as YYYY-MM-DD app-wide.
export { formatDate } from "@/shared/lib/dates";

export const formatLkr = (value: number | null | undefined) =>
  value != null ? `LKR ${Number(value).toLocaleString()}` : "-";
