import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

// Shell-level test only: does ReportsPage wire each tab to the right
// panel? InventoryReportPanel (Component A) and AuditReportPanel
// (Component D, its own full test suite already) are stubbed so this
// doesn't depend on their internals or data-fetching.
vi.mock("../components/InventoryReportPanel", () => ({ default: () => <div>Inventory panel content</div> }));
vi.mock("../components/AuditReportPanel", () => ({ default: () => <div>Audit panel content</div> }));

import ReportsPage from "./ReportsPage";

describe("ReportsPage", () => {
  it("has all four report tabs and shows the Inventory panel by default", () => {
    render(<ReportsPage />);

    expect(screen.getByRole("tab", { name: "Asset Inventory" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Maintenance" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Disposal" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Audit" })).toBeInTheDocument();
    expect(screen.getByText("Inventory panel content")).toBeInTheDocument();
  });

  it("switches to the mock Maintenance tab, and to the real Audit panel", async () => {
    const user = userEvent.setup();
    render(<ReportsPage />);

    await user.click(screen.getByRole("tab", { name: "Maintenance" }));
    // Maintenance and Disposal share the same generic MockNotice title, so
    // this checks the mock report's own description text instead, which is
    // unique per tab (see reports/data/mockReports.ts).
    expect(screen.getByText(/Cost and repair-count trends across the fleet/)).toBeInTheDocument();

    await user.click(screen.getByRole("tab", { name: "Audit" }));
    expect(screen.getByText("Audit panel content")).toBeInTheDocument();
  });
});
