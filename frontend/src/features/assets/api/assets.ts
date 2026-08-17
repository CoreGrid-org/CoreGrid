import type {
  Asset,
  AssetCategory,
  AssetAttributeDefinition,
  AssetDetail,
  AssetQueryParameters,
  AssetType,
  CreateAssetAttributeDefinitionRequest,
  CreateAssetCategoryRequest,
  CreateAssetRequest,
  CreateAssetTypeRequest,
  Department,
  Location,
  OrganizationCode,
  PagedResult,
  UpdateAssetAttributeDefinitionRequest,
  UpdateAssetCategoryRequest,
  UpdateAssetConditionRequest,
  UpdateAssetRequest,
  UpdateAssetTypeRequest,
} from "../types/asset";

const API_URL = import.meta.env.VITE_API_URL;

async function handle<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || fallback);
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

function buildQuery(params: AssetQueryParameters): string {
  const search = new URLSearchParams();
  if (params.search) search.set("search", params.search);
  if (params.assetTypeId) search.set("assetTypeId", params.assetTypeId);
  if (params.departmentId) search.set("departmentId", params.departmentId);
  if (params.locationId) search.set("locationId", params.locationId);
  if (params.status) search.set("status", params.status);
  if (params.condition) search.set("condition", params.condition);
  if (params.sortBy) search.set("sortBy", params.sortBy);
  if (params.sortDirection) search.set("sortDirection", params.sortDirection);
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  const query = search.toString();
  return query ? `?${query}` : "";
}

// backend/Features/Assets/Controllers/AssetsController.cs — [Authorize], org is
// derived server-side from the caller's ThunderID access token.
export async function listAssets(
  params: AssetQueryParameters,
  accessToken: string,
): Promise<PagedResult<Asset>> {
  const response = await fetch(`${API_URL}/assets${buildQuery(params)}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load assets.");
}

export async function getAsset(id: string, accessToken: string): Promise<AssetDetail> {
  const response = await fetch(`${API_URL}/assets/${id}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load asset.");
}

export async function getAssetByQrCode(code: string, accessToken: string): Promise<AssetDetail> {
  const response = await fetch(`${API_URL}/assets/qr/${encodeURIComponent(code)}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not find an asset for that code.");
}

export async function getOrganizationCode(accessToken: string): Promise<OrganizationCode> {
  const response = await fetch(`${API_URL}/assets/organization-code`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load organization code.");
}

export async function createAsset(
  payload: CreateAssetRequest,
  accessToken: string,
): Promise<AssetDetail> {
  const response = await fetch(`${API_URL}/assets`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create asset.");
}

export async function updateAsset(
  id: string,
  payload: UpdateAssetRequest,
  accessToken: string,
): Promise<AssetDetail> {
  const response = await fetch(`${API_URL}/assets/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update asset.");
}

export async function updateAssetCondition(
  id: string,
  payload: UpdateAssetConditionRequest,
  accessToken: string,
): Promise<void> {
  const response = await fetch(`${API_URL}/assets/${id}/condition`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update asset condition.");
}

export async function listAssetCategories(accessToken: string): Promise<AssetCategory[]> {
  const response = await fetch(`${API_URL}/asset-categories`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load asset categories.");
}

export async function createAssetCategory(
  payload: CreateAssetCategoryRequest,
  accessToken: string,
): Promise<AssetCategory> {
  const response = await fetch(`${API_URL}/asset-categories`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create asset category.");
}

export async function updateAssetCategory(
  id: string,
  payload: UpdateAssetCategoryRequest,
  accessToken: string,
): Promise<AssetCategory> {
  const response = await fetch(`${API_URL}/asset-categories/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update asset category.");
}

// DELETE /api/asset-categories/{id} — 204 (hard-deleted, gone) or 200 with
// the now-deactivated category (still referenced by an AssetType).
export async function deleteAssetCategory(
  id: string,
  accessToken: string,
): Promise<AssetCategory | undefined> {
  const response = await fetch(`${API_URL}/asset-categories/${id}`, {
    method: "DELETE",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not delete asset category.");
}

export async function activateAssetCategory(
  id: string,
  accessToken: string,
): Promise<AssetCategory> {
  const response = await fetch(`${API_URL}/asset-categories/${id}/activate`, {
    method: "PATCH",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not reactivate asset category.");
}

export async function listAssetTypes(accessToken: string): Promise<AssetType[]> {
  const response = await fetch(`${API_URL}/asset-types`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load asset types.");
}

export async function createAssetType(
  payload: CreateAssetTypeRequest,
  accessToken: string,
): Promise<AssetType> {
  const response = await fetch(`${API_URL}/asset-types`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create asset type.");
}

export async function updateAssetType(
  id: string,
  payload: UpdateAssetTypeRequest,
  accessToken: string,
): Promise<AssetType> {
  const response = await fetch(`${API_URL}/asset-types/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update asset type.");
}

// DELETE /api/asset-types/{id} — 204 (hard-deleted, gone) or 200 with the
// now-deactivated type (still referenced by an Asset).
export async function deleteAssetType(
  id: string,
  accessToken: string,
): Promise<AssetType | undefined> {
  const response = await fetch(`${API_URL}/asset-types/${id}`, {
    method: "DELETE",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not delete asset type.");
}

export async function activateAssetType(
  id: string,
  accessToken: string,
): Promise<AssetType> {
  const response = await fetch(`${API_URL}/asset-types/${id}/activate`, {
    method: "PATCH",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not reactivate asset type.");
}

export async function getAssetTypeAttributes(
  assetTypeId: string,
  accessToken: string,
): Promise<AssetAttributeDefinition[]> {
  const response = await fetch(`${API_URL}/asset-types/${assetTypeId}/attributes`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load asset type attributes.");
}

export async function listDepartments(accessToken: string): Promise<Department[]> {
  const response = await fetch(`${API_URL}/departments`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load departments.");
}

export async function listLocations(
  departmentId: string | undefined,
  accessToken: string,
): Promise<Location[]> {
  const query = departmentId ? `?departmentId=${encodeURIComponent(departmentId)}` : "";
  const response = await fetch(`${API_URL}/locations${query}`, {
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not load locations.");
}

export async function createAssetAttributeDefinition(
  assetTypeId: string,
  payload: CreateAssetAttributeDefinitionRequest,
  accessToken: string,
): Promise<AssetAttributeDefinition> {
  const response = await fetch(`${API_URL}/asset-types/${assetTypeId}/attributes`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not create attribute.");
}

export async function updateAssetAttributeDefinition(
  assetTypeId: string,
  attributeId: string,
  payload: UpdateAssetAttributeDefinitionRequest,
  accessToken: string,
): Promise<AssetAttributeDefinition> {
  const response = await fetch(`${API_URL}/asset-types/${assetTypeId}/attributes/${attributeId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not update attribute.");
}

// DELETE /api/asset-types/{id}/attributes/{attributeId} — 204 (hard-deleted,
// gone) or 200 with the now-deactivated definition (still has stored values).
export async function deleteAssetAttributeDefinition(
  assetTypeId: string,
  attributeId: string,
  accessToken: string,
): Promise<AssetAttributeDefinition | undefined> {
  const response = await fetch(`${API_URL}/asset-types/${assetTypeId}/attributes/${attributeId}`, {
    method: "DELETE",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not delete attribute.");
}

export async function activateAssetAttributeDefinition(
  assetTypeId: string,
  attributeId: string,
  accessToken: string,
): Promise<AssetAttributeDefinition> {
  const response = await fetch(
    `${API_URL}/asset-types/${assetTypeId}/attributes/${attributeId}/activate`,
    { method: "PATCH", headers: authHeaders(accessToken) },
  );
  return handle(response, "Could not reactivate attribute.");
}
