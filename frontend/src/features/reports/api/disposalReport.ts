import { listDisposals } from "@/features/transfers/services/disposals";
import type { DisposalQueryParameters, DisposalResponse } from "@/features/transfers/types";

const PAGE_SIZE = 100;

// FR-084 (Reports > Disposal): same "fetch every page client-side and
// aggregate in the browser" pattern as Maintenance's getMaintenanceReportRecords
// and Asset Inventory's getInventoryAssets — GET /api/disposals already does
// server-side filter/pagination, so no dedicated report backend endpoint is
// needed.
export async function getDisposalReportRecords(
  params: Omit<DisposalQueryParameters, "page" | "pageSize">,
  accessToken: string,
): Promise<DisposalResponse[]> {
  const firstPage = await listDisposals({ ...params, page: 1, pageSize: PAGE_SIZE }, accessToken);
  const records = [...firstPage.items];

  for (let page = 2; page <= firstPage.total_pages; page += 1) {
    const result = await listDisposals({ ...params, page, pageSize: PAGE_SIZE }, accessToken);
    records.push(...result.items);
  }

  return records;
}
