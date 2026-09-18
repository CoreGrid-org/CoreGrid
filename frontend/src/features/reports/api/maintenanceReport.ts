import { listMaintenanceRecords } from "@/features/maintenance/api/maintenance";
import type { MaintenanceQueryParameters, MaintenanceRecord } from "@/features/maintenance/types/maintenance";

const PAGE_SIZE = 100;

// FR-084 (Reports > Maintenance): same "fetch every page client-side and
// aggregate in the browser" pattern as Asset Inventory's getInventoryAssets
// — GET /api/maintenance already does server-side filter/sort/pagination
// (FR-042), so no dedicated report backend endpoint is needed.
export async function getMaintenanceReportRecords(
  params: Omit<MaintenanceQueryParameters, "page" | "pageSize">,
  accessToken: string,
): Promise<MaintenanceRecord[]> {
  const firstPage = await listMaintenanceRecords(
    { ...params, page: 1, pageSize: PAGE_SIZE, sortBy: "createdat", sortDirection: "desc" },
    accessToken,
  );
  const records = [...firstPage.items];

  for (let page = 2; page <= firstPage.total_pages; page += 1) {
    const result = await listMaintenanceRecords(
      { ...params, page, pageSize: PAGE_SIZE, sortBy: "createdat", sortDirection: "desc" },
      accessToken,
    );
    records.push(...result.items);
  }

  return records;
}
