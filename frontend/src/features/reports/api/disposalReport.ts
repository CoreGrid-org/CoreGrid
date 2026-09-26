import { listDisposals } from "@/features/transfers/services/disposals";
import type { DisposalQueryParameters, DisposalResponse } from "@/features/transfers/types";
import { fetchAllPages } from "../lib/fetchAllPages";

// Every disposal record matching the filters, for the report's stats and export.
export function getDisposalReportRecords(
  params: Omit<DisposalQueryParameters, "page" | "pageSize">,
  accessToken: string,
): Promise<DisposalResponse[]> {
  return fetchAllPages((page, pageSize) => listDisposals({ ...params, page, pageSize }, accessToken));
}
