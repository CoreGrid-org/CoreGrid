import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  approveMaintenance,
  cancelMaintenance,
  completeMaintenance,
  listMaintenanceRecords,
  reportFault,
  startMaintenance,
  uploadMaintenancePhoto,
} from "./maintenance";

const fetchMock = vi.fn();

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });

const lastCall = () => {
  const [url, init] = fetchMock.mock.calls.at(-1)!;
  return { url: String(url), init: (init ?? {}) as RequestInit };
};

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("maintenance api", () => {
  it("lists records with only the filters that are set, in the backend's query names", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ items: [], total_count: 0, page: 2, page_size: 10, total_pages: 0 }));

    await listMaintenanceRecords(
      { status: "IN_PROGRESS", priority: "HIGH", dateFrom: "2026-01-01", sortBy: "createdat", sortDirection: "desc", page: 2, pageSize: 10 },
      "token-1",
    );

    const { url, init } = lastCall();
    const query = new URL(url, "http://x").searchParams;
    expect(url).toContain("/maintenance?");
    expect(Object.fromEntries(query)).toEqual({
      status: "IN_PROGRESS",
      priority: "HIGH",
      dateFrom: "2026-01-01",
      sortBy: "createdat",
      sortDirection: "desc",
      page: "2",
      pageSize: "10",
    });
    expect(init.headers).toEqual({ Authorization: "Bearer token-1" });
  });

  it("sends no query string when there are no filters", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ items: [], total_count: 0, page: 1, page_size: 20, total_pages: 0 }));

    await listMaintenanceRecords({}, "t");

    expect(lastCall().url).toMatch(/\/maintenance$/);
  });

  it("reports a fault as a JSON POST to /maintenance/faults", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ id: "rec-1" }, 201));
    const payload = { asset_id: "ast-1", description: "Leaking", observed_condition: "POOR" };

    const result = await reportFault(payload, "t");

    const { url, init } = lastCall();
    expect(url).toMatch(/\/maintenance\/faults$/);
    expect(init.method).toBe("POST");
    expect(init.headers).toMatchObject({ "Content-Type": "application/json", Authorization: "Bearer t" });
    expect(JSON.parse(String(init.body))).toEqual(payload);
    expect(result).toEqual({ id: "rec-1" });
  });

  it("posts each lifecycle step to its own endpoint", async () => {
    fetchMock.mockImplementation(() => Promise.resolve(jsonResponse({ id: "rec-9" })));

    await approveMaintenance("rec-9", { estimated_cost: 500, assignee_id: "u-1" }, "t");
    expect(lastCall().url).toMatch(/\/maintenance\/rec-9\/approve$/);
    expect(JSON.parse(String(lastCall().init.body))).toEqual({ estimated_cost: 500, assignee_id: "u-1" });

    await startMaintenance("rec-9", "t");
    expect(lastCall().url).toMatch(/\/maintenance\/rec-9\/start$/);
    expect(lastCall().init.body).toBeUndefined();

    await completeMaintenance(
      "rec-9",
      { actual_cost: 450, work_performed: "Replaced the pump seal", completion_date: "2026-10-01", resulting_condition: "GOOD" },
      "t",
    );
    expect(lastCall().url).toMatch(/\/maintenance\/rec-9\/complete$/);

    await cancelMaintenance("rec-9", { reason: "Duplicate" }, "t");
    expect(lastCall().url).toMatch(/\/maintenance\/rec-9\/cancel$/);
    expect(JSON.parse(String(lastCall().init.body))).toEqual({ reason: "Duplicate" });

    for (const [, init] of fetchMock.mock.calls) {
      expect((init as RequestInit).method).toBe("POST");
    }
  });

  it("uploads a photo as multipart form data and returns the stored key", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ url: "maintenance/org/abc/photo.jpg" }));
    const file = new File(["img"], "pump.jpg", { type: "image/jpeg" });

    const key = await uploadMaintenancePhoto(file, "t");

    const { url, init } = lastCall();
    expect(url).toMatch(/\/maintenance\/photos$/);
    expect(init.body).toBeInstanceOf(FormData);
    expect((init.body as FormData).get("photo")).toBe(file);
    // The browser must set the multipart boundary itself.
    expect(init.headers).toEqual({ Authorization: "Bearer t" });
    expect(key).toBe("maintenance/org/abc/photo.jpg");
  });

  it("throws the server's error text when a request fails", async () => {
    fetchMock.mockResolvedValue(new Response("Only an IN_PROGRESS maintenance record can be completed.", { status: 409 }));

    await expect(
      completeMaintenance("rec-1", { actual_cost: 1, work_performed: "x".repeat(10), completion_date: "2026-10-01", resulting_condition: "GOOD" }, "t"),
    ).rejects.toThrow("Only an IN_PROGRESS maintenance record can be completed.");
  });

  it("falls back to a friendly message when the error has no body", async () => {
    fetchMock.mockResolvedValue(new Response("", { status: 500 }));

    await expect(startMaintenance("rec-1", "t")).rejects.toThrow("Could not start maintenance.");
  });
});
