// All mock — organisation configuration has no backend yet beyond Users
// (PROGRESS.md). Shapes follow doc/SRS/06-functional-requirements.md §6.2
// and doc/SRS/system.md (Appendix F) §F.6.

export interface MockDepartment {
  code: string;
  name: string;
  locationCount: number;
  userCount: number;
  isActive: boolean;
}

// FR-010, FR-012.
export const MOCK_DEPARTMENTS: MockDepartment[] = [
  { code: "FLT", name: "Fleet Operations", locationCount: 3, userCount: 8, isActive: true },
  { code: "FAC", name: "Facilities", locationCount: 4, userCount: 5, isActive: true },
  { code: "ITE", name: "IT & Equipment", locationCount: 2, userCount: 4, isActive: true },
  { code: "WHS", name: "Warehouse", locationCount: 2, userCount: 6, isActive: true },
  { code: "WRD", name: "Ward Services", locationCount: 3, userCount: 12, isActive: true },
  { code: "FIN", name: "Finance", locationCount: 1, userCount: 3, isActive: true },
  { code: "HRD", name: "Human Resources", locationCount: 1, userCount: 2, isActive: false },
];

export interface MockLocation {
  name: string;
  type: string;
  department: string;
  isActive: boolean;
}

// FR-011, FR-012 — type is illustrative, not an exhaustive enum ("such as
// store, workshop, office or ward"); doc/SRS/system.md §F.6 deliberately
// leaves this column unconstrained for the same reason.
export const MOCK_LOCATIONS: MockLocation[] = [
  { name: "Depot A — Bay 1", type: "Workshop", department: "Fleet Operations", isActive: true },
  { name: "Depot A — Bay 2", type: "Workshop", department: "Fleet Operations", isActive: true },
  { name: "Depot B", type: "Store", department: "Fleet Operations", isActive: true },
  { name: "HQ — IT Store", type: "Store", department: "IT & Equipment", isActive: true },
  { name: "HQ — Server Room", type: "Workshop", department: "IT & Equipment", isActive: true },
  { name: "Ward 4B", type: "Ward", department: "Ward Services", isActive: true },
  { name: "Ward 2A", type: "Ward", department: "Ward Services", isActive: true },
  { name: "HQ — 3rd Floor", type: "Office", department: "Finance", isActive: true },
  { name: "HQ — 2nd Floor", type: "Office", department: "Human Resources", isActive: false },
];

export interface MockPolicy {
  label: string;
  value: string;
  purpose: string;
}

// FR-015; §7.6 rules PR-01 to PR-09 consume these thresholds.
export const MOCK_POLICIES: MockPolicy[] = [
  { label: "Repair-to-replace cost threshold", value: "0.65", purpose: "PR-04 — above this ratio, Budget Analysis favours REPLACE over REPAIR" },
  { label: "Minimum service life before disposal", value: "5 years", purpose: "PR-02 — a disposal recommendation requires elapsed service life at or above this" },
  { label: "Maximum acceptable failure frequency", value: "3 / year", purpose: "Feeds the Maintenance Analysis Agent's cost-trend assessment" },
  { label: "Valuation validity window", value: "90 days", purpose: "PR-03 — a disposal valuation older than this forces NEEDS_REVISION" },
  { label: "Confidence floor", value: "0.70", purpose: "PR-08 — below this, human review is forced regardless of the recommended action" },
  { label: "Cost variance tolerance", value: "15%", purpose: "FR-038 BR1 — maintenance completion is rejected above this without a recorded justification" },
  { label: "Outstanding transfer threshold", value: "7 days", purpose: "FR-048 — an approved but unconfirmed transfer is flagged on the dashboard" },
  { label: "Approval overdue period", value: "48 hours", purpose: "AI-19 — a workflow awaiting approval past this is surfaced as overdue" },
];
