// All mock — the agentic subsystem has no backend yet (PROGRESS.md). Shapes
// follow doc/SRS/07-agentic-ai-subsystem-requirements.md §7.2, §7.5 and
// doc/SRS/system.md (Appendix F) §F.11.

export interface MockActiveWorkflow {
  assetCode: string;
  objective: string;
  status: string;
  currentStep: string;
  startedAt: string;
}

// FR-067 to FR-070.
export const MOCK_ACTIVE_WORKFLOWS: MockActiveWorkflow[] = [
  { assetCode: "ORG-VAN-0014", objective: "Assess whether to repair or replace after third breakdown this year", status: "ANALYZING", currentStep: "Maintenance Analysis Agent", startedAt: "2026-08-12 09:40" },
  { assetCode: "ORG-NSW-0021", objective: "Routine lifecycle check ahead of warranty expiry", status: "PLANNING", currentStep: "Planner Agent", startedAt: "2026-08-12 10:05" },
];

export interface MockApprovalWorkflow {
  assetCode: string;
  objective: string;
  recommendation: string;
  isHighImpact: boolean;
  supportingFactors: string[];
  ruleResults: { ruleId: string; outcome: "PASS" | "FAIL" | "NEEDS_REVISION" }[];
}

// FR-071, FR-072; §7.6, §7.7 — one row shown in full to demonstrate AI-15's
// requirement that the approval screen present objective, plan, findings,
// recommendation and the rule-by-rule validation result before the decision
// controls.
export const MOCK_AWAITING_APPROVAL: MockApprovalWorkflow[] = [
  {
    assetCode: "ORG-PMN-0034",
    objective: "Evaluate disposal after condition dropped to Unserviceable",
    recommendation: "DISPOSE",
    isHighImpact: true,
    supportingFactors: [
      "Repair-to-replace ratio 0.78 exceeds the 0.65 organisation threshold",
      "Elapsed service life 7.2 years ≥ 7-year minimum for Patient Monitor",
      "Valuation of $650 recorded 2026-08-10, within the validity window",
      "No open maintenance or transfer record",
    ],
    ruleResults: [
      { ruleId: "PR-01", outcome: "PASS" },
      { ruleId: "PR-02", outcome: "PASS" },
      { ruleId: "PR-03", outcome: "PASS" },
      { ruleId: "PR-09", outcome: "PASS" },
    ],
  },
];

export interface MockCompletedWorkflow {
  assetCode: string;
  recommendation: string;
  outcome: string;
  completedAt: string;
}

// FR-073 to FR-076.
export const MOCK_COMPLETED_WORKFLOWS: MockCompletedWorkflow[] = [
  { assetCode: "ORG-VAN-0019", recommendation: "DISPOSE", outcome: "APPROVED", completedAt: "2026-08-05" },
  { assetCode: "ORG-LAP-0142", recommendation: "RETAIN", outcome: "COMPLETED_ADVISORY", completedAt: "2026-08-03" },
  { assetCode: "ORG-CHR-0210", recommendation: "REPAIR", outcome: "REJECTED", completedAt: "2026-07-30" },
  { assetCode: "ORG-HBD-0061", recommendation: "RETAIN", outcome: "COMPLETED_ADVISORY", completedAt: "2026-07-25" },
];
