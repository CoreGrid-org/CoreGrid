import { useEffect, useMemo, useState } from "react";
import { ComboBox, Dropdown, Search, Tag } from "@carbon/react";
import { useAssetCategories, useAssetTypes, useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import {
  ASSET_CONDITIONS,
  ASSET_STATUSES,
  type Asset,
  type AssetCategory,
  type AssetQueryParameters,
  type AssetType,
  type Department,
  type Location,
} from "@/features/assets/types/asset";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { formatDate } from "@/shared/lib/dates";
import { getInventoryAssets } from "../api/inventoryReport";
import { useReportData } from "../hooks/useReportData";
import { downloadCsv, downloadPdf, exportBlockReason, formatCurrency, type ExportColumn, type ReportExport } from "../lib/export";
import { ReportExportBar, ReportFilters, ReportStats, ReportStatus, ReportTable, type ReportColumn } from "./ReportParts";

const YEAR_MS = 365.25 * 24 * 60 * 60 * 1000;

const EXPORT_COLUMNS: ExportColumn<Asset>[] = [
  { header: "Asset code", width: 25, value: (a) => a.asset_code },
  { header: "Name", width: 34, value: (a) => a.name },
  { header: "Asset type", width: 31, value: (a) => a.asset_type_name },
  { header: "Department", width: 29, value: (a) => a.department_name },
  { header: "Location", width: 28, value: (a) => a.location_name },
  { header: "Status", width: 25, value: (a) => formatStatusLabel(a.status) },
  { header: "Condition", width: 25, value: (a) => formatStatusLabel(a.condition) },
  { header: "Acquired", width: 24, value: (a) => formatDate(a.acquisition_date.slice(0, 10)) },
  { header: "Cost", width: 27, value: (a) => formatCurrency(a.acquisition_cost) },
];

const DETAIL_COLUMNS: ReportColumn<Asset>[] = [
  { header: "Asset code", render: (a) => <span className="cg-table__mono">{a.asset_code}</span> },
  { header: "Name", render: (a) => a.name },
  { header: "Asset type", muted: true, render: (a) => a.asset_type_name },
  { header: "Department", muted: true, render: (a) => a.department_name },
  { header: "Location", muted: true, render: (a) => a.location_name },
  { header: "Status", render: (a) => <Tag type={statusTagColor(a.status)}>{formatStatusLabel(a.status)}</Tag> },
  { header: "Condition", render: (a) => <Tag type={statusTagColor(a.condition)}>{formatStatusLabel(a.condition)}</Tag> },
  { header: "Acquired", muted: true, render: (a) => formatDate(a.acquisition_date.slice(0, 10)) },
  { header: "Acquisition cost", muted: true, render: (a) => formatCurrency(a.acquisition_cost) },
];

type Breakdown = { key: string; count: number; value: number };
const BREAKDOWN_COLUMNS: ReportColumn<Breakdown>[] = [
  { header: "Department", render: (b) => b.key },
  { header: "Assets", muted: true, render: (b) => b.count },
  { header: "Total value", muted: true, render: (b) => formatCurrency(b.value) },
];

// Search plus category/type/department/location/status/condition filters over
// GET /api/assets (scoped to the caller's role by the backend), with PDF/CSV export.
export default function InventoryReportPanel() {
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState<AssetCategory | null>(null);
  const [assetType, setAssetType] = useState<AssetType | null>(null);
  const [department, setDepartment] = useState<Department | null>(null);
  const [location, setLocation] = useState<Location | null>(null);
  const [status, setStatus] = useState("");
  const [condition, setCondition] = useState("");

  const { data: categories } = useAssetCategories();
  const { data: assetTypes } = useAssetTypes();
  const { data: departments } = useDepartments();
  const { data: locations } = useLocations(department?.id);
  // Only types in the chosen category are offered.
  const typeOptions = (assetTypes ?? []).filter((t) => !category || t.asset_category_id === category.id);

  // Debounce typing so every keystroke doesn't start a full report reload.
  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const query: Omit<AssetQueryParameters, "page" | "pageSize"> = {
    search: search || undefined,
    categoryId: category?.id,
    assetTypeId: assetType?.id,
    departmentId: department?.id,
    locationId: location?.id,
    status: status || undefined,
    condition: condition || undefined,
  };
  const queryKey = JSON.stringify(query);
  const report = useReportData(queryKey, true, (token) => getInventoryAssets(query, token));
  // Ages are measured from when the page opened, keeping the stats pure.
  const [now] = useState(() => Date.now());
  const assets = useMemo(() => report.data ?? [], [report.data]);

  const { totalValue, averageAge, byDepartment } = useMemo(() => {
    const groups = new Map<string, Breakdown>();
    let value = 0;
    let ageYears = 0;
    for (const a of assets) {
      value += a.acquisition_cost;
      ageYears += Math.max(0, now - new Date(a.acquisition_date).getTime()) / YEAR_MS;
      const current = groups.get(a.department_name) ?? { key: a.department_name, count: 0, value: 0 };
      groups.set(a.department_name, { key: a.department_name, count: current.count + 1, value: current.value + a.acquisition_cost });
    }
    return {
      totalValue: value,
      averageAge: `${(assets.length > 0 ? ageYears / assets.length : 0).toFixed(1)} years`,
      byDepartment: [...groups.values()].sort((a, b) => b.count - a.count),
    };
  }, [assets, now]);

  const filterLabels = [
    search && `Search: "${search}"`,
    category && `Category: ${category.name}`,
    assetType && `Type: ${assetType.name}`,
    department && `Department: ${department.name}`,
    location && `Location: ${location.name}`,
    status && `Status: ${formatStatusLabel(status)}`,
    condition && `Condition: ${formatStatusLabel(condition)}`,
  ].filter((label): label is string => Boolean(label));

  const clearFilters = () => {
    setSearchInput("");
    setSearch("");
    setCategory(null);
    setAssetType(null);
    setDepartment(null);
    setLocation(null);
    setStatus("");
    setCondition("");
  };

  const exportData: ReportExport<Asset> = {
    title: "Asset Inventory Report",
    fileName: "asset-inventory",
    columns: EXPORT_COLUMNS,
    rows: assets,
    summary: [
      `Assets in scope: ${assets.length.toLocaleString()}`,
      `Total acquisition value: ${formatCurrency(totalValue)}`,
      `Average age: ${averageAge}`,
    ],
    filters: filterLabels,
  };

  return (
    <div className="cg-report">
      <ReportFilters activeCount={filterLabels.length} onClear={clearFilters} isRefreshing={report.isRefreshing}>
        <Search
          id="inventory-report-search"
          labelText="Search assets"
          placeholder="Code, name, category, type, or attribute"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          size="md"
          className="cg-report__filter-wide"
        />
        <ComboBox<AssetCategory>
          id="inventory-report-category"
          titleText="Category"
          placeholder="All categories"
          items={categories ?? []}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={category}
          shouldFilterItem={comboBoxFilter(category)}
          onChange={({ selectedItem }) => {
            setCategory(selectedItem ?? null);
            setAssetType(null);
          }}
        />
        <ComboBox<AssetType>
          id="inventory-report-type"
          titleText="Asset type"
          placeholder="All types"
          items={typeOptions}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={assetType}
          shouldFilterItem={comboBoxFilter(assetType)}
          onChange={({ selectedItem }) => setAssetType(selectedItem ?? null)}
        />
        <ComboBox<Department>
          id="inventory-report-department"
          titleText="Department"
          placeholder="All departments"
          items={departments ?? []}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={department}
          shouldFilterItem={comboBoxFilter(department)}
          onChange={({ selectedItem }) => {
            setDepartment(selectedItem ?? null);
            setLocation(null);
          }}
        />
        <ComboBox<Location>
          id="inventory-report-location"
          titleText="Location"
          placeholder={department ? "All locations" : "Choose a department first"}
          items={locations ?? []}
          itemToString={(item) => item?.name ?? ""}
          selectedItem={location}
          shouldFilterItem={comboBoxFilter(location)}
          onChange={({ selectedItem }) => setLocation(selectedItem ?? null)}
          disabled={!department}
        />
        <Dropdown
          id="inventory-report-status"
          titleText="Status"
          label="All statuses"
          items={["", ...ASSET_STATUSES]}
          itemToString={(val) => (val ? formatStatusLabel(val) : "All statuses")}
          selectedItem={status}
          onChange={({ selectedItem }) => setStatus(selectedItem ?? "")}
        />
        <Dropdown
          id="inventory-report-condition"
          titleText="Condition"
          label="All conditions"
          items={["", ...ASSET_CONDITIONS]}
          itemToString={(val) => (val ? formatStatusLabel(val) : "All conditions")}
          selectedItem={condition}
          onChange={({ selectedItem }) => setCondition(selectedItem ?? "")}
        />
      </ReportFilters>

      <ReportStatus title="Could not load the asset inventory" error={report.error} isInitialLoading={report.isInitialLoading} />

      {report.data && (
        <div className={report.isRefreshing ? "cg-report__results is-stale" : "cg-report__results"}>
          <ReportStats
            stats={[
              { label: "Assets in scope", value: assets.length.toLocaleString() },
              { label: "Total acquisition value", value: formatCurrency(totalValue) },
              { label: "Average age", value: averageAge },
            ]}
          />
          <ReportExportBar
            count={assets.length}
            noun="assets"
            onPdf={() => downloadPdf(exportData)}
            onCsv={() => downloadCsv(exportData)}
            disabledReason={exportBlockReason(true, report.isRefreshing, assets.length)}
          />
          <ReportTable
            title="By department"
            columns={BREAKDOWN_COLUMNS}
            rows={byDepartment}
            rowKey={(b) => b.key}
            emptyText="No assets match these filters."
            pageable={false}
          />
          <ReportTable
            key={queryKey}
            title="Asset details"
            columns={DETAIL_COLUMNS}
            rows={assets}
            rowKey={(a) => a.id}
            emptyText="No assets match these filters."
          />
        </div>
      )}
    </div>
  );
}
