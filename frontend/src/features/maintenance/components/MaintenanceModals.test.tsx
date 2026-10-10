import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ApproveMaintenanceModal from "./ApproveMaintenanceModal";
import CancelMaintenanceModal from "./CancelMaintenanceModal";
import CompleteMaintenanceModal from "./CompleteMaintenanceModal";
import type { MaintenanceCostSuggestion, MaintenanceRecord } from "../types/maintenance";

const { approveMock, completeMock, cancelMock, suggestion } = vi.hoisted(() => ({
  approveMock: vi.fn(),
  completeMock: vi.fn(),
  cancelMock: vi.fn(),
  suggestion: { current: undefined as MaintenanceCostSuggestion | undefined },
}));

const mutation = (mutate: ReturnType<typeof vi.fn>) => ({ mutate, isPending: false, isError: false, error: null });

vi.mock("../hooks/useMaintenance", () => ({
  useApproveMaintenance: () => mutation(approveMock),
  useCompleteMaintenance: () => mutation(completeMock),
  useCancelMaintenance: () => mutation(cancelMock),
  useCostSuggestion: () => ({ data: suggestion.current, isLoading: false }),
}));

vi.mock("@/features/users/hooks/useUsers", () => ({
  useUsersList: () => ({
    data: [{ id: "user-7", given_name: "Nimal", family_name: "Perera", email: "nimal@example.test" }],
    isLoading: false,
  }),
}));

const record: MaintenanceRecord = {
  id: "rec-1",
  asset_id: "ast-1",
  asset_code: "AST-00101",
  asset_name: "Infusion Pump",
  asset_type_name: "Pump",
  description: "Alarm keeps sounding",
  observed_condition: "POOR",
  type: "CORRECTIVE",
  priority: "MEDIUM",
  status: "REQUESTED",
  created_at: "2026-10-01T08:00:00Z",
};

const props = () => ({ isOpen: true, onClose: vi.fn(), onSuccess: vi.fn(), record });

beforeEach(() => {
  approveMock.mockReset();
  completeMock.mockReset();
  cancelMock.mockReset();
  suggestion.current = undefined;
});

describe("CompleteMaintenanceModal", () => {
  it("refuses to submit without a valid actual cost", async () => {
    const user = userEvent.setup();
    render(<CompleteMaintenanceModal {...props()} />);

    await user.click(screen.getByRole("button", { name: "Complete" }));

    expect(screen.getByText("Please enter a valid actual cost.")).toBeInTheDocument();
    expect(completeMock).not.toHaveBeenCalled();
  });

  it("asks for the remaining required fields", async () => {
    const user = userEvent.setup();
    render(<CompleteMaintenanceModal {...props()} />);

    await user.type(screen.getByLabelText("Actual Cost (LKR)"), "1200");
    await user.click(screen.getByRole("button", { name: "Complete" }));

    expect(screen.getByText("Please fill in all required fields.")).toBeInTheDocument();
    expect(completeMock).not.toHaveBeenCalled();
  });

  it("submits the completion, leaving out an empty overspend justification", async () => {
    const user = userEvent.setup();
    const p = props();
    render(<CompleteMaintenanceModal {...p} />);

    await user.type(screen.getByLabelText("Actual Cost (LKR)"), "1200.50");
    await user.type(screen.getByLabelText("Work Performed"), "Replaced the alarm sensor");
    await user.selectOptions(screen.getByLabelText("Resulting Condition"), "GOOD");
    await user.click(screen.getByRole("button", { name: "Complete" }));

    expect(completeMock).toHaveBeenCalledTimes(1);
    const [variables, options] = completeMock.mock.calls[0];
    expect(variables).toEqual({
      id: "rec-1",
      payload: {
        actual_cost: 1200.5,
        work_performed: "Replaced the alarm sensor",
        completion_date: new Date().toISOString().split("T")[0],
        resulting_condition: "GOOD",
        overspend_justification: undefined,
      },
    });

    options.onSuccess();
    expect(p.onSuccess).toHaveBeenCalled();
    expect(p.onClose).toHaveBeenCalled();
  });

  it("sends the overspend justification when one is given", async () => {
    const user = userEvent.setup();
    render(<CompleteMaintenanceModal {...props()} />);

    await user.type(screen.getByLabelText("Actual Cost (LKR)"), "9000");
    await user.type(screen.getByLabelText("Work Performed"), "Replaced the whole pump head");
    await user.selectOptions(screen.getByLabelText("Resulting Condition"), "FAIR");
    await user.type(screen.getByLabelText("Overspend Justification (Optional)"), "Part price doubled");
    await user.click(screen.getByRole("button", { name: "Complete" }));

    expect(completeMock.mock.calls[0][0].payload.overspend_justification).toBe("Part price doubled");
  });

  it("offers every condition the backend accepts as a result", () => {
    render(<CompleteMaintenanceModal {...props()} />);

    const values = Array.from((screen.getByLabelText("Resulting Condition") as HTMLSelectElement).options)
      .map((o) => o.value)
      .filter(Boolean);
    expect(values).toEqual(["GOOD", "FAIR", "POOR", "UNSERVICEABLE"]);
  });
});

describe("CancelMaintenanceModal", () => {
  it("requires a reason", async () => {
    const user = userEvent.setup();
    render(<CancelMaintenanceModal {...props()} />);

    await user.type(screen.getByLabelText("Cancellation Reason"), "   ");
    await user.click(screen.getByRole("button", { name: "Confirm Cancellation" }));

    expect(screen.getByText("Please provide a cancellation reason.")).toBeInTheDocument();
    expect(cancelMock).not.toHaveBeenCalled();
  });

  it("cancels with the given reason and closes on success", async () => {
    const user = userEvent.setup();
    const p = props();
    render(<CancelMaintenanceModal {...p} />);

    await user.type(screen.getByLabelText("Cancellation Reason"), "Asset was replaced");
    await user.click(screen.getByRole("button", { name: "Confirm Cancellation" }));

    const [variables, options] = cancelMock.mock.calls[0];
    expect(variables).toEqual({ id: "rec-1", payload: { reason: "Asset was replaced" } });
    options.onSuccess();
    expect(p.onSuccess).toHaveBeenCalled();
    expect(p.onClose).toHaveBeenCalled();
  });
});

describe("ApproveMaintenanceModal", () => {
  it("requires an estimate and then an assignee", async () => {
    const user = userEvent.setup();
    render(<ApproveMaintenanceModal {...props()} />);

    await user.click(screen.getByRole("button", { name: "Approve" }));
    expect(screen.getByText("Please enter a valid estimated cost.")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Estimated Cost (LKR)"), "500");
    await user.click(screen.getByRole("button", { name: "Approve" }));
    expect(screen.getByText("Please select an assignee.")).toBeInTheDocument();
    expect(approveMock).not.toHaveBeenCalled();
  });

  it("approves with the chosen officer and estimate", async () => {
    const user = userEvent.setup();
    render(<ApproveMaintenanceModal {...props()} />);

    await user.click(screen.getByRole("combobox", { name: "Assign to Officer" }));
    await user.click(await screen.findByText("Nimal Perera (nimal@example.test)"));
    await user.type(screen.getByLabelText("Estimated Cost (LKR)"), "750");
    await user.click(screen.getByRole("button", { name: "Approve" }));

    expect(approveMock).toHaveBeenCalledTimes(1);
    expect(approveMock.mock.calls[0][0]).toEqual({ id: "rec-1", payload: { estimated_cost: 750, assignee_id: "user-7" } });
  });

  it("shows the cost suggestion and fills the estimate when it's used", async () => {
    suggestion.current = {
      suggested_cost: 1800,
      low_cost: 1500,
      high_cost: 2100,
      sample_size: 4,
      basis: "ASSET_TYPE",
      basis_label: "Pump",
      priority_matched: true,
      confidence: "HIGH",
      method: "trimmed-mean",
    };
    const user = userEvent.setup();
    render(<ApproveMaintenanceModal {...props()} />);

    expect(screen.getByText(/Suggested LKR 1,800/)).toBeInTheDocument();
    expect(screen.getByText("high confidence")).toBeInTheDocument();
    expect(screen.getByText(/Based on 4 completed records of Pump/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Use" }));

    expect(screen.getByLabelText("Estimated Cost (LKR)")).toHaveValue(1800);
  });

  it("tells the approver to enter the figure when there's no history", () => {
    suggestion.current = {
      suggested_cost: null,
      low_cost: null,
      high_cost: null,
      sample_size: 0,
      basis: "NONE",
      basis_label: "No completed maintenance history yet",
      priority_matched: false,
      confidence: "NONE",
      method: "none",
    };
    render(<ApproveMaintenanceModal {...props()} />);

    expect(screen.getByText("No completed maintenance history yet. Enter the estimate manually.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Use" })).not.toBeInTheDocument();
  });
});
