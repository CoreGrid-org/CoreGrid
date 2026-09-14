import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { thunderIDTestDouble } from "@/test/mocks/thunderid";
import type { Discrepancy } from "../api/discrepancies";

vi.mock("@thunderid/react", () => ({
  useThunderID: () => thunderIDTestDouble(),
}));

const { listCampaignsMock, listDiscrepanciesMock } = vi.hoisted(() => ({
  listCampaignsMock: vi.fn(),
  listDiscrepanciesMock: vi.fn(),
}));
vi.mock("../api/campaigns", () => ({
  listCampaigns: listCampaignsMock,
  createCampaign: vi.fn(),
}));
vi.mock("../api/discrepancies", () => ({
  listDiscrepancies: listDiscrepanciesMock,
  resolveDiscrepancy: vi.fn(),
}));

vi.mock("./ResolveDiscrepancyModal", () => ({
  default: ({ discrepancy }: { discrepancy: Discrepancy }) => <div role="dialog">Resolve {discrepancy.asset_code}</div>,
}));

import DiscrepanciesPanel from "./DiscrepanciesPanel";

const OPEN: Discrepancy = {
  id: "disc-1",
  campaign_id: "c1",
  verification_task_id: "t1",
  asset_id: "a1",
  asset_code: "MOHSL-MED-MON-0002",
  type: "ConditionMismatch",
  is_automatic: true,
  raised_by_user_id: null,
  raised_by_email: null,
  description: "Condition recorded as Poor during scan.",
  photo_url: null,
  status: "Open",
  resolution_type: null,
  resolution_explanation: null,
  corrective_action: null,
  register_corrected: false,
  resolved_by_user_id: null,
  resolved_at: null,
  created_at: "2026-09-12T00:00:00Z",
} as Discrepancy;

const RESOLVED: Discrepancy = {
  ...OPEN,
  id: "disc-2",
  asset_code: "MOHSL-FUR-WARD-0001",
  status: "Resolved",
  is_automatic: false,
  raised_by_email: "auditor@mohsl.gov.lk",
};

describe("DiscrepanciesPanel", () => {
  it("shows an empty state when nothing matches the filters", async () => {
    listCampaignsMock.mockResolvedValue([]);
    listDiscrepanciesMock.mockResolvedValue([]);
    render(<DiscrepanciesPanel />);
    expect(await screen.findByText("No discrepancies match these filters.")).toBeInTheDocument();
  });

  it("shows a Resolve button only for open discrepancies, and 'System (auto)' for automatic ones", async () => {
    listCampaignsMock.mockResolvedValue([]);
    listDiscrepanciesMock.mockResolvedValue([OPEN, RESOLVED]);
    render(<DiscrepanciesPanel />);

    expect(await screen.findByText("MOHSL-MED-MON-0002")).toBeInTheDocument();
    expect(screen.getByText("System (auto)")).toBeInTheDocument();
    expect(screen.getByText("auditor@mohsl.gov.lk")).toBeInTheDocument();
    // Only the Open row gets a Resolve action.
    expect(screen.getAllByRole("button", { name: "Resolve" })).toHaveLength(1);
  });

  it("opens the resolve modal for the clicked discrepancy", async () => {
    listCampaignsMock.mockResolvedValue([]);
    listDiscrepanciesMock.mockResolvedValue([OPEN]);
    const user = userEvent.setup();
    render(<DiscrepanciesPanel />);

    await user.click(await screen.findByRole("button", { name: "Resolve" }));

    expect(screen.getByText("Resolve MOHSL-MED-MON-0002")).toBeInTheDocument();
  });

  it("shows an error notification when the list fails to load", async () => {
    listCampaignsMock.mockResolvedValue([]);
    listDiscrepanciesMock.mockRejectedValue(new Error("network down"));
    render(<DiscrepanciesPanel />);
    expect(await screen.findByText("Could not load discrepancies")).toBeInTheDocument();
  });
});
