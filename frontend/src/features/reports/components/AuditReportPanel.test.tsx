import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { thunderIDTestDouble } from "@/test/mocks/thunderid";
import type { AuditReport, AuditReportDiscrepancyRow } from "../api/auditReport";

vi.mock("@thunderid/react", () => ({
  useThunderID: () => thunderIDTestDouble(),
}));

const { getAuditReportMock } = vi.hoisted(() => ({ getAuditReportMock: vi.fn() }));
vi.mock("../api/auditReport", () => ({
  getAuditReport: getAuditReportMock,
  downloadAuditReportExport: vi.fn(),
}));
vi.mock("@/features/assets/hooks/useAssets", () => ({
  useDepartments: () => ({ data: [] }),
  useAssetCategories: () => ({ data: [] }),
}));

import AuditReportPanel from "./AuditReportPanel";

function discrepancyRow(i: number): AuditReportDiscrepancyRow {
  return {
    asset_code: `MOHSL-TEST-${String(i).padStart(4, "0")}`,
    asset_name: `Test Asset ${i}`,
    department_name: "Radiology",
    classification: "ConditionMismatch",
    status: i % 2 === 0 ? "Resolved" : "Open",
    raised_at: "2026-09-01T00:00:00Z",
    resolved_at: i % 2 === 0 ? "2026-09-05T00:00:00Z" : null,
  };
}

const BASE_REPORT: AuditReport = {
  from: null,
  to: null,
  campaigns_in_period: 2,
  assets_in_scope: 40,
  assets_verified: 30,
  open_discrepancies: 1,
  by_classification: [{ classification: "ConditionMismatch", raised: 2, resolved: 1 }],
  // discrepancies is just this page's rows now (server-side pagination);
  // discrepancies_total_count is the true count across every page.
  discrepancies: [discrepancyRow(1), discrepancyRow(2)],
  discrepancies_total_count: 2,
  page: 1,
  page_size: 25,
  total_pages: 1,
  generated_at: "2026-09-14T00:00:00Z",
};

describe("AuditReportPanel", () => {
  it("shows a loading state before the report arrives", () => {
    getAuditReportMock.mockReturnValue(new Promise(() => {}));
    render(<AuditReportPanel />);
    expect(screen.getByText("Loading…")).toBeInTheDocument();
  });

  it("renders the summary stats, classification breakdown and discrepancy list", async () => {
    getAuditReportMock.mockResolvedValue(BASE_REPORT);
    render(<AuditReportPanel />);

    expect(await screen.findByText("40")).toBeInTheDocument(); // assets in scope
    expect(screen.getByText("Showing 2 of 2 filtered discrepancies")).toBeInTheDocument();
    expect(screen.getByText("MOHSL-TEST-0001")).toBeInTheDocument();
    expect(screen.getByText("MOHSL-TEST-0002")).toBeInTheDocument();
  });

  it("does not crash when the API response is missing discrepancies/by_classification (defensive null-guard)", async () => {
    const { discrepancies: _d, by_classification: _c, ...incomplete } = BASE_REPORT;
    getAuditReportMock.mockResolvedValue({ ...incomplete, discrepancies_total_count: 0 } as unknown as AuditReport);
    render(<AuditReportPanel />);

    expect(await screen.findByText("Showing 0 of 0 filtered discrepancies")).toBeInTheDocument();
    // Both the classification-summary table and the discrepancy-list table
    // fall back to the same empty-state copy when their array is empty.
    expect(screen.getAllByText("No discrepancies match these filters.")).toHaveLength(2);
  });

  it("shows an error notification when the report fails to load", async () => {
    getAuditReportMock.mockRejectedValue(new Error("network down"));
    render(<AuditReportPanel />);
    expect(await screen.findByText("Could not load the report")).toBeInTheDocument();
  });

  it("shows the pagination control driven by the server-reported total, not just this page's rows", async () => {
    // Server-side pagination: the mock returns only this page's 10 rows,
    // but reports a true total of 12 across all pages.
    const firstPage = Array.from({ length: 10 }, (_, i) => discrepancyRow(i + 1));
    getAuditReportMock.mockResolvedValue({
      ...BASE_REPORT, discrepancies: firstPage, discrepancies_total_count: 12, total_pages: 2,
    });
    render(<AuditReportPanel />);

    expect(await screen.findByText("Showing 10 of 12 filtered discrepancies")).toBeInTheDocument();
    expect(screen.getByText(/of 12 items/i)).toBeInTheDocument();
  });
});
