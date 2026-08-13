// All mock — Component D's audit slice has no backend yet (PROGRESS.md).
// Shapes follow doc/SRS/06-functional-requirements.md §6.7 and
// doc/SRS/system.md (Appendix F) §F.10.

export interface MockCampaign {
  name: string;
  period: string;
  scope: string;
  status: string;
  verified: number;
  total: number;
  discrepancies: number;
}

// FR-056, FR-057, FR-065, FR-066.
export const MOCK_CAMPAIGNS: MockCampaign[] = [
  { name: "Q3 2026 Ward Equipment Verification", period: "01 Jul – 30 Sep 2026", scope: "Ward Services · Medical Equipment", status: "ACTIVE", verified: 142, total: 210, discrepancies: 6 },
  { name: "Fleet Mid-Year Check", period: "01 Jun – 30 Jun 2026", scope: "Fleet Operations · Vehicles & Fleet", status: "COMPLETED", verified: 38, total: 38, discrepancies: 2 },
  { name: "IT Asset Annual Audit 2026", period: "01 Sep – 31 Oct 2026", scope: "IT & Equipment (all departments)", status: "DRAFT", verified: 0, total: 96, discrepancies: 0 },
];

export interface MockDiscrepancy {
  assetCode: string;
  classification: string;
  status: string;
  raisedBy: string;
  date: string;
}

// FR-060 to FR-062. Classification uses DiscrepancyClassification
// (Appendix A) — includes SURPLUS, which VerificationResult deliberately
// does not (doc/SRS/system.md §F.4 point 4).
export const MOCK_DISCREPANCIES: MockDiscrepancy[] = [
  { assetCode: "ORG-PMN-0034", classification: "CONDITION_MISMATCH", status: "OPEN", raisedBy: "System (auto)", date: "2026-08-10" },
  { assetCode: "ORG-VAN-0011", classification: "LOCATION_MISMATCH", status: "UNDER_REVIEW", raisedBy: "System (auto)", date: "2026-08-09" },
  { assetCode: "ORG-CHR-0211", classification: "DATA_MISMATCH", status: "RESOLVED", raisedBy: "Dilani Jayasuriya", date: "2026-08-02" },
  { assetCode: "ORG-NSW-0021", classification: "SURPLUS", status: "OPEN", raisedBy: "Nadeesha Perera", date: "2026-08-11" },
  { assetCode: "ORG-HBD-0061", classification: "MISSING", status: "RESOLVED", raisedBy: "System (auto)", date: "2026-07-18" },
];

export interface MockAuditLogEntry {
  timestamp: string;
  actor: string;
  entity: string;
  operation: string;
  correlationId: string;
}

// FR-063, FR-064 — append-only, never editable or deletable (DR-12).
export const MOCK_AUDIT_LOG: MockAuditLogEntry[] = [
  { timestamp: "2026-08-12 09:14:02", actor: "Hasitha Erandika", entity: "DisposalRequest #ORG-PMN-0034", operation: "APPROVE", correlationId: "c9f2a1e0" },
  { timestamp: "2026-08-12 08:55:41", actor: "System", entity: "AgentWorkflow #a341", operation: "AWAITING_APPROVAL", correlationId: "a34117bd" },
  { timestamp: "2026-08-11 16:40:07", actor: "Hasitha Erandika", entity: "AssetTransfer #ORG-VAN-0011", operation: "APPROVE", correlationId: "77c0e921" },
  { timestamp: "2026-08-11 11:22:15", actor: "Dilani Jayasuriya", entity: "Discrepancy #ORG-CHR-0211", operation: "RESOLVE", correlationId: "5b8d3f44" },
  { timestamp: "2026-08-10 14:55:39", actor: "Ishara Wickrama", entity: "MaintenanceRecord #ORG-HBD-0061", operation: "COMPLETE", correlationId: "e112dc0a" },
  { timestamp: "2026-08-09 10:03:52", actor: "System", entity: "Asset #ORG-PMN-0034", operation: "VERIFY", correlationId: "9930acb2" },
];
