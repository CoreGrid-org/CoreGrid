import { listAssets } from "@/features/assets/api/assets";
import type { Asset, AssetQueryParameters } from "@/features/assets/types/asset";
import { fetchAllPages } from "../lib/fetchAllPages";

// Every asset matching the filters, by name, for the report's stats and export.
export function getInventoryAssets(
  params: Omit<AssetQueryParameters, "page" | "pageSize">,
  accessToken: string,
): Promise<Asset[]> {
  return fetchAllPages((page, pageSize) => listAssets({ ...params, page, pageSize, sortBy: "name", sortDirection: "asc" }, accessToken));
}
