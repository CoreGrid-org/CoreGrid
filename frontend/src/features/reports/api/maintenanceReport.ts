import { listMaintenanceRecords } from "@/features/maintenance/api/maintenance";
import type { MaintenanceQueryParameters, MaintenanceRecord } from "@/features/maintenance/types/maintenance";
import { fetchAllPages } from "../lib/fetchAllPages";

// Every maintenance record matching the filters, newest first, for the report's stats and export.
export function getMaintenanceReportRecords(
  params: Omit<MaintenanceQueryParameters, "page" | "pageSize">,
  accessToken: string,
): Promise<MaintenanceRecord[]> {
  return fetchAllPages((page, pageSize) =>
    listMaintenanceRecords({ ...params, page, pageSize, sortBy: "createdat", sortDirection: "desc" }, accessToken),
  );
}
