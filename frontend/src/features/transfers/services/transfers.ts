import type {
  InitiateTransferRequest,
  PagedResult,
  TransferQueryParameters,
  TransferResponse,
} from "../types";

const API_URL = import.meta.env.VITE_API_URL;

async function handle<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) {
    let detail = "";
    try {
      const errJson = await response.json();
      detail = errJson.message || errJson.title || JSON.stringify(errJson);
    } catch {
      detail = await response.text().catch(() => "");
    }
    throw new Error(detail || fallback);
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}

// GET /api/transfers — list transfers with optional status/department filters and pagination
export async function listTransfers(
  params?: TransferQueryParameters,
  accessToken?: string
): Promise<PagedResult<TransferResponse>> {
  const search = new URLSearchParams();
  if (params?.status) search.set("status", params.status);
  if (params?.departmentId) search.set("departmentId", params.departmentId);
  if (params?.page) search.set("page", String(params.page));
  if (params?.pageSize) search.set("pageSize", String(params.pageSize));
  const query = search.toString() ? `?${search.toString()}` : "";

  const response = await fetch(`${API_URL}/transfers${query}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<PagedResult<TransferResponse>>(response, "Could not load transfers.");
}

// GET /api/transfers/{id} — get transfer by id
export async function getTransferById(
  id: string,
  accessToken: string
): Promise<TransferResponse> {
  const response = await fetch(`${API_URL}/transfers/${id}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<TransferResponse>(response, `Could not load transfer ${id}.`);
}

// GET /api/assets/{assetId}/transfers — FR-047: full transfer history for an asset
export async function getTransferHistoryForAsset(
  assetId: string,
  accessToken: string
): Promise<TransferResponse[]> {
  const response = await fetch(`${API_URL}/assets/${assetId}/transfers`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<TransferResponse[]>(
    response,
    `Could not load transfer history for asset ${assetId}.`
  );
}

// POST /api/transfers — FR-044: initiate transfer request
export async function initiateTransfer(
  payload: InitiateTransferRequest,
  accessToken: string
): Promise<TransferResponse> {
  const response = await fetch(`${API_URL}/transfers`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
  return handle<TransferResponse>(response, "Could not initiate transfer.");
}

// POST /api/transfers/{id}/approve — FR-045: approve transfer request (Administrator)
export async function approveTransfer(
  id: string,
  accessToken: string
): Promise<TransferResponse> {
  const response = await fetch(`${API_URL}/transfers/${id}/approve`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<TransferResponse>(response, `Could not approve transfer ${id}.`);
}

// POST /api/transfers/{id}/confirm-receipt — FR-046: confirm receipt of asset
export async function confirmTransferReceipt(
  id: string,
  accessToken: string
): Promise<TransferResponse> {
  const response = await fetch(`${API_URL}/transfers/${id}/confirm-receipt`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<TransferResponse>(
    response,
    `Could not confirm receipt for transfer ${id}.`
  );
}
