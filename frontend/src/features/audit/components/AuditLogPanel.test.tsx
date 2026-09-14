import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { thunderIDTestDouble } from "@/test/mocks/thunderid";
import type { AuditLogEntry, PagedResult } from "../api/auditLog";

vi.mock("@thunderid/react", () => ({
  useThunderID: () => thunderIDTestDouble(),
}));

const { listAuditLogMock } = vi.hoisted(() => ({ listAuditLogMock: vi.fn() }));
vi.mock("../api/auditLog", () => ({
  listAuditLog: listAuditLogMock,
}));

import AuditLogPanel from "./AuditLogPanel";

const ENTRY: AuditLogEntry = {
  id: "e1",
  actor_user_id: "u1",
  actor_email: "admin@mohsl.gov.lk",
  entity_type: "Asset",
  entity_id: "12345678-aaaa-bbbb-cccc-000000000000",
  operation: "Update",
  changes: null,
  correlation_id: "87654321-aaaa-bbbb-cccc-000000000000",
  created_at: "2026-09-14T10:00:00Z",
};

function pagedResult(items: AuditLogEntry[], totalCount = items.length): PagedResult<AuditLogEntry> {
  return { items, total_count: totalCount, page: 1, page_size: 25, total_pages: 1 };
}

describe("AuditLogPanel", () => {
  it("shows an empty state when no entries match the filters", async () => {
    listAuditLogMock.mockResolvedValue(pagedResult([]));
    render(<AuditLogPanel />);
    expect(await screen.findByText("No audit log entries match these filters.")).toBeInTheDocument();
  });

  it("renders an entry row with actor, truncated entity/correlation ids, and an operation tag", async () => {
    listAuditLogMock.mockResolvedValue(pagedResult([ENTRY]));
    render(<AuditLogPanel />);

    expect(await screen.findByText("admin@mohsl.gov.lk")).toBeInTheDocument();
    expect(screen.getByText("Asset (12345678)")).toBeInTheDocument();
    expect(screen.getByText("87654321")).toBeInTheDocument();
    expect(screen.getByText("Update")).toBeInTheDocument();
  });

  it("falls back to 'System' when there is no actor", async () => {
    listAuditLogMock.mockResolvedValue(pagedResult([{ ...ENTRY, actor_email: null }]));
    render(<AuditLogPanel />);
    expect(await screen.findByText("System")).toBeInTheDocument();
  });

  it("shows an error notification when the log fails to load", async () => {
    listAuditLogMock.mockRejectedValue(new Error("network down"));
    render(<AuditLogPanel />);
    expect(await screen.findByText("Could not load the audit log")).toBeInTheDocument();
  });

  it("does not render pagination when there are no results", async () => {
    listAuditLogMock.mockResolvedValue(pagedResult([], 0));
    render(<AuditLogPanel />);
    await screen.findByText("No audit log entries match these filters.");
    expect(screen.queryByLabelText(/items per page/i)).not.toBeInTheDocument();
  });
});
