import hashlib
import os
import time
from uuid import UUID

import httpx

from app.contracts import AssetSummary


class AssetSummaryToolError(RuntimeError):
    pass


class AssetSummaryHttpError(AssetSummaryToolError):
    pass


async def get_asset_summary(asset_id: UUID, organization_id: UUID) -> AssetSummary:
    """Read the only system tool allowed to the Planner Agent."""
    base_url = os.environ.get("COREGRID_API_BASE_URL", "http://localhost:5083").rstrip("/")
    token = os.environ.get("AGENT_SERVICE_TOKEN", "")
    headers = {"Authorization": f"Bearer {token}"} if token else {}
    params = {"organizationId": str(organization_id)}
    endpoint = f"{base_url}/api/agent-tools/assets/{asset_id}/summary"
    last_error: Exception | None = None

    for attempt in range(3):
        started = time.monotonic()
        try:
            async with httpx.AsyncClient(timeout=15.0) as client:
                response = await client.get(endpoint, params=params, headers=headers)
            if response.status_code == 404:
                raise AssetSummaryToolError("The asset was not found in the initiating organisation.")
            if response.status_code in (401, 403):
                raise AssetSummaryHttpError(
                    f"CoreGrid rejected get_asset_summary with HTTP {response.status_code}. "
                    "Set AGENT_SERVICE_TOKEN to a valid CoreGrid bearer token."
                )
            if not response.is_success:
                raise AssetSummaryHttpError(
                    f"CoreGrid get_asset_summary returned HTTP {response.status_code}."
                )
            response.raise_for_status()
            summary = AssetSummary.model_validate(response.json())
            print(f"tool=get_asset_summary outcome=success duration_ms={int((time.monotonic() - started) * 1000)} retry={attempt}")
            return summary
        except (httpx.HTTPError, ValueError, AssetSummaryToolError) as error:
            last_error = error
            print(
                f"tool=get_asset_summary outcome=error type={type(error).__name__} "
                f"message={error} duration_ms={int((time.monotonic() - started) * 1000)} retry={attempt}"
            )
            if isinstance(error, AssetSummaryToolError):
                break
            if attempt < 2:
                await __import__("asyncio").sleep(2**attempt)

    raise AssetSummaryToolError("get_asset_summary failed after two retries.") from last_error


def input_hash(asset_id: UUID, organization_id: UUID) -> str:
    value = f"{asset_id}:{organization_id}".encode("utf-8")
    return hashlib.sha256(value).hexdigest()
