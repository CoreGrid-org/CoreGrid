import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { thunderIDTestDouble } from "@/test/mocks/thunderid";
import type { AgentWorkflow } from "../api/workflows";
import type { MeResponse } from "@/features/auth/services/me";

vi.mock("@thunderid/react", () => ({
  useThunderID: () => thunderIDTestDouble(),
}));

const { listWorkflowsMock, getMeMock } = vi.hoisted(() => ({
  listWorkflowsMock: vi.fn(),
  getMeMock: vi.fn(),
}));
vi.mock("../api/workflows", () => ({
  listWorkflows: listWorkflowsMock,
  createWorkflow: vi.fn(),
  evaluatePolicy: vi.fn(),
  runPolicyAgent: vi.fn(),
  runMaintenanceAgent: vi.fn(),
  resumeWorkflow: vi.fn(),
  decideWorkflow: vi.fn(),
  workflowTitle: (w: { scope: string; asset_code: string; asset_type_name: string }) =>
    w.scope === "ASSET" && w.asset_code ? w.asset_code : `${w.asset_type_name} fleet`,
}));
vi.mock("@/features/auth/services/me", () => ({
  getMe: getMeMock,
}));

// Each opens its own modal with its own data-fetching — stubbed so this
// test stays about WorkflowsPage's own rendering/role-gating.
vi.mock("../components/CreateWorkflowModal", () => ({ default: () => null }));
vi.mock("../components/EvaluatePolicyModal", () => ({ default: () => null }));
vi.mock("../components/DecideWorkflowModal", () => ({ default: () => null }));

import WorkflowsPage from "./WorkflowsPage";

const ADMIN: MeResponse = { id: "u1", email: "admin@mohsl.gov.lk", given_name: "A", family_name: "B", role: "Administrator", is_active: true, organization_name: "Test Organisation", department_id: null };
const AUDITOR: MeResponse = { ...ADMIN, id: "u2", email: "auditor@mohsl.gov.lk", role: "Auditor" };

const AWAITING: AgentWorkflow = {
  id: "w1",
  scope: "ASSET",
  asset_type_id: "t1",
  asset_type_name: "Server",
  category_name: "ICT",
  asset_id: "a1",
  asset_code: "MOHSL-ICT-SRV-0002",
  objective: "Assess for disposal",
  status: "AWAITING_APPROVAL",
  recommendation: "DISPOSE",
  is_high_impact: true,
  plan: null,
  approval_status: "PENDING",
  revision_count: 0,
  failure_reason: null,
  maintenance_analysis: null,
  budget_analysis: null,
  fleet: null,
  validation_result: {
    verdict: "PASS",
    rule_results: [{ rule_id: "PR-01", expected: "CONDEMNED", actual: "CONDEMNED", outcome: "PASS" }],
    blocking_reasons: [],
    is_high_impact: true,
  },
  correlation_id: "corr-1",
  initiated_by_user_id: "u1",
  initiated_by_email: "admin@mohsl.gov.lk",
  started_at: "2026-09-14T08:00:00Z",
  completed_at: null,
  created_at: "2026-09-14T08:00:00Z",
};

describe("WorkflowsPage", () => {
  it("shows the page subtitle and the awaiting-approval recommendation with its policy checks", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockResolvedValue([AWAITING]);
    render(<WorkflowsPage />);

    expect(screen.getByText("Review and approve agent-recommended actions.")).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(await screen.findByRole("tab", { name: /Awaiting Approval/ }));

    expect(await screen.findByText("MOHSL-ICT-SRV-0002")).toBeInTheDocument();
    expect(screen.getByText("Dispose")).toBeInTheDocument();
    expect(screen.getByText("High impact")).toBeInTheDocument();
    expect(screen.getByText("PR-01")).toBeInTheDocument();
    expect(screen.getByText("CONDEMNED → CONDEMNED")).toBeInTheDocument();
  });

  it("shows decision actions for an Administrator", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockResolvedValue([AWAITING]);
    const user = userEvent.setup();
    render(<WorkflowsPage />);

    await user.click(await screen.findByRole("tab", { name: /Awaiting Approval/ }));

    expect(await screen.findByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reject" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Request revision" })).toBeInTheDocument();
  });

  it("hides decision actions and shows a waiting message for a non-Administrator", async () => {
    getMeMock.mockResolvedValue(AUDITOR);
    listWorkflowsMock.mockResolvedValue([AWAITING]);
    const user = userEvent.setup();
    render(<WorkflowsPage />);

    await user.click(await screen.findByRole("tab", { name: /Awaiting Approval/ }));

    expect(await screen.findByText("Awaiting an Administrator's decision.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
  });

  it("does not show 'New evaluation' for a non-Administrator/Officer role", async () => {
    getMeMock.mockResolvedValue(AUDITOR);
    listWorkflowsMock.mockResolvedValue([]);
    render(<WorkflowsPage />);

    await screen.findByText("No evaluations in progress.");
    expect(screen.queryByRole("button", { name: "New evaluation" })).not.toBeInTheDocument();
  });

  it("renders a completed workflow's outcome and completion date", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockResolvedValue([{ ...AWAITING, id: "w2", status: "COMPLETED_ADVISORY", completed_at: "2026-09-14T12:00:00Z" }]);
    const user = userEvent.setup();
    render(<WorkflowsPage />);

    await user.click(await screen.findByRole("tab", { name: /Completed/ }));

    expect(await screen.findByText("MOHSL-ICT-SRV-0002")).toBeInTheDocument();
    expect(screen.getByText("Completed Advisory")).toBeInTheDocument();
  });

  it("renders an asset-type evaluation with its fleet split and per-asset outcomes", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockResolvedValue([
      {
        ...AWAITING,
        id: "w3",
        scope: "ASSET_TYPE",
        asset_id: null,
        asset_code: "",
        recommendation: "REPLACE",
        fleet: {
          asset_count: 3,
          action_counts: { REPLACE: 1, RETAIN: 1 },
          pass_count: 2,
          deferred_count: 1,
          blocked_count: 0,
          assets: [
            { asset_id: "a1", asset_code: "SRV-1", condition: "POOR", action: "REPLACE", verdict: "PASS", is_high_impact: false, ratio: 0.9, projected_cost: 5000, reason: "Financial triage favours REPLACE." },
            { asset_id: "a2", asset_code: "SRV-2", condition: "GOOD", action: "RETAIN", verdict: "PASS", is_high_impact: false, ratio: 0, projected_cost: 0, reason: "No action justified." },
            { asset_id: "a3", asset_code: "SRV-3", condition: "FAIR", action: "REPAIR", verdict: "NEEDS_REVISION", is_high_impact: false, ratio: 0.1, projected_cost: 300, reason: "PR-07: an open maintenance record must be resolved first." },
          ],
        },
      },
    ]);
    const user = userEvent.setup();
    render(<WorkflowsPage />);

    await user.click(await screen.findByRole("tab", { name: "Awaiting Approval (1)" }));

    expect(await screen.findByText("Server fleet")).toBeInTheDocument();
    expect(screen.getByText("Asset type")).toBeInTheDocument();
    expect(screen.getByRole("img", { name: "1 Replace, 1 Retain, 1 Deferred" })).toBeInTheDocument();
    expect(screen.getByText("SRV-3")).toBeInTheDocument();
    expect(screen.getAllByText("Deferred")).toHaveLength(2); // legend + the deferred row
  });

  it("offers to resume an evaluation that is still in progress", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockResolvedValue([{ ...AWAITING, id: "w4", status: "ANALYZING", validation_result: null }]);
    render(<WorkflowsPage />);

    expect(await screen.findByRole("button", { name: "Resume evaluation" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Active (1)" })).toBeInTheDocument();
  });

  it("shows an error notification when the workflow list fails to load", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockRejectedValue(new Error("network down"));
    render(<WorkflowsPage />);
    expect(await screen.findByText("Could not load workflows")).toBeInTheDocument();
  });
});
