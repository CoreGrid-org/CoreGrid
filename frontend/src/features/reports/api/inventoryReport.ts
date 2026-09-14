import { listAssets } from "@/features/assets/api/assets";
import type { Asset, AssetQueryParameters } from "@/features/assets/types/asset";

const PAGE_SIZE = 100;

export async function getInventoryAssets(
  params: Omit<AssetQueryParameters, "page" | "pageSize">,
  accessToken: string,
): Promise<Asset[]> {
  const firstPage = await listAssets({ ...params, page: 1, pageSize: PAGE_SIZE, sortBy: "name", sortDirection: "asc" }, accessToken);
  const assets = [...firstPage.items];

  for (let page = 2; page <= firstPage.total_pages; page += 1) {
    const result = await listAssets({ ...params, page, pageSize: PAGE_SIZE, sortBy: "name", sortDirection: "asc" }, accessToken);
    assets.push(...result.items);
  }

  return assets;
}