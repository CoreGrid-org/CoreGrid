import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button } from "@carbon/react";
import { Add, Edit } from "@carbon/icons-react";
import MockNotice from "@/shared/components/MockNotice";
import { MOCK_DEPARTMENTS, MOCK_LOCATIONS, MOCK_POLICIES } from "../data/mockSettings";

export default function SettingsPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Organisation Settings</h1>
          <p className="cg-page__subtitle">Departments, locations and policy thresholds (FR-010 to FR-015).</p>
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Organisation settings sections">
          <Tab>Departments</Tab>
          <Tab>Locations</Tab>
          <Tab>Policy Parameters</Tab>
        </TabList>
        <TabPanels>
          {/* ── Departments ─────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-010", "FR-012"]}>
              A department referenced by an active asset cannot be deleted, only deactivated — and
              deactivation is refused while active assets still reference it.
            </MockNotice>

            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Departments</p>
                <Button kind="ghost" size="sm" renderIcon={Add}>
                  Add department
                </Button>
              </div>
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Name</th>
                    <th>Locations</th>
                    <th>Users</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_DEPARTMENTS.map((d) => (
                    <tr key={d.code}>
                      <td className="cg-table__mono">{d.code}</td>
                      <td>{d.name}</td>
                      <td className="cg-table__muted">{d.locationCount}</td>
                      <td className="cg-table__muted">{d.userCount}</td>
                      <td>
                        <Tag type={d.isActive ? "green" : "gray"}>{d.isActive ? "Active" : "Inactive"}</Tag>
                      </td>
                      <td>
                        <Edit size={16} style={{ fill: "#8d8d8d" }} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Locations ───────────────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-011", "FR-012"]}>
              Every location belongs to exactly one department. The "type" here is illustrative — store,
              workshop, office, ward — not a fixed enumeration, so an organisation can name its own location
              types without a schema change.
            </MockNotice>

            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Locations</p>
                <Button kind="ghost" size="sm" renderIcon={Add}>
                  Add location
                </Button>
              </div>
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Type</th>
                    <th>Department</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {MOCK_LOCATIONS.map((l, i) => (
                    <tr key={i}>
                      <td>{l.name}</td>
                      <td>
                        <Tag type="blue">{l.type}</Tag>
                      </td>
                      <td className="cg-table__muted">{l.department}</td>
                      <td>
                        <Tag type={l.isActive ? "green" : "gray"}>{l.isActive ? "Active" : "Inactive"}</Tag>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TabPanel>

          {/* ── Policy parameters ───────────────────────────────────────── */}
          <TabPanel>
            <MockNotice requirements={["FR-015"]}>
              These thresholds are consumed by the lifecycle rules and by the Policy Compliance Agent's
              deterministic rule engine (§7.6, rules PR-01 to PR-09) — the agent gathers facts, but the
              verdict is always computed from these organisation-configured values, never invented by the
              model.
            </MockNotice>

            <div className="cg-section">
              {MOCK_POLICIES.map((p, i) => (
                <div
                  key={p.label}
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "flex-start",
                    gap: "1.5rem",
                    padding: "0.875rem 1.5rem",
                    borderBottom: i < MOCK_POLICIES.length - 1 ? "1px solid #e0e0e0" : "none",
                  }}
                >
                  <div>
                    <p style={{ margin: 0, fontSize: "0.875rem", fontWeight: 500 }}>{p.label}</p>
                    <p className="cg-table__muted" style={{ margin: "0.15rem 0 0", fontSize: "0.75rem" }}>
                      {p.purpose}
                    </p>
                  </div>
                  <Tag type="high-contrast" size="lg">
                    {p.value}
                  </Tag>
                </div>
              ))}
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
