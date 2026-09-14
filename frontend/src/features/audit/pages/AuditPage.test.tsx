import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

// Shell-level test: does AuditPage wire its three tabs to the right panel?
// Each panel has its own full test suite (CampaignsPanel.test.tsx etc.) —
// stubbed here so this test doesn't re-verify their internals or need
// their data-fetching mocks.
import { vi } from "vitest";
vi.mock("../components/CampaignsPanel", () => ({ default: () => <div>Campaigns panel content</div> }));
vi.mock("../components/DiscrepanciesPanel", () => ({ default: () => <div>Discrepancies panel content</div> }));
vi.mock("../components/AuditLogPanel", () => ({ default: () => <div>Audit log panel content</div> }));

import AuditPage from "./AuditPage";

describe("AuditPage", () => {
  it("shows the Campaigns panel by default and switches panels with the tabs", async () => {
    const user = userEvent.setup();
    render(<AuditPage />);

    expect(screen.getByText("Campaigns panel content")).toBeInTheDocument();

    await user.click(screen.getByRole("tab", { name: "Discrepancies" }));
    expect(screen.getByText("Discrepancies panel content")).toBeInTheDocument();

    await user.click(screen.getByRole("tab", { name: "Audit Log" }));
    expect(screen.getByText("Audit log panel content")).toBeInTheDocument();
  });
});
