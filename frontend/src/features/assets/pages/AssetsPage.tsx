import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Tag, Button, Select, SelectItem, Pagination, InlineNotification } from "@carbon/react";
import { Add, Search } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useAssetTypes, useAssetsList, useDepartments, useLocations } from "../hooks/useAssets";
import AssetDetailModal from "../components/AssetDetailModal";
import { ASSET_CONDITIONS, ASSET_STATUSES, type AssetQueryParameters } from "../types/asset";
import { formatCurrency } from "../utils/format";

// The asset register — GET /api/assets with server-side search/filter/pagination.
// Categories and Types & Attributes management live on the separate Asset
// Config page (sidebar: Assets > Asset Config).
export default function AssetsPage() {
  const navigate = useNavigate();
  const location = useLocation();

  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [assetTypeId, setAssetTypeId] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [status, setStatus] = useState("");
  const [condition, setCondition] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [selectedAssetId, setSelectedAssetId] = useState<string | undefined>(
    (location.state as { openAssetId?: string } | null)?.openAssetId,
  );

  const { data: assetTypes } = useAssetTypes();
  const { data: departments } = useDepartments();
  const { data: locations } = useLocations(departmentId || undefined);

  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [search, assetTypeId, departmentId, locationId, status, condition]);

  useEffect(() => {
    setLocationId("");
  }, [departmentId]);

  // Clear the navigation state once consumed, so a browser refresh doesn't
  // keep re-opening the modal for an asset the user may have since closed.
  useEffect(() => {
    if ((location.state as { openAssetId?: string } | null)?.openAssetId) {
      navigate(location.pathname, { replace: true, state: null });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- run once on mount only
  }, []);

  const params: AssetQueryParameters = {
    search: search || undefined,
    assetTypeId: assetTypeId || undefined,
    departmentId: departmentId || undefined,
    locationId: locationId || undefined,
    status: status || undefined,
    condition: condition || undefined,
    page,
    pageSize,
  };

  const { data, isLoading, isError, error, refetch } = useAssetsList(params);

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Asset Register</h1>
          <p className="cg-page__subtitle">Every asset in the organisation, searchable and filterable (FR-021 to FR-025).</p>
        </div>
        <Button renderIcon={Add} onClick={() => navigate("/admin/assets/new")}>
          Register asset
        </Button>
      </div>

      {isError && (
        <InlineNotification
          kind="error"
          title="Could not load assets"
          subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div className="cg-section">
        <div className="cg-toolbar" style={{ flexWrap: "wrap", gap: "0.75rem" }}>
          <div className="cg-search">
            <Search size={16} className="cg-search__icon" />
            <input
              className="cg-search__input"
              placeholder="Search by code or name…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <div style={{ minWidth: "10rem" }}>
            <Select
              id="asset-filter-type"
              labelText="Type"
              hideLabel
              value={assetTypeId}
              onChange={(e) => setAssetTypeId(e.target.value)}
            >
              <SelectItem value="" text="All types" />
              {assetTypes?.map((t) => <SelectItem key={t.id} value={t.id} text={t.name} />)}
            </Select>
          </div>
          <div style={{ minWidth: "10rem" }}>
            <Select
              id="asset-filter-department"
              labelText="Department"
              hideLabel
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value)}
            >
              <SelectItem value="" text="All departments" />
              {departments?.map((d) => <SelectItem key={d.id} value={d.id} text={d.name} />)}
            </Select>
          </div>
          <div style={{ minWidth: "10rem" }}>
            <Select
              id="asset-filter-location"
              labelText="Location"
              hideLabel
              value={locationId}
              onChange={(e) => setLocationId(e.target.value)}
            >
              <SelectItem value="" text="All locations" />
              {locations?.map((l) => <SelectItem key={l.id} value={l.id} text={l.name} />)}
            </Select>
          </div>
          <div style={{ minWidth: "10rem" }}>
            <Select
              id="asset-filter-status"
              labelText="Status"
              hideLabel
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              <SelectItem value="" text="All statuses" />
              {ASSET_STATUSES.map((s) => <SelectItem key={s} value={s} text={formatStatusLabel(s)} />)}
            </Select>
          </div>
          <div style={{ minWidth: "10rem" }}>
            <Select
              id="asset-filter-condition"
              labelText="Condition"
              hideLabel
              value={condition}
              onChange={(e) => setCondition(e.target.value)}
            >
              <SelectItem value="" text="All conditions" />
              {ASSET_CONDITIONS.map((c) => <SelectItem key={c} value={c} text={formatStatusLabel(c)} />)}
            </Select>
          </div>
        </div>

        {isLoading ? (
          <div className="cg-placeholder">
            <p>Loading assets…</p>
          </div>
        ) : data && data.items.length > 0 ? (
          <>
            <table className="cg-table">
              <thead>
                <tr>
                  <th>Asset code</th>
                  <th>Name</th>
                  <th>Type</th>
                  <th>Department</th>
                  <th>Location</th>
                  <th>Status</th>
                  <th>Condition</th>
                  <th>Acquisition cost</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((asset) => (
                  <tr key={asset.id} onClick={() => setSelectedAssetId(asset.id)} style={{ cursor: "pointer" }}>
                    <td className="cg-table__mono">{asset.asset_code}</td>
                    <td>{asset.name}</td>
                    <td className="cg-table__muted">{asset.asset_type_name}</td>
                    <td className="cg-table__muted">{asset.department_name}</td>
                    <td className="cg-table__muted">{asset.location_name}</td>
                    <td>
                      <Tag type={statusTagColor(asset.status)}>{formatStatusLabel(asset.status)}</Tag>
                    </td>
                    <td>
                      <Tag type={statusTagColor(asset.condition)}>{formatStatusLabel(asset.condition)}</Tag>
                    </td>
                    <td className="cg-table__muted">{formatCurrency(asset.acquisition_cost)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <Pagination
              page={data.page}
              pageSize={data.page_size}
              pageSizes={[10, 20, 50, 100]}
              totalItems={data.total_count}
              onChange={({ page: nextPage, pageSize: nextPageSize }) => {
                setPage(nextPage);
                setPageSize(nextPageSize);
              }}
            />
          </>
        ) : (
          <div className="cg-placeholder">
            <p>No assets match these filters.</p>
          </div>
        )}
      </div>

      {selectedAssetId && (
        <AssetDetailModal
          assetId={selectedAssetId}
          onClose={() => setSelectedAssetId(undefined)}
          onConditionUpdated={refetch}
        />
      )}
    </div>
  );
}
