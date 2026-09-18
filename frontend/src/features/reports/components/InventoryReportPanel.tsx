import { Button, ComboBox, InlineNotification, Pagination, Search, Select, SelectItem, SkeletonText, Tag } from "@carbon/react";
import { DocumentExport, DocumentPdf } from "@carbon/icons-react";
import { jsPDF } from "jspdf";
import { useEffect, useState } from "react";
import { useThunderID } from "@thunderid/react";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useAssetCategories, useAssetTypes, useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import { listAssets } from "@/features/assets/api/assets";
import { ASSET_CONDITIONS, ASSET_STATUSES, type AssetCategory, type AssetQueryParameters, type AssetType, type Department, type Location, type PagedResult } from "@/features/assets/types/asset";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { getInventoryAssets } from "../api/inventoryReport";
import type { Asset } from "@/features/assets/types/asset";

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", maximumFractionDigits: 0 }).format(value);
}

function averageAgeInYears(assets: Asset[]) {
  if (assets.length === 0) return "0.0 years";
  const today = Date.now();
  const ageInYears = assets.reduce((total, asset) => {
    const acquisitionTime = new Date(asset.acquisition_date).getTime();
    return total + Math.max(0, today - acquisitionTime) / (365.25 * 24 * 60 * 60 * 1000);
  }, 0) / assets.length;
  return `${ageInYears.toFixed(1)} years`;
}

function formatDate(value: string) {
  return new Date(`${value}T00:00:00`).toLocaleDateString();
}

function csvEscape(value: string | number) {
  const text = String(value);
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function downloadCsv(assets: Asset[]) {
  const headers = ["Asset code", "Name", "Asset type", "Department", "Location", "Status", "Condition", "Acquired", "Acquisition cost"];
  const rows = assets.map((asset) => [
    asset.asset_code,
    asset.name,
    asset.asset_type_name,
    asset.department_name,
    asset.location_name,
    formatStatusLabel(asset.status),
    formatStatusLabel(asset.condition),
    asset.acquisition_date,
    asset.acquisition_cost,
  ]);
  const csv = [headers, ...rows].map((row) => row.map(csvEscape).join(",")).join("\n");
  const blob = new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `asset-inventory-${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function printPdf(assets: Asset[], totalValue: number) {
  const document = new jsPDF({ orientation: "landscape", unit: "mm", format: "a4" });
  const pageHeight = document.internal.pageSize.getHeight();
  const left = 10;
  const columnWidths = [25, 34, 31, 29, 28, 25, 25, 24, 27];
  const headers = ["Asset code", "Name", "Asset type", "Department", "Location", "Status", "Condition", "Acquired", "Cost"];
  let y = 15;

  const drawHeader = () => {
    document.setFillColor(224, 224, 224);
    document.rect(left, y - 5, columnWidths.reduce((sum, width) => sum + width, 0), 8, "F");
    document.setFont("helvetica", "bold");
    document.setFontSize(6.5);
    let x = left + 1;
    headers.forEach((header, index) => {
      document.text(header, x, y);
      x += columnWidths[index];
    });
    y += 7;
    document.setFont("helvetica", "normal");
  };

  document.setFont("helvetica", "bold");
  document.setFontSize(16);
  document.text("Asset Inventory Report", left, y);
  y += 6;
  document.setFont("helvetica", "normal");
  document.setFontSize(8);
  document.text(`Generated ${new Date().toLocaleString()}`, left, y);
  y += 8;
  document.setFontSize(9);
  document.text(`Assets in scope: ${assets.length.toLocaleString()}`, left, y);
  document.text(`Total acquisition value: ${formatCurrency(totalValue)}`, left + 60, y);
  document.text(`Average age: ${averageAgeInYears(assets)}`, left + 145, y);
  y += 9;
  drawHeader();

  assets.forEach((asset) => {
    const values = [
      asset.asset_code,
      asset.name,
      asset.asset_type_name,
      asset.department_name,
      asset.location_name,
      formatStatusLabel(asset.status),
      formatStatusLabel(asset.condition),
      formatDate(asset.acquisition_date),
      formatCurrency(asset.acquisition_cost),
    ];
    const lines = values.map((value, index) => document.splitTextToSize(String(value), columnWidths[index] - 2));
    const rowHeight = Math.max(...lines.map((value) => value.length)) * 3.2 + 3;
    if (y + rowHeight > pageHeight - 10) {
      document.addPage();
      y = 15;
      drawHeader();
    }
    let x = left + 1;
    lines.forEach((value, index) => {
      document.text(value, x, y, { baseline: "top" });
      x += columnWidths[index];
    });
    document.setDrawColor(210, 210, 210);
    document.line(left, y + rowHeight - 1, left + columnWidths.reduce((sum, width) => sum + width, 0), y + rowHeight - 1);
    y += rowHeight;
  });

  document.save(`asset-inventory-${new Date().toISOString().slice(0, 10)}.pdf`);
}

export default function InventoryReportPanel() {
  const { getAccessToken } = useThunderID();
  const [assets, setAssets] = useState<Asset[]>([]);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [assetTypeId, setAssetTypeId] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [status, setStatus] = useState("");
  const [condition, setCondition] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [error, setError] = useState<unknown>();
  const [isLoading, setIsLoading] = useState(true);
  const { data: categories } = useAssetCategories();
  const { data: assetTypes } = useAssetTypes();
  const { data: departments } = useDepartments();
  const { data: locations } = useLocations(departmentId || undefined);

  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    setLocationId("");
  }, [departmentId]);

  useEffect(() => {
    setPage(1);
  }, [search, categoryId, assetTypeId, departmentId, locationId, status, condition]);

  const query: Omit<AssetQueryParameters, "page" | "pageSize"> = {
    search: search || undefined,
    categoryId: categoryId || undefined,
    assetTypeId: assetTypeId || undefined,
    departmentId: departmentId || undefined,
    locationId: locationId || undefined,
    status: status || undefined,
    condition: condition || undefined,
  };

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(undefined);

    getAccessToken()
      .then((token) => getInventoryAssets(query, token))
      .then((result) => {
        if (!cancelled) {
          setAssets(result);
          setIsLoading(false);
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setError(reason);
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [getAccessToken, search, categoryId, assetTypeId, departmentId, locationId, status, condition]);

  // Real server-side pagination for the detail table specifically — a
  // separate request per page, not a client-side slice of the full
  // (already-aggregated) `assets` array above, which stays as-is for the
  // stats/by-department breakdown and for PDF/CSV export (those need every
  // filtered asset, not just one page of them).
  const [detailPage, setDetailPage] = useState<PagedResult<Asset>>({
    items: [], total_count: 0, page: 1, page_size: pageSize, total_pages: 0,
  });

  useEffect(() => {
    let cancelled = false;

    getAccessToken()
      .then((token) => listAssets({ ...query, page, pageSize, sortBy: "name", sortDirection: "asc" }, token))
      .then((result) => {
        if (!cancelled) setDetailPage(result);
      })
      .catch(() => {
        // Errors here surface through the aggregate fetch's own error
        // state above (same filters, same failure mode) — no need for a
        // second error banner for the same underlying request.
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- query is rebuilt every render from the same state below
  }, [getAccessToken, search, categoryId, assetTypeId, departmentId, locationId, status, condition, page, pageSize]);

  if (isLoading) {
    return <div className="cg-section"><SkeletonText paragraph lineCount={4} /></div>;
  }

  if (error) {
    return (
      <InlineNotification
        kind="error"
        title="Could not load the asset inventory"
        subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
        hideCloseButton
      />
    );
  }

  const totalValue = assets.reduce((total, asset) => total + asset.acquisition_cost, 0);
  const byDepartment = Array.from(
    assets.reduce((groups, asset) => {
      const current = groups.get(asset.department_name) ?? { count: 0, value: 0 };
      groups.set(asset.department_name, {
        count: current.count + 1,
        value: current.value + asset.acquisition_cost,
      });
      return groups;
    }, new Map<string, { count: number; value: number }>()),
  ).sort(([, left], [, right]) => right.count - left.count);

  return (
    <div className="cg-section">
      <div className="cg-toolbar" style={{ flexWrap: "wrap", gap: "0.75rem", alignItems: "end" }}>
        <Search
          id="inventory-report-search"
          labelText="Search assets"
          placeholder="Code, name, category, type, or attribute"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          size="md"
          style={{ minWidth: "18rem" }}
        />
        <ComboBox<AssetCategory>
          id="inventory-report-category"
          titleText="Category"
          placeholder="All categories"
          items={categories ?? []}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={categories?.find((category) => category.id === categoryId) ?? null}
          onChange={({ selectedItem }) => setCategoryId(selectedItem?.id ?? "")}
        />
        <ComboBox<AssetType>
          id="inventory-report-type"
          titleText="Asset type"
          placeholder="All types"
          items={assetTypes ?? []}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={assetTypes?.find((type) => type.id === assetTypeId) ?? null}
          onChange={({ selectedItem }) => setAssetTypeId(selectedItem?.id ?? "")}
        />
        <ComboBox<Department>
          id="inventory-report-department"
          titleText="Department"
          placeholder="All departments"
          items={departments ?? []}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={departments?.find((department) => department.id === departmentId) ?? null}
          onChange={({ selectedItem }) => setDepartmentId(selectedItem?.id ?? "")}
        />
        <ComboBox<Location>
          id="inventory-report-location"
          titleText="Location"
          placeholder="All locations"
          items={locations ?? []}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={locations?.find((location) => location.id === locationId) ?? null}
          onChange={({ selectedItem }) => setLocationId(selectedItem?.id ?? "")}
          disabled={!departmentId && locations?.length === 0}
        />
        <Select id="inventory-report-status" labelText="Status" value={status} onChange={(event) => setStatus(event.target.value)}>
          <SelectItem value="" text="All statuses" />
          {ASSET_STATUSES.map((value) => <SelectItem key={value} value={value} text={formatStatusLabel(value)} />)}
        </Select>
        <Select id="inventory-report-condition" labelText="Condition" value={condition} onChange={(event) => setCondition(event.target.value)}>
          <SelectItem value="" text="All conditions" />
          {ASSET_CONDITIONS.map((value) => <SelectItem key={value} value={value} text={formatStatusLabel(value)} />)}
        </Select>
        <Button
          kind="ghost"
          size="md"
          onClick={() => {
            setSearchInput("");
            setSearch("");
            setCategoryId("");
            setAssetTypeId("");
            setDepartmentId("");
            setLocationId("");
            setStatus("");
            setCondition("");
          }}
        >
          Clear filters
        </Button>
      </div>
      <div className="cg-stat-grid" style={{ padding: "1.5rem", marginBottom: 0, gridTemplateColumns: "repeat(3, 1fr)" }}>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Assets in scope</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{assets.length.toLocaleString()}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Total acquisition value</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{formatCurrency(totalValue)}</p>
        </div>
        <div className="cg-stat-card">
          <p className="cg-stat-card__label">Average age</p>
          <p className="cg-stat-card__value" style={{ fontSize: "1.5rem" }}>{averageAgeInYears(assets)}</p>
        </div>
      </div>

      <div className="cg-toolbar" style={{ justifyContent: "flex-end", marginTop: "1rem" }}>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button kind="tertiary" size="sm" renderIcon={DocumentPdf} onClick={() => printPdf(assets, totalValue)}>
            Export PDF
          </Button>
          <Button kind="tertiary" size="sm" renderIcon={DocumentExport} onClick={() => downloadCsv(assets)}>
            Export CSV
          </Button>
        </div>
      </div>

      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr><th>Department</th><th>Assets</th><th>Total value</th></tr>
        </thead>
        <tbody>
          {byDepartment.map(([department, summary]) => (
            <tr key={department}>
              <td>{department}</td>
              <td className="cg-table__muted">{summary.count}</td>
              <td className="cg-table__muted">{formatCurrency(summary.value)}</td>
            </tr>
          ))}
          {byDepartment.length === 0 && <tr><td colSpan={3} className="cg-table__muted">No assets found.</td></tr>}
        </tbody>
      </table>

      <div style={{ marginTop: "2rem" }}>
        <div className="cg-section__header">
          <div>
            <h2 className="cg-section__title">Asset details</h2>
            <p className="cg-section__subtitle">
              Showing {detailPage.items.length.toLocaleString()} of {detailPage.total_count.toLocaleString()} filtered assets
            </p>
          </div>
        </div>
        <div style={{ overflowX: "auto" }}>
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Asset code</th>
                <th>Name</th>
                <th>Asset type</th>
                <th>Department</th>
                <th>Location</th>
                <th>Status</th>
                <th>Condition</th>
                <th>Acquired</th>
                <th>Acquisition cost</th>
              </tr>
            </thead>
            <tbody>
              {detailPage.items.map((asset) => (
                <tr key={asset.id}>
                  <td>{asset.asset_code}</td>
                  <td>{asset.name}</td>
                  <td className="cg-table__muted">{asset.asset_type_name}</td>
                  <td className="cg-table__muted">{asset.department_name}</td>
                  <td className="cg-table__muted">{asset.location_name}</td>
                  <td><Tag type={statusTagColor(asset.status)}>{formatStatusLabel(asset.status)}</Tag></td>
                  <td><Tag type={statusTagColor(asset.condition)}>{formatStatusLabel(asset.condition)}</Tag></td>
                  <td className="cg-table__muted">{formatDate(asset.acquisition_date)}</td>
                  <td className="cg-table__muted">{formatCurrency(asset.acquisition_cost)}</td>
                </tr>
              ))}
              {detailPage.items.length === 0 && <tr><td colSpan={9} className="cg-table__muted">No assets found.</td></tr>}
            </tbody>
          </table>
        </div>
        {detailPage.total_count > 0 && (
          <Pagination
            page={page}
            pageSize={pageSize}
            pageSizes={[10, 20, 50, 100]}
            totalItems={detailPage.total_count}
            onChange={({ page: nextPage, pageSize: nextPageSize }) => {
              setPage(nextPage);
              setPageSize(nextPageSize);
            }}
          />
        )}
      </div>
    </div>
  );
}