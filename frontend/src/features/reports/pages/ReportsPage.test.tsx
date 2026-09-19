import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { thunderIDTestDouble } from "@/test/mocks/thunderid";
import type { MeResponse } from "@/features/auth/services/me";

// Shell-level test only: does ReportsPage wire each tab to the right
// panel, and gate the Audit tab to the roles the backend's own
// AuditReportController actually allows (Auditor/Administrator)?
// InventoryReportPanel (Component A), MaintenanceReportPanel (Component B),
// DisposalReportPanel (Component C) and AuditReportPanel (Component D,
// its own full test suite already) are stubbed so this doesn't depend on
// their internals or data-fetching.
vi.mock("../components/InventoryReportPanel", () => ({ default: () => <div>Inventory panel content</div> }));
vi.mock("../components/MaintenanceReportPanel", () => ({ default: () => <div>Maintenance panel content</div> }));
vi.mock("../components/DisposalReportPanel", () => ({ default: () => <div>Disposal panel content</div> }));
vi.mock("../components/AuditReportPanel", () => ({ default: () => <div>Audit panel content</div> }));

vi.mock("@thunderid/react", () => ({
  useThunderID: () => thunderIDTestDouble(),
}));

const { getMeMock } = vi.hoisted(() => ({ getMeMock: vi.fn() }));
vi.mock("@/features/auth/services/me", () => ({
  getMe: getMeMock,
}));

import ReportsPage from "./ReportsPage";

const ADMIN: MeResponse = { id: "u1", email: "admin@mohsl.gov.lk", given_name: "A", family_name: "B", role: "Administrator", is_active: true };
const OFFICER: MeResponse = { ...ADMIN, id: "u2", email: "officer@mohsl.gov.lk", role: "InventoryOfficer" };

describe("ReportsPage", () => {
  it("shows all four report tabs to an Administrator, Inventory panel by default", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    render(<ReportsPage />);

    expect(screen.getByRole("tab", { name: "Asset Inventory" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Maintenance" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Disposal" })).toBeInTheDocument();
    expect(await screen.findByRole("tab", { name: "Audit" })).toBeInTheDocument();
    expect(screen.getByText("Inventory panel content")).toBeInTheDocument();
  });

  it("hides the Audit tab from an Inventory Officer — its backend endpoint is Auditor/Administrator-only", async () => {
    getMeMock.mockResolvedValue(OFFICER);
    render(<ReportsPage />);

    expect(await screen.findByRole("tab", { name: "Asset Inventory" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Maintenance" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "Disposal" })).toBeInTheDocument();
    expect(screen.queryByRole("tab", { name: "Audit" })).not.toBeInTheDocument();
    expect(screen.getByText(/Inventory, maintenance and disposal reports\./)).toBeInTheDocument();
  });

  it("switches to the real Maintenance panel, and to the real Audit panel for an Administrator", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    const user = userEvent.setup();
    render(<ReportsPage />);

    await user.click(screen.getByRole("tab", { name: "Maintenance" }));
    expect(screen.getByText("Maintenance panel content")).toBeInTheDocument();

    await user.click(await screen.findByRole("tab", { name: "Audit" }));
    expect(screen.getByText("Audit panel content")).toBeInTheDocument();
  });

  it("switches to the real Disposal panel", async () => {
    getMeMock.mockResolvedValue(ADMIN);
    const user = userEvent.setup();
    render(<ReportsPage />);

    await user.click(screen.getByRole("tab", { name: "Disposal" }));
    expect(screen.getByText("Disposal panel content")).toBeInTheDocument();
  });
});
