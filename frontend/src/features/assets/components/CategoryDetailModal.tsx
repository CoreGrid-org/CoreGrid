import { useState } from "react";
import {
  Modal,
  Tag,
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
  Pagination,
  InlineNotification,
} from "@carbon/react";
import { useAssetTypes, useAssetsList } from "../hooks/useAssets";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { formatCurrency } from "../utils/format";
import type { AssetCategory } from "../types/asset";

interface CategoryDetailModalProps {
  category: AssetCategory;
  onClose: () => void;
  onSelectAsset?: (assetId: string) => void;
}

export default function CategoryDetailModal({
  category,
  onClose,
  onSelectAsset,
}: CategoryDetailModalProps) {
  const { data: allTypes, isLoading: isLoadingTypes } = useAssetTypes();
  const [assetPage, setAssetPage] = useState(1);
  const [assetPageSize, setAssetPageSize] = useState(10);

  const assets = useAssetsList({
    categoryId: category.id,
    page: assetPage,
    pageSize: assetPageSize,
  });

  const categoryTypes = (allTypes ?? []).filter(
    (t) => t.asset_category_id === category.id,
  );

  return (
    <Modal
      open
      modalLabel="Asset Category"
      modalHeading={category.name}
      passiveModal
      onRequestClose={onClose}
      size="lg"
    >
      <div
        style={{
          display: "flex",
          gap: "0.5rem",
          alignItems: "center",
          flexWrap: "wrap",
          marginBottom: "1.25rem",
        }}
      >
        <Tag type="blue">Code: {category.code}</Tag>
        {category.is_active ? (
          <Tag type="green">Active</Tag>
        ) : (
          <Tag type="gray">Inactive</Tag>
        )}
        <Tag type="purple">{category.type_count} Asset Types</Tag>
        <Tag type="teal">{category.asset_count} Total Assets</Tag>
      </div>

      <Tabs>
        <TabList aria-label="Category details tabs">
          <Tab>Asset Types ({categoryTypes.length})</Tab>
          <Tab>Assets ({assets.data?.total_count ?? category.asset_count})</Tab>
        </TabList>

        <TabPanels>
          {/* ── Tab 1: Asset Types belonging to this Category ── */}
          <TabPanel style={{ paddingTop: "1rem" }}>
            {isLoadingTypes ? (
              <div className="cg-placeholder">
                <p>Loading asset types…</p>
              </div>
            ) : categoryTypes.length > 0 ? (
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Type Name</th>
                    <th>Useful Life</th>
                    <th>Maintenance</th>
                    <th>Attributes</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {categoryTypes.map((type) => (
                    <tr
                      key={type.id}
                      style={{ opacity: type.is_active ? 1 : 0.65 }}
                    >
                      <td className="cg-table__mono">{type.code}</td>
                      <td style={{ fontWeight: 500 }}>{type.name}</td>
                      <td>
                        {type.useful_life_years
                          ? `${type.useful_life_years} years`
                          : "—"}
                      </td>
                      <td>
                        {type.default_maintenance_interval_days
                          ? `Every ${type.default_maintenance_interval_days} days`
                          : "—"}
                      </td>
                      <td>{type.attribute_count} custom attrs</td>
                      <td>
                        <Tag type={type.is_active ? "green" : "gray"}>
                          {type.is_active ? "Active" : "Inactive"}
                        </Tag>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <div className="cg-placeholder">
                <p>No asset types configured for this category yet.</p>
              </div>
            )}
          </TabPanel>

          {/* ── Tab 2: Assets belonging to this Category ── */}
          <TabPanel style={{ paddingTop: "1rem" }}>
            {assets.isLoading ? (
              <div className="cg-placeholder">
                <p>Loading belonging assets…</p>
              </div>
            ) : assets.isError ? (
              <InlineNotification
                kind="error"
                title="Could not load assets"
                subtitle="Failed to fetch assets belonging to this category."
                lowContrast
                hideCloseButton
              />
            ) : assets.data && assets.data.items.length > 0 ? (
              <>
                <div style={{ overflowX: "auto", width: "100%" }}>
                  <table className="cg-table">
                    <thead>
                      <tr>
                        <th>Asset Code</th>
                        <th>Name</th>
                        <th>Type</th>
                        <th>Department</th>
                        <th>Location</th>
                        <th>Status</th>
                        <th>Condition</th>
                        <th>Cost</th>
                      </tr>
                    </thead>
                    <tbody>
                      {assets.data.items.map((asset) => (
                        <tr
                          key={asset.id}
                          onClick={() => onSelectAsset?.(asset.id)}
                          style={{ cursor: onSelectAsset ? "pointer" : "default" }}
                        >
                          <td className="cg-table__mono">{asset.asset_code}</td>
                          <td style={{ fontWeight: 500 }}>{asset.name}</td>
                          <td className="cg-table__muted">
                            {asset.asset_type_name}
                          </td>
                          <td className="cg-table__muted">
                            {asset.department_name}
                          </td>
                          <td className="cg-table__muted">
                            {asset.location_name}
                          </td>
                          <td>
                            <Tag type={statusTagColor(asset.status)}>
                              {formatStatusLabel(asset.status)}
                            </Tag>
                          </td>
                          <td>
                            <Tag type={statusTagColor(asset.condition)}>
                              {formatStatusLabel(asset.condition)}
                            </Tag>
                          </td>
                          <td className="cg-table__muted">
                            {formatCurrency(asset.acquisition_cost)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <Pagination
                  page={assets.data.page}
                  pageSize={assets.data.page_size}
                  pageSizes={[5, 10, 20]}
                  totalItems={assets.data.total_count}
                  onChange={({ page: p, pageSize: ps }) => {
                    setAssetPage(p);
                    setAssetPageSize(ps);
                  }}
                />
              </>
            ) : (
              <div className="cg-placeholder">
                <p>No assets registered under this category yet.</p>
              </div>
            )}
          </TabPanel>
        </TabPanels>
      </Tabs>
    </Modal>
  );
}
