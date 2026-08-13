import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, Accordion, AccordionItem } from "@carbon/react";
import { Add, Search } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { MOCK_ASSETS, MOCK_ASSET_CATEGORIES, MOCK_ASSET_TYPES } from "../data/mockAssets";

const DATA_TYPE_COLOR: Record<string, "gray" | "blue" | "purple" | "teal" | "magenta"> = {
  TEXT: "gray",
  NUMBER: "blue",
  DATE: "purple",
  BOOLEAN: "teal",
  SELECT: "magenta",
};

export default function AssetsPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Asset Registry</h1>
          <p className="cg-page__subtitle">Register, classify and track assets by QR code (FR-016 to FR-032).</p>
        </div>
        <Button renderIcon={Add}>Register asset</Button>
      </div>

      <Tabs>
        <TabList aria-label="Asset registry sections">
          <Tab>Asset Register</Tab>
          <Tab>Categories</Tab>
          <Tab>Types & Attributes</Tab>
        </TabList>
        <TabPanels>
          {/* ── Asset Register ─────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-021", "FR-025", "FR-028"]}>
              The real tab lists every asset in the organisation via GET /api/assets, with server-side
              search, filter and pagination; rows link through to a detail view with history, QR label and
              the Verify action.
            </MockNotice>

            <div className="cg-section">
              <div className="cg-toolbar">
                <div className="cg-search">
                  <Search size={16} className="cg-search__icon" />
                  <input className="cg-search__input" placeholder="Search by code, name or attribute…" disabled />
                </div>
              </div>
              <table className="cg-table cg-table--no-hover">
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
                  {MOCK_ASSETS.map((asset) => (
                    <tr key={asset.code}>
                      <td className="cg-table__mono">{asset.code}</td>
                      <td>{asset.name}</td>
                      <td className="cg-table__muted">{asset.type}</td>
                      <td className="cg-table__muted">{asset.department}</td>
                      <td className="cg-table__muted">{asset.location}</td>
                      <td>
                        <Tag type={statusTagColor(asset.status)}>{formatStatusLabel(asset.status)}</Tag>
                      </td>
                      <td>
                        <Tag type={statusTagColor(asset.condition)}>{formatStatusLabel(asset.condition)}</Tag>
                      </td>
                      <td className="cg-table__muted">${asset.acquisitionCost.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Categories ──────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-016"]}>
              An Administrator creates asset categories as the top-level grouping used for reporting (assets
              by department/category charts on the dashboard); a category cannot be deleted while an asset
              type still references it.
            </MockNotice>

            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Asset categories</p>
                <Button kind="ghost" size="sm" renderIcon={Add}>
                  Add category
                </Button>
              </div>
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Name</th>
                    <th>Asset types</th>
                    <th>Assets</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_ASSET_CATEGORIES.map((cat) => (
                    <tr key={cat.code}>
                      <td className="cg-table__mono">{cat.code}</td>
                      <td>{cat.name}</td>
                      <td className="cg-table__muted">{cat.typeCount}</td>
                      <td className="cg-table__muted">{cat.assetCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Types & Attributes ─────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-017", "FR-018", "FR-019", "FR-020"]}>
              This is the configurable platform model: an Administrator defines, per asset type, an ordered
              set of custom attributes (name, data type, required flag, validation rule). Both React and
              Flutter render the asset form dynamically from these definitions — a new asset type with new
              attributes needs no client-side code change, which is what lets one CoreGrid deployment serve a
              transport fleet and a hospital inventory with entirely different "shapes" of asset.
            </MockNotice>

            <Accordion>
              {MOCK_ASSET_TYPES.map((type) => (
                <AccordionItem
                  key={type.code}
                  title={
                    <span>
                      <strong>{type.name}</strong>
                      <span className="cg-table__muted"> · {type.category} · {type.code}</span>
                    </span>
                  }
                >
                  <div className="cg-kv-grid cg-kv-grid--three" style={{ marginBottom: "1rem", border: "1px solid #e0e0e0" }}>
                    <div className="cg-kv-item">
                      <p className="cg-kv-item__label">Useful life</p>
                      <p className="cg-kv-item__value">{type.usefulLifeYears} years</p>
                    </div>
                    <div className="cg-kv-item">
                      <p className="cg-kv-item__label">Maintenance interval</p>
                      <p className="cg-kv-item__value">
                        {type.maintenanceIntervalDays ? `${type.maintenanceIntervalDays} days` : "Not scheduled"}
                      </p>
                    </div>
                    <div className="cg-kv-item">
                      <p className="cg-kv-item__label">Custom attributes</p>
                      <p className="cg-kv-item__value">{type.attributes.length}</p>
                    </div>
                  </div>

                  <table className="cg-table cg-table--no-hover">
                    <thead>
                      <tr>
                        <th>Attribute</th>
                        <th>Data type</th>
                        <th>Required</th>
                        <th>Notes</th>
                      </tr>
                    </thead>
                    <tbody>
                      {type.attributes.map((attr) => (
                        <tr key={attr.name}>
                          <td>{attr.name}</td>
                          <td>
                            <Tag type={DATA_TYPE_COLOR[attr.dataType]}>{attr.dataType}</Tag>
                          </td>
                          <td className="cg-table__muted">{attr.required ? "Required" : "Optional"}</td>
                          <td className="cg-table__muted">{attr.notes ?? "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </AccordionItem>
              ))}
            </Accordion>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
