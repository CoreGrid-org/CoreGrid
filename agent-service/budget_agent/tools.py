from typing import Optional, Dict, Any
from uuid import UUID
from datetime import datetime, timezone, timedelta
import httpx
import os
from dotenv import load_dotenv

load_dotenv()

# Token Cache
_cached_token: Optional[str] = None
_token_expires_at: Optional[datetime] = None


class AgentToolsClient:
    """
    Client for calling CoreGrid ASP.NET Core /api/agent-tools/* endpoints
    with OAuth2 client_credentials token acquisition and caching.
    """

    def __init__(
        self,
        base_url: Optional[str] = None,
        token_url: Optional[str] = None,
        client_id: Optional[str] = None,
        client_secret: Optional[str] = None,
        resource: Optional[str] = None,
    ):
        self.base_url = (base_url or os.getenv("BACKEND_API_URL", "http://localhost:5000")).rstrip("/")
        self.token_url = token_url or os.getenv("THUNDERID_TOKEN_URL", "https://localhost:8090/oauth2/token")
        self.client_id = client_id or os.getenv("AGENT_CLIENT_ID", "")
        self.client_secret = client_secret or os.getenv("AGENT_CLIENT_SECRET", "")
        self.resource = resource or os.getenv("THUNDERID_RESOURCE", "https://localhost:8090/mcp")

    async def get_access_token(self) -> Optional[str]:
        """
        Retrieves a valid M2M token from ThunderID using client_credentials grant,
        caching it until near-expiration.
        """
        global _cached_token, _token_expires_at

        # If credentials are not configured, return None (allows mock/local test fallback)
        if not self.client_id or not self.client_secret:
            return None

        now = datetime.now(timezone.utc)
        if _cached_token and _token_expires_at and now < _token_expires_at:
            return _cached_token

        async with httpx.AsyncClient(verify=False) as client:
            response = await client.post(
                self.token_url,
                data={
                    "grant_type": "client_credentials",
                    "client_id": self.client_id,
                    "client_secret": self.client_secret,
                    "scope": "agent:tools",
                    "resource": self.resource,
                },
                headers={"Content-Type": "application/x-www-form-urlencoded"},
                timeout=10.0,
            )
            if response.is_success:
                data = response.json()
                _cached_token = data.get("access_token")
                expires_in = data.get("expires_in", 3600)
                # Refresh 60s before expiry
                _token_expires_at = now + timedelta(seconds=max(30, expires_in - 60))
                return _cached_token
            return None

    async def get_asset_financials(
        self, asset_id: UUID, organization_id: Optional[UUID] = None
    ) -> Dict[str, Any]:
        """
        Calls GET /api/agent-tools/assets/{assetId}/financials
        """
        token = await self.get_access_token()
        headers = {"Authorization": f"Bearer {token}"} if token else {}
        params = {"organizationId": str(organization_id)} if organization_id else {}

        url = f"{self.base_url}/api/agent-tools/assets/{asset_id}/financials"
        async with httpx.AsyncClient(verify=False) as client:
            response = await client.get(url, headers=headers, params=params, timeout=10.0)
            if response.status_code == 404:
                return {"error": f"Asset {asset_id} not found", "status_code": 404}
            response.raise_for_status()
            return response.json()

    async def get_department_budget_summary(
        self,
        department_id: UUID,
        fiscal_year: Optional[int] = None,
        organization_id: Optional[UUID] = None,
    ) -> Dict[str, Any]:
        """
        Calls GET /api/agent-tools/departments/{departmentId}/budget-summary
        Handles NOT_CONFIGURED gracefully per AI-10.
        """
        token = await self.get_access_token()
        headers = {"Authorization": f"Bearer {token}"} if token else {}
        params = {}
        if fiscal_year:
            params["fiscalYear"] = fiscal_year
        if organization_id:
            params["organizationId"] = str(organization_id)

        url = f"{self.base_url}/api/agent-tools/departments/{department_id}/budget-summary"
        async with httpx.AsyncClient(verify=False) as client:
            response = await client.get(url, headers=headers, params=params, timeout=10.0)
            if response.status_code == 404:
                return {
                    "department_id": str(department_id),
                    "status": "NOT_FOUND",
                    "note": "Department not found in database",
                }
            response.raise_for_status()
            return response.json()

    async def compute_depreciation(
        self,
        acquisition_cost: float,
        acquisition_date: str,
        useful_life_years: int,
        as_of_date: Optional[str] = None,
    ) -> Dict[str, Any]:
        """
        Calls POST /api/agent-tools/compute-depreciation (pure computation)
        """
        url = f"{self.base_url}/api/agent-tools/compute-depreciation"
        payload = {
            "acquisitionCost": acquisition_cost,
            "acquisitionDate": acquisition_date,
            "usefulLifeYears": useful_life_years,
        }
        if as_of_date:
            payload["asOfDate"] = as_of_date

        async with httpx.AsyncClient(verify=False) as client:
            response = await client.post(url, json=payload, timeout=10.0)
            response.raise_for_status()
            return response.json()
