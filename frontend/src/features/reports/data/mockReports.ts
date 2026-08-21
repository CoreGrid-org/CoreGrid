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
    key: "maintenance",
    title: "Maintenance Report",
    description: "Cost and repair-count trends across the fleet, filterable by date range and department.",
    requirement: "FR-084",
    stats: [
      { label: "Records this period", value: "58" },
      { label: "Total cost", value: "LKR 64,250" },
      { label: "Average cost per repair", value: "LKR 1,108" },
    ],
    columns: ["Asset type", "Repairs", "Total cost"],
    rows: [
      { "Asset type": "Delivery Van", Repairs: 21, "Total cost": "LKR 38,400" },
      { "Asset type": "Patient Monitor", Repairs: 9, "Total cost": "LKR 11,850" },
      { "Asset type": "Laptop", Repairs: 14, "Total cost": "LKR 4,200" },
      { "Asset type": "Network Switch", Repairs: 3, "Total cost": "LKR 2,100" },
    ],
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
// used to live here as a fifth mock entry; the other three above
// (inventory/maintenance/disposal) belong to Components A/B/C and stay mock
// until those components build their own report backends.
