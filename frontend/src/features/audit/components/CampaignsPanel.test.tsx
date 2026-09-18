import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { thunderIDTestDouble } from "@/test/mocks/thunderid";
import type { Campaign } from "../api/campaigns";

vi.mock("@thunderid/react", () => ({
  useThunderID: () => thunderIDTestDouble(),
}));

const { listCampaignsMock } = vi.hoisted(() => ({ listCampaignsMock: vi.fn() }));
vi.mock("../api/campaigns", () => ({
  listCampaigns: listCampaignsMock,
  createCampaign: vi.fn(),
}));

// The two modals do their own data-fetching (asset types, users, ...) —
// stubbed out here so this test stays focused on CampaignsPanel's own
// rendering/wiring, not their internals.
vi.mock("./CreateCampaignModal", () => ({
  default: ({ onClose }: { onClose: () => void }) => (
    <div role="dialog" aria-label="create campaign">
      <button onClick={onClose}>Close create modal</button>
    </div>
  ),
}));
vi.mock("./CampaignReportModal", () => ({
  default: ({ campaignName }: { campaignName: string }) => <div role="dialog">Report for {campaignName}</div>,
}));
vi.mock("./CampaignTasksModal", () => ({
  default: ({ campaignName }: { campaignName: string }) => <div role="dialog">Tasks for {campaignName}</div>,
}));

import CampaignsPanel from "./CampaignsPanel";

const CAMPAIGN: Campaign = {
  id: "c1",
  name: "Q3 Ward Verification",
  period_start: "2026-07-01",
  period_end: "2026-07-31",
  scope_department_id: "d1",
  scope_department_name: "Radiology",
  scope_location_id: null,
  scope_location_name: null,
  scope_asset_category_id: null,
  scope_asset_category_name: null,
  scope_asset_type_id: null,
  scope_asset_type_name: null,
  status: "Active",
  task_count: 40,
  completed_task_count: 12,
  open_discrepancy_count: 3,
  created_at: "2026-07-01T00:00:00Z",
};

describe("CampaignsPanel", () => {
  it("shows a loading state before data arrives", () => {
    listCampaignsMock.mockReturnValue(new Promise(() => {})); // never resolves
    render(<CampaignsPanel />);
    expect(screen.getByText("Loading campaigns…")).toBeInTheDocument();
  });

  it("shows an empty state when there are no campaigns", async () => {
    listCampaignsMock.mockResolvedValue([]);
    render(<CampaignsPanel />);
    expect(await screen.findByText("No verification campaigns yet.")).toBeInTheDocument();
  });

  it("renders a campaign row with its scope label and discrepancy count", async () => {
    listCampaignsMock.mockResolvedValue([CAMPAIGN]);
    render(<CampaignsPanel />);

    expect(await screen.findByText("Q3 Ward Verification")).toBeInTheDocument();
    expect(screen.getByText("Radiology")).toBeInTheDocument();
    expect(screen.getByText("12 / 40 verified")).toBeInTheDocument();
    expect(screen.getByText("3")).toBeInTheDocument();
  });

  it("shows an error notification when the campaign list fails to load", async () => {
    listCampaignsMock.mockRejectedValue(new Error("network down"));
    render(<CampaignsPanel />);
    expect(await screen.findByText("Could not load verification campaigns")).toBeInTheDocument();
  });

  it("opens the create-campaign modal from the section header button", async () => {
    listCampaignsMock.mockResolvedValue([]);
    const user = userEvent.setup();
    render(<CampaignsPanel />);

    await waitFor(() => expect(screen.getByText("No verification campaigns yet.")).toBeInTheDocument());
    await user.click(screen.getByRole("button", { name: "New campaign" }));

    expect(screen.getByRole("dialog", { name: "create campaign" })).toBeInTheDocument();
  });

  it("opens the campaign report modal from a row's 'View report' button", async () => {
    listCampaignsMock.mockResolvedValue([CAMPAIGN]);
    const user = userEvent.setup();
    render(<CampaignsPanel />);

    await user.click(await screen.findByRole("button", { name: "View report" }));

    expect(screen.getByText("Report for Q3 Ward Verification")).toBeInTheDocument();
  });

  it("opens the campaign tasks modal from a row's 'View tasks' button", async () => {
    listCampaignsMock.mockResolvedValue([CAMPAIGN]);
    const user = userEvent.setup();
    render(<CampaignsPanel />);

    await user.click(await screen.findByRole("button", { name: "View tasks" }));

    expect(screen.getByText("Tasks for Q3 Ward Verification")).toBeInTheDocument();
  });
});
