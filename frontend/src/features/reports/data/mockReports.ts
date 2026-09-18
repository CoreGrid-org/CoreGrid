// All mock — reporting/export has no backend yet (PROGRESS.md). Shapes
// follow doc/SRS/06-functional-requirements.md §6.10 (FR-081 to FR-086).

export interface ReportStat {
  label: string;
  value: string;
}

export interface ReportRow {
  [key: string]: string | number;
}

export interface MockReport {
  key: string;
  title: string;
  description: string;
  requirement: string;
  stats: ReportStat[];
  columns: string[];
  rows: ReportRow[];
}

export const MOCK_REPORTS: MockReport[] = [
  {
    key: "inventory",
    title: "Asset Inventory Report",
    description: "Every asset in scope, filterable by department, category, status and condition.",
    requirement: "FR-084",
    stats: [
      { label: "Assets in scope", value: "1,284" },
      { label: "Total acquisition value", value: "LKR 2,914,600" },
      { label: "Average age", value: "3.4 years" },
    ],
    columns: ["Department", "Assets", "Total value"],
    rows: [
      { Department: "Fleet Operations", Assets: 412, "Total value": "LKR 1,120,400" },
      { Department: "Facilities", Assets: 268, "Total value": "LKR 342,900" },
      { Department: "IT & Equipment", Assets: 231, "Total value": "LKR 486,200" },
      { Department: "Ward Services", Assets: 219, "Total value": "LKR 812,600" },
    ],
  },
  {
    // Real (2026-09-17) — see MaintenanceReportPanel.tsx. Kept here only so
    // ReportsPage.tsx's tab list (which iterates this array for every tab
    // except Audit) still renders a "Maintenance" tab; the description/
    // stats/columns/rows below aren't used once a real panel exists for a
    // key (see ReportsPage.tsx's report.key === "maintenance" branch).
    key: "maintenance",
    title: "Maintenance Report",
    description: "Cost and repair-count trends across the fleet, filterable by date range and department.",
    requirement: "FR-084",
    stats: [],
    columns: [],
    rows: [],
  },
  {
    key: "disposal",
    title: "Disposal Report",
    description: "Every disposal outcome — method, proceeds and authorising user.",
    requirement: "FR-084",
    stats: [
      { label: "Disposals this period", value: "6" },
      { label: "Total proceeds", value: "LKR 18,900" },
      { label: "Average approval time", value: "2.1 days" },
    ],
    columns: ["Method", "Disposals", "Proceeds"],
    rows: [
      { Method: "Auction", Disposals: 3, Proceeds: "LKR 9,650" },
      { Method: "Transfer to entity", Disposals: 2, Proceeds: "LKR 7,200" },
      { Method: "Destruction", Disposals: 1, Proceeds: "LKR 0" },
    ],
  },
];

// The Audit Campaign Report tab (FR-065, Component D) is real — see
// features/reports/pages/ReportsPage.tsx and hooks/useAuditReport.ts. It
// used to live here as a fifth mock entry. Inventory (Component A) and
// Maintenance (Component B, 2026-09-17) are now real too — see
// InventoryReportPanel.tsx/MaintenanceReportPanel.tsx. Disposal (Component
// C) is the only one still mock, pending its own report backend.
