import type { BarChartDatum } from "../components/BarChart";
import type { LinePoint } from "../components/LineChart";

// The stat tiles are real now (GET /api/dashboard/summary — see
// hooks/useDashboardSummary.ts). Only the charts below remain mock — FR-082
// (assets by department/condition, maintenance cost by month) has no
// backend yet.

// FR-082 — "assets by department" (nominal categorical → single hue, slot 1 blue).
export const MOCK_ASSETS_BY_DEPARTMENT: BarChartDatum[] = [
  { label: "Fleet Operations", value: 412 },
  { label: "Facilities", value: 268 },
  { label: "IT & Equipment", value: 231 },
  { label: "Warehouse", value: 219 },
  { label: "Maintenance", value: 154 },
];

// FR-082 — "assets by condition" (ordinal: New → Unserviceable → one-hue ramp,
// light to dark, per dataviz skill's ordinal treatment).
export const MOCK_ASSETS_BY_CONDITION: BarChartDatum[] = [
  { label: "New", value: 318, color: "#86b6ef" },
  { label: "Good", value: 542, color: "#5598e7" },
  { label: "Fair", value: 274, color: "#2a78d6" },
  { label: "Poor", value: 106, color: "#1c5cab" },
  { label: "Unserviceable", value: 44, color: "#104281" },
];

// FR-082 — "maintenance cost by month" (single time series → one hue).
export const MOCK_MAINTENANCE_COST_BY_MONTH: LinePoint[] = [
  { label: "Jan", value: 4200 },
  { label: "Feb", value: 3800 },
  { label: "Mar", value: 5100 },
  { label: "Apr", value: 4650 },
  { label: "May", value: 6200 },
  { label: "Jun", value: 5400 },
  { label: "Jul", value: 4900 },
  { label: "Aug", value: 7100 },
  { label: "Sep", value: 6300 },
  { label: "Oct", value: 5800 },
  { label: "Nov", value: 4700 },
  { label: "Dec", value: 5950 },
];
