// All mock — Component C (Transfer & Disposal) has no backend yet
// (PROGRESS.md). Shapes follow doc/SRS/06-functional-requirements.md §6.6
// and doc/SRS/system.md (Appendix F) §F.9.

export interface MockTransfer {
  assetCode: string;
  assetName: string;
  fromDepartment: string;
  toDepartment: string;
  status: string;
  requestedBy: string;
  requestedAt: string;
}

// FR-043 to FR-048.
export const MOCK_TRANSFERS: MockTransfer[] = [
  { assetCode: "ORG-VAN-0011", assetName: "Toyota HiAce", fromDepartment: "Fleet Operations", toDepartment: "Warehouse", status: "REQUESTED", requestedBy: "Kasun Fernando", requestedAt: "2026-08-09" },
  { assetCode: "ORG-LAP-0143", assetName: "Dell Latitude 5440", fromDepartment: "Finance", toDepartment: "Human Resources", status: "APPROVED", requestedBy: "Dilani Jayasuriya", requestedAt: "2026-08-06" },
  { assetCode: "ORG-CHR-0210", assetName: "Herman Miller Aeron", fromDepartment: "Finance", toDepartment: "IT & Equipment", status: "IN_TRANSIT", requestedBy: "Dilani Jayasuriya", requestedAt: "2026-08-04" },
  { assetCode: "ORG-PMN-0033", assetName: "Philips IntelliVue MX450", fromDepartment: "Ward Services", toDepartment: "Ward Services", status: "COMPLETED", requestedBy: "Ishara Wickrama", requestedAt: "2026-07-28" },
  { assetCode: "ORG-NSW-0021", assetName: "Cisco Catalyst 9200", fromDepartment: "IT & Equipment", toDepartment: "Facilities", status: "REJECTED", requestedBy: "Nadeesha Perera", requestedAt: "2026-07-20" },
];

export interface MockPrecondition {
  id: string;
  label: string;
  satisfied: boolean;
}

export interface MockDisposal {
  assetCode: string;
  assetName: string;
  proposedMethod: string;
  status: string;
  valuation: number | null;
  requestedBy: string;
  preconditions: MockPrecondition[];
}

// FR-049 to FR-055 — FR-051's P1–P6 preconditions are shown for the one
// PENDING_APPROVAL row to demonstrate the live checklist the React approval
// screen is required to render before Approve is enabled.
export const MOCK_DISPOSALS: MockDisposal[] = [
  {
    assetCode: "ORG-PMN-0034",
    assetName: "Philips IntelliVue MX450",
    proposedMethod: "AUCTION",
    status: "PENDING_APPROVAL",
    valuation: 650,
    requestedBy: "Ishara Wickrama",
    preconditions: [
      { id: "P1", label: "Asset status is CONDEMNED", satisfied: true },
      { id: "P2", label: "Valuation amount and date recorded", satisfied: true },
      { id: "P3", label: "Elapsed service life ≥ minimum for asset type", satisfied: true },
      { id: "P4", label: "No open maintenance record", satisfied: true },
      { id: "P5", label: "No open transfer record", satisfied: true },
      { id: "P6", label: "Linked agentic workflow reached AWAITING_APPROVAL with PASS", satisfied: false },
    ],
  },
  {
    assetCode: "ORG-VAN-0014",
    assetName: "Isuzu NPR",
    proposedMethod: "DESTRUCTION",
    status: "DRAFT",
    valuation: null,
    requestedBy: "Kasun Fernando",
    preconditions: [],
  },
  {
    assetCode: "ORG-VAN-0019",
    assetName: "Toyota HiAce",
    proposedMethod: "TRANSFER_TO_ENTITY",
    status: "COMPLETED",
    valuation: 4200,
    requestedBy: "Kasun Fernando",
    preconditions: [],
  },
];
