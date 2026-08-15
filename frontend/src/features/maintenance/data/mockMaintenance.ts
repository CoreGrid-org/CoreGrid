// All mock — Component B (Maintenance Management) has no backend yet
// (PROGRESS.md). Shapes follow doc/SRS/06-functional-requirements.md §6.5
// and doc/SRS/system.md (Appendix F) §F.8.

export interface MockMaintenanceRecord {
  assetCode: string;
  assetName: string;
  type: "CORRECTIVE" | "PREVENTIVE";
  priority: string;
  status: string;
  assignedTo: string | null;
  estimatedCost: number | null;
  actualCost: number | null;
  requestedAt: string;
}

// FR-033 to FR-042.
export const MOCK_MAINTENANCE_RECORDS: MockMaintenanceRecord[] = [
  { assetCode: "ORG-LAP-0143", assetName: "Dell Latitude 5440", type: "CORRECTIVE", priority: "MEDIUM", status: "IN_PROGRESS", assignedTo: "Nadeesha Perera", estimatedCost: 120, actualCost: null, requestedAt: "2026-08-05" },
  { assetCode: "ORG-VAN-0014", assetName: "Isuzu NPR", type: "CORRECTIVE", priority: "CRITICAL", status: "APPROVED", assignedTo: "Kasun Fernando", estimatedCost: 2400, actualCost: null, requestedAt: "2026-08-08" },
  { assetCode: "ORG-PMN-0034", assetName: "Philips IntelliVue MX450", type: "CORRECTIVE", priority: "HIGH", status: "COMPLETED", assignedTo: "Ishara Wickrama", estimatedCost: 900, actualCost: 1050, requestedAt: "2026-07-22" },
  { assetCode: "ORG-NSW-0021", assetName: "Cisco Catalyst 9200", type: "PREVENTIVE", priority: "LOW", status: "REQUESTED", assignedTo: null, estimatedCost: null, actualCost: null, requestedAt: "2026-08-11" },
  { assetCode: "ORG-HBD-0061", assetName: "Hill-Rom Advanta 2", type: "PREVENTIVE", priority: "LOW", status: "COMPLETED", assignedTo: "Ishara Wickrama", estimatedCost: 60, actualCost: 60, requestedAt: "2026-07-15" },
  { assetCode: "ORG-LAP-0142", assetName: "Dell Latitude 5440", type: "CORRECTIVE", priority: "LOW", status: "CANCELLED", assignedTo: "Nadeesha Perera", estimatedCost: 80, actualCost: null, requestedAt: "2026-07-30" },
];

export interface MockPreventiveSchedule {
  assetType: string;
  intervalDays: number;
  lastCompleted: string;
  nextDue: string;
  daysUntilDue: number;
}

// FR-041 (Should) — scheduled when an asset type's maintenance interval has
// elapsed since the last completed maintenance.
export const MOCK_PREVENTIVE_SCHEDULE: MockPreventiveSchedule[] = [
  { assetType: "Patient Monitor", intervalDays: 90, lastCompleted: "2026-05-20", nextDue: "2026-08-18", daysUntilDue: 5 },
  { assetType: "Delivery Van", intervalDays: 90, lastCompleted: "2026-06-01", nextDue: "2026-08-30", daysUntilDue: 17 },
  { assetType: "Hospital Bed", intervalDays: 180, lastCompleted: "2026-07-15", nextDue: "2027-01-11", daysUntilDue: 151 },
  { assetType: "Laptop", intervalDays: 180, lastCompleted: "2026-03-02", nextDue: "2026-08-29", daysUntilDue: 16 },
];

export interface MockNotification {
  title: string;
  body: string;
  isRead: boolean;
  sentAt: string;
}

// FR-077 to FR-080.
export const MOCK_NOTIFICATIONS: MockNotification[] = [
  { title: "Maintenance assigned", body: "ORG-LAP-0143 assigned to Nadeesha Perera — action required.", isRead: false, sentAt: "2026-08-12 09:14" },
  { title: "Disposal awaiting approval", body: "ORG-PMN-0034 disposal request needs your decision.", isRead: false, sentAt: "2026-08-12 08:02" },
  { title: "Transfer approved", body: "ORG-VAN-0011 transfer to Depot B was approved.", isRead: true, sentAt: "2026-08-11 16:40" },
  { title: "Workflow awaiting approval", body: "Agentic evaluation for ORG-VAN-0014 recommends REPLACE.", isRead: true, sentAt: "2026-08-11 11:20" },
  { title: "Maintenance completed", body: "ORG-HBD-0061 preventive maintenance closed.", isRead: true, sentAt: "2026-08-10 14:55" },
];
