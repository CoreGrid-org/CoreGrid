import type {
  CondemnAssetRequest,
  CondemnAssetResponse,
  DisposalQueryParameters,
  DisposalResponse,
  PagedResult,
  RequestDisposalRevisionRequest,
  SubmitDisposalRequest,
} from "../types";

const API_URL = import.meta.env.VITE_API_URL;

async function handle<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) {
    let detail = "";
    try {
      const errJson = await response.json();
      if (errJson.preconditions?.checks) {
        const failedChecks = errJson.preconditions.checks
          .filter((c: { passed: boolean }) => !c.passed)
          .map((c: { code: string; failure_reason: string; description: string }) =>
            `${c.code}: ${c.failure_reason || c.description}`
          )
          .join("; ");
        detail = `${errJson.message || fallback} (${failedChecks})`;
      } else {
        detail = errJson.message || errJson.title || JSON.stringify(errJson);
      }
    } catch {
      detail = await response.text().catch(() => "");
    }
    throw new Error(detail || fallback);
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}

// POST /api/assets/{id}/condemn — FR-049: Condemn asset
export async function condemnAsset(
  assetId: string,
  payload: CondemnAssetRequest,
  accessToken: string
): Promise<CondemnAssetResponse> {
  const response = await fetch(`${API_URL}/assets/${assetId}/condemn`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
  return handle<CondemnAssetResponse>(response, `Could not condemn asset ${assetId}.`);
}

// POST /api/disposals — FR-050: Submit disposal request
export async function submitDisposal(
  payload: SubmitDisposalRequest,
  accessToken: string
): Promise<DisposalResponse> {
  const response = await fetch(`${API_URL}/disposals`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
  return handle<DisposalResponse>(response, "Could not submit disposal request.");
}

// GET /api/disposals — List disposal requests with optional filters and pagination
export async function listDisposals(
  params?: DisposalQueryParameters,
  accessToken?: string
): Promise<PagedResult<DisposalResponse>> {
  const search = new URLSearchParams();
  if (params?.status) search.set("status", params.status);
  if (params?.method) search.set("method", params.method);
  if (params?.page) search.set("page", String(params.page));
  if (params?.pageSize) search.set("pageSize", String(params.pageSize));
  const query = search.toString() ? `?${search.toString()}` : "";

  const response = await fetch(`${API_URL}/disposals${query}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<PagedResult<DisposalResponse>>(response, "Could not load disposals.");
}

// GET /api/disposals/{id} — Get disposal request with live precondition evaluation (FR-051/052)
export async function getDisposalById(
  id: string,
  accessToken: string
): Promise<DisposalResponse> {
  const response = await fetch(`${API_URL}/disposals/${id}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<DisposalResponse>(response, `Could not load disposal ${id}.`);
}

// POST /api/disposals/{id}/approve — FR-051/054: Approve disposal request (Administrator)
export async function approveDisposal(
  id: string,
  accessToken: string
): Promise<DisposalResponse> {
  const response = await fetch(`${API_URL}/disposals/${id}/approve`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return handle<DisposalResponse>(response, `Could not approve disposal ${id}.`);
}

// POST /api/disposals/{id}/request-revision — FR-053: Request revision on disposal request
export async function requestDisposalRevision(
  id: string,
  payload: RequestDisposalRevisionRequest,
  accessToken: string
): Promise<DisposalResponse> {
  const response = await fetch(`${API_URL}/disposals/${id}/request-revision`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
  return handle<DisposalResponse>(
    response,
    `Could not request revision on disposal ${id}.`
  );
}
