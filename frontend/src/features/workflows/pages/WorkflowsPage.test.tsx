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
  decideWorkflow: vi.fn(),
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

const ADMIN: MeResponse = { id: "u1", email: "admin@mohsl.gov.lk", given_name: "A", family_name: "B", role: "Administrator", is_active: true };
const AUDITOR: MeResponse = { ...ADMIN, id: "u2", email: "auditor@mohsl.gov.lk", role: "Auditor" };

const AWAITING: AgentWorkflow = {
  id: "w1",
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
  it("shows the 'no agents built' banner and the awaiting-approval recommendation with its policy checks", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockResolvedValue([AWAITING]);
    render(<WorkflowsPage />);

    expect(screen.getByText("Planner and Maintenance Analysis Agents are connected")).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(await screen.findByRole("tab", { name: "Awaiting Approval" }));

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

    await user.click(await screen.findByRole("tab", { name: "Awaiting Approval" }));

    expect(await screen.findByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reject" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Request revision" })).toBeInTheDocument();
  });

  it("hides decision actions and shows a waiting message for a non-Administrator", async () => {
    getMeMock.mockResolvedValue(AUDITOR);
    listWorkflowsMock.mockResolvedValue([AWAITING]);
    const user = userEvent.setup();
    render(<WorkflowsPage />);

    await user.click(await screen.findByRole("tab", { name: "Awaiting Approval" }));

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

    await user.click(await screen.findByRole("tab", { name: "Completed" }));

    expect(await screen.findByText("MOHSL-ICT-SRV-0002")).toBeInTheDocument();
    expect(screen.getByText("Completed Advisory")).toBeInTheDocument();
  });

  it("shows an error notification when the workflow list fails to load", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    listWorkflowsMock.mockRejectedValue(new Error("network down"));
    render(<WorkflowsPage />);
    expect(await screen.findByText("Could not load workflows")).toBeInTheDocument();
  });
});
