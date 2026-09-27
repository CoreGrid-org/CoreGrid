import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { thunderIDTestDouble } from "@/test/mocks/thunderid";
import type { DisposalResponse, TransferResponse, PagedResult } from "../types";

vi.mock("@thunderid/react", () => ({
  useThunderID: () => thunderIDTestDouble(),
}));

const mockTransfers: PagedResult<TransferResponse> = {
  items: [
    {
      id: "tr-1",
      organization_id: "org-1",
      asset_id: "ast-1",
      asset_code: "AST-00101",
      asset_name: "Dell Server R740",
      from_department_id: "dept-1",
      from_department_name: "IT Infrastructure",
      to_department_id: "dept-2",
      to_department_name: "Operations",
      from_location_id: "loc-1",
      from_location_name: "Server Room A",
      to_location_id: "loc-2",
      to_location_name: "Data Center B",
      initiated_by_user_id: "u1",
      initiated_by_user_email: "officer@mohsl.gov.lk",
      approved_by_user_id: null,
      approved_by_user_email: null,
      confirmed_by_user_id: null,
      confirmed_by_user_email: null,
      status: "REQUESTED",
      requested_at: "2026-09-20T10:00:00Z",
      approved_at: null,
      confirmed_at: null,
      rejection_reason: null,
    },
    {
      id: "tr-2",
      organization_id: "org-1",
      asset_id: "ast-2",
      asset_code: "AST-00102",
      asset_name: "Office Projector 4K",
      from_department_id: "dept-1",
      from_department_name: "IT Infrastructure",
      to_department_id: "dept-3",
      to_department_name: "Finance",
      from_location_id: "loc-1",
      from_location_name: "Server Room A",
      to_location_id: "loc-3",
      to_location_name: "Finance Room 102",
      initiated_by_user_id: "u1",
      initiated_by_user_email: "officer@mohsl.gov.lk",
      approved_by_user_id: "admin-1",
      approved_by_user_email: "admin@mohsl.gov.lk",
      confirmed_by_user_id: null,
      confirmed_by_user_email: null,
      status: "APPROVED",
      requested_at: "2026-09-19T10:00:00Z",
      approved_at: "2026-09-19T14:00:00Z",
      confirmed_at: null,
      rejection_reason: null,
    },
  ],
  total_count: 2,
  page: 1,
  page_size: 20,
  total_pages: 1,
};

const mockDisposals: PagedResult<DisposalResponse> = {
  items: [
    {
      id: "disp-1",
      organization_id: "org-1",
      asset_id: "ast-3",
      asset_code: "AST-00201",
      asset_name: "Core Switch Catalyst",
      asset_condition: "Unserviceable",
      asset_status: "CONDEMNED",
      initiated_by_user_id: "u1",
      initiated_by_user_email: "officer@mohsl.gov.lk",
      approved_by_user_id: null,
      approved_by_user_email: null,
      disposal_method: "SCRAP",
      estimated_residual_value: 50000,
      valuation_date: "2026-09-15T00:00:00Z",
      status: "PENDING",
      requested_at: "2026-09-20T09:00:00Z",
      approved_at: null,
      disposed_at: null,
      notes: "Power supply fried, circuit board charred.",
      precondition_evaluation: {
        all_passed: true,
        separation_of_duties_passed: true,
        separation_of_duties_failure_reason: null,
        checks: [
          { code: "P1", description: "Asset must be in CONDEMNED status", passed: true, failure_reason: null },
          { code: "P2", description: "No active maintenance work orders", passed: true, failure_reason: null },
          { code: "P3", description: "No active transfer requests", passed: true, failure_reason: null },
          { code: "P4", description: "No open verification discrepancies", passed: true, failure_reason: null },
          { code: "P5", description: "Residual value does not exceed threshold", passed: true, failure_reason: null },
          { code: "P6", description: "Board of Survey approval recorded", passed: true, failure_reason: null },
        ],
      },
    },
  ],
  total_count: 1,
  page: 1,
  page_size: 20,
  total_pages: 1,
};

const approveTransferMock = vi.fn();
const confirmReceiptMock = vi.fn();
const approveDisposalMock = vi.fn();

vi.mock("../hooks/useTransfers", () => ({
  useTransfersList: () => ({
    data: mockTransfers,
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  }),
  useApproveTransfer: () => ({
    mutate: approveTransferMock,
    isPending: false,
    isError: false,
    error: null,
  }),
  useConfirmTransferReceipt: () => ({
    mutate: confirmReceiptMock,
    isPending: false,
    isError: false,
    error: null,
  }),
}));

vi.mock("../hooks/useDisposals", () => ({
  useDisposalsList: () => ({
    data: mockDisposals,
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  }),
  useApproveDisposal: () => ({
    mutate: approveDisposalMock,
    isPending: false,
    isError: false,
    error: null,
  }),
}));

// Modals are stubbed out to focus on TransfersPage's own rendering and role-gating
vi.mock("../components/InitiateTransferModal", () => ({ default: () => <div data-testid="initiate-transfer-modal" /> }));
vi.mock("../components/CondemnAssetModal", () => ({ default: () => <div data-testid="condemn-asset-modal" /> }));
vi.mock("../components/SubmitDisposalModal", () => ({ default: () => <div data-testid="submit-disposal-modal" /> }));
vi.mock("../components/RequestRevisionModal", () => ({ default: () => <div data-testid="request-revision-modal" /> }));
vi.mock("../components/DisposalDetailModal", () => ({ default: () => <div data-testid="disposal-detail-modal" /> }));

import TransfersPage from "./TransfersPage";

describe("TransfersPage - Role boundary enforcement", () => {
  describe("role='Auditor' (Read-only compliance audit)", () => {
    it("renders zero mutating buttons across both transfer and disposal views", async () => {
      const user = userEvent.setup();
      render(<TransfersPage role="Auditor" />);

      // Page title reflects audit view
      expect(screen.getByText("Transfers & Disposals Audit")).toBeInTheDocument();

      // Header creation buttons must NOT render
      expect(screen.queryByRole("button", { name: /initiate transfer/i })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: /condemn asset/i })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: /submit disposal/i })).not.toBeInTheDocument();

      // Transfer table action buttons must NOT render
      expect(screen.queryByRole("button", { name: /approve/i })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: /confirm receipt/i })).not.toBeInTheDocument();

      // Switch to Disposal compliance tab
      await user.click(screen.getByRole("tab", { name: "Disposal compliance" }));

      // Disposal table must NOT render mutating action buttons
      expect(screen.queryByRole("button", { name: /request revision/i })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: /approve disposal/i })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: /approve/i })).not.toBeInTheDocument();
    });

    it("renders the read-only 'View compliance details' action in the disposal compliance view", async () => {
      const user = userEvent.setup();
      render(<TransfersPage role="Auditor" />);

      await user.click(screen.getByRole("tab", { name: "Disposal compliance" }));

      const inspectButton = screen.getByRole("button", { name: /view compliance details/i });
      expect(inspectButton).toBeInTheDocument();
      expect(inspectButton).toHaveClass("cds--btn--icon-only");

      // Clicking opens the read-only detail modal
      await user.click(inspectButton);
      expect(screen.getByTestId("disposal-detail-modal")).toBeInTheDocument();
    });
  });

  describe("role='Administrator' (Full operational and approval authority)", () => {
    it("renders header action buttons, transfer approval, receipt confirmation, and disposal approval", async () => {
      const user = userEvent.setup();
      render(<TransfersPage role="Administrator" />);

      // Header creation actions
      expect(screen.getByRole("button", { name: /initiate transfer/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /condemn asset/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /submit disposal/i })).toBeInTheDocument();

      // Transfer table actions: Approve for requested transfer, Confirm receipt for approved transfer
      expect(screen.getByRole("button", { name: /^approve$/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /confirm receipt/i })).toBeInTheDocument();

      // Switch to Disposals tab
      await user.click(screen.getByRole("tab", { name: "Disposals" }));

      // Precondition checklist review section renders with approval and revision actions
      expect(screen.getByText(/precondition checklist: AST-00201/i)).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /approve disposal/i })).toBeInTheDocument();
      expect(screen.getAllByRole("button", { name: /request revision/i }).length).toBeGreaterThan(0);
    });
  });

  describe("role='InventoryOfficer' (Creation and receipt confirmation only)", () => {
    it("renders creation and receipt confirmation actions, but hides approval and revision actions", async () => {
      const user = userEvent.setup();
      render(<TransfersPage role="InventoryOfficer" />);

      // Creation buttons allowed
      expect(screen.getByRole("button", { name: /initiate transfer/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /condemn asset/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /submit disposal/i })).toBeInTheDocument();

      // Receipt confirmation allowed
      expect(screen.getByRole("button", { name: /confirm receipt/i })).toBeInTheDocument();

      // Transfer approval blocked
      expect(screen.queryByRole("button", { name: /^approve$/i })).not.toBeInTheDocument();

      // Switch to Disposals tab
      await user.click(screen.getByRole("tab", { name: "Disposals" }));

      // Approval and revision actions blocked
      expect(screen.queryByRole("button", { name: /approve disposal/i })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: /request revision/i })).not.toBeInTheDocument();
      expect(screen.queryByText(/precondition checklist:/i)).not.toBeInTheDocument();
    });
  });
});
