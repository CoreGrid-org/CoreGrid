import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, Dropdown, Link, InlineNotification } from "@carbon/react";
import { Add, Edit, LogoGithub } from "@carbon/icons-react";
import { useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import { useOrganizationPolicies, useSetDepartmentActive, useSetLocationActive } from "../hooks/useOrgConfig";
import DepartmentModal from "../components/DepartmentModal";
import LocationModal from "../components/LocationModal";
import PolicyModal from "../components/PolicyModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Department, Location } from "@/features/assets/types/asset";
import type { OrganizationPolicy } from "../api/orgConfig";

const REPO_URL = "https://github.com/CoreGrid-org/CoreGrid";

// FR-015 field descriptions — genuinely useful context the backend doesn't
// (and shouldn't) send over the wire; keyed to OrganizationPolicy's fields.
const POLICY_LABELS: Record<keyof Omit<OrganizationPolicy, "id" | "asset_type_id" | "asset_type_name">, { label: string; purpose: string; format: (v: number) => string }> = {
  repair_to_replace_cost_threshold: {
    label: "Repair-to-replace cost threshold",
    purpose: "Above this ratio, Budget Analysis favours REPLACE over REPAIR.",
    format: (v) => v.toFixed(2),
  },
  minimum_service_life_years: {
    label: "Minimum service life before disposal",
    purpose: "A disposal recommendation requires elapsed service life at or above this.",
    format: (v) => `${v} years`,
  },
  max_acceptable_failure_frequency: {
    label: "Maximum acceptable failure frequency",
    purpose: "Feeds the Maintenance Analysis Agent's cost-trend assessment.",
    format: (v) => `${v} / year`,
  },
  valuation_validity_window_days: {
    label: "Valuation validity window",
    purpose: "A disposal valuation older than this forces NEEDS_REVISION.",
    format: (v) => `${v} days`,
  },
  confidence_floor: {
    label: "Confidence floor",
    purpose: "Below this, human review is forced regardless of the recommended action.",
    format: (v) => v.toFixed(2),
  },
  cost_variance_tolerance_percent: {
    label: "Cost variance tolerance",
    purpose: "Maintenance completion is rejected above this without a recorded justification.",
    format: (v) => `${v}%`,
  },
  outstanding_transfer_days: {
    label: "Outstanding transfer threshold",
    purpose: "An approved but unconfirmed transfer is flagged on the dashboard past this.",
    format: (v) => `${v} days`,
  },
  approval_overdue_period_hours: {
    label: "Approval overdue period",
    purpose: "A workflow awaiting approval past this is surfaced as overdue.",
    format: (v) => `${v} hours`,
  },
};

export default function SettingsPage() {
  const departments = useDepartments();
  const locations = useLocations(undefined);
  const policies = useOrganizationPolicies();

  const setDepartmentActive = useSetDepartmentActive();
  const setLocationActive = useSetLocationActive();

  const [departmentModal, setDepartmentModal] = useState<{ department?: Department } | null>(null);
  const [locationModal, setLocationModal] = useState<{ location?: Location } | null>(null);
  const [policyModal, setPolicyModal] = useState<{ policy?: OrganizationPolicy } | null>(null);

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
          <Tab>About</Tab>
        </TabList>
        <TabPanels>
          {/* ── Departments ─────────────────────────────────────────────── */}
          <TabPanel>
            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Departments</p>
                <Button kind="ghost" size="sm" renderIcon={Add} onClick={() => setDepartmentModal({})}>
                  Add department
                </Button>
              </div>

              {departments.isError && (
                <InlineNotification
                  kind="error"
                  title="Could not load departments"
                  subtitle={getErrorMessage(departments.error, "Something went wrong. Please try again.")}
                  lowContrast
                  hideCloseButton
                  style={{ margin: "1rem 1.5rem", maxWidth: "calc(100% - 3rem)" }}
                />
              )}

              {departments.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading departments…</p>
                </div>
              ) : departments.data && departments.data.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Code</th>
                      <th>Name</th>
                      <th>Status</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {departments.data.map((d) => (
                      <tr key={d.id}>
                        <td className="cg-table__mono">{d.code}</td>
                        <td>{d.name}</td>
                        <td>
                          <Tag type={d.is_active ? "green" : "gray"}>{d.is_active ? "Active" : "Inactive"}</Tag>
                        </td>
                        <td style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
                          <Button kind="ghost" size="sm" onClick={() => setDepartmentModal({ department: d })}>
                            <Edit size={16} />
                          </Button>
                          <Button
                            kind="ghost"
                            size="sm"
                            disabled={setDepartmentActive.isPending}
                            onClick={() =>
                              setDepartmentActive.mutate(
                                { id: d.id, isActive: !d.is_active },
                                { onSuccess: () => departments.refetch() },
                              )
                            }
                          >
                            {d.is_active ? "Deactivate" : "Activate"}
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No departments yet. Add the first one to get started.</p>
                </div>
              )}
            </div>
            {setDepartmentActive.isError && (
              <InlineNotification
                kind="error"
                title="Could not update department"
                subtitle={getErrorMessage(setDepartmentActive.error, "It may still have active assets assigned to it.")}
                lowContrast
                hideCloseButton
                style={{ marginTop: "1rem", maxWidth: "100%" }}
              />
            )}
          </TabPanel>

          {/* ── Locations ───────────────────────────────────────────────── */}
          <TabPanel>
            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Locations</p>
                <Button
                  kind="ghost"
                  size="sm"
                  renderIcon={Add}
                  disabled={!departments.data?.length}
                  onClick={() => setLocationModal({})}
                >
                  Add location
                </Button>
              </div>

              {locations.isError && (
                <InlineNotification
                  kind="error"
                  title="Could not load locations"
                  subtitle={getErrorMessage(locations.error, "Something went wrong. Please try again.")}
                  lowContrast
                  hideCloseButton
                  style={{ margin: "1rem 1.5rem", maxWidth: "calc(100% - 3rem)" }}
                />
              )}

              {locations.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading locations…</p>
                </div>
              ) : locations.data && locations.data.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Type</th>
                      <th>Department</th>
                      <th>Status</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {locations.data.map((l) => (
                      <tr key={l.id}>
                        <td>{l.name}</td>
                        <td>
                          <Tag type="blue">{l.type}</Tag>
                        </td>
                        <td className="cg-table__muted">{l.department_name}</td>
                        <td>
                          <Tag type={l.is_active ? "green" : "gray"}>{l.is_active ? "Active" : "Inactive"}</Tag>
                        </td>
                        <td style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
                          <Button kind="ghost" size="sm" onClick={() => setLocationModal({ location: l })}>
                            <Edit size={16} />
                          </Button>
                          <Button
                            kind="ghost"
                            size="sm"
                            disabled={setLocationActive.isPending}
                            onClick={() =>
                              setLocationActive.mutate(
                                { id: l.id, isActive: !l.is_active },
                                { onSuccess: () => locations.refetch() },
                              )
                            }
                          >
                            {l.is_active ? "Deactivate" : "Activate"}
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No locations yet. Add the first one to get started.</p>
                </div>
              )}
            </div>
            {setLocationActive.isError && (
              <InlineNotification
                kind="error"
                title="Could not update location"
                subtitle={getErrorMessage(setLocationActive.error, "It may still have active assets assigned to it.")}
                lowContrast
                hideCloseButton
                style={{ marginTop: "1rem", maxWidth: "100%" }}
              />
            )}
          </TabPanel>

          {/* ── Policy parameters ───────────────────────────────────────── */}
          <TabPanel>
            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Policy Parameters</p>
                <Button kind="ghost" size="sm" renderIcon={Add} onClick={() => setPolicyModal({})}>
                  Add policy
                </Button>
              </div>

              {policies.isError && (
                <InlineNotification
                  kind="error"
                  title="Could not load policy parameters"
                  subtitle={getErrorMessage(policies.error, "Something went wrong. Please try again.")}
                  lowContrast
                  hideCloseButton
                  style={{ margin: "1rem 1.5rem", maxWidth: "calc(100% - 3rem)" }}
                />
              )}

              {policies.isLoading ? (
                <div className="cg-placeholder">
                  <p>Loading policy parameters…</p>
                </div>
              ) : policies.data && policies.data.length > 0 ? (
                policies.data.map((p, pi) => (
                  <div key={p.id} style={{ borderBottom: pi < policies.data!.length - 1 ? "1px solid #e0e0e0" : "none" }}>
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        padding: "0.875rem 1.5rem 0.5rem",
                      }}
                    >
                      <p style={{ margin: 0, fontSize: "0.8125rem", fontWeight: 600 }}>
                        {p.asset_type_name ?? "Organisation-wide default"}
                      </p>
                      <Button kind="ghost" size="sm" onClick={() => setPolicyModal({ policy: p })}>
                        <Edit size={16} />
                      </Button>
                    </div>
                    {(Object.keys(POLICY_LABELS) as Array<keyof typeof POLICY_LABELS>).map((key) => (
                      <div
                        key={key}
                        style={{
                          display: "flex",
                          justifyContent: "space-between",
                          alignItems: "flex-start",
                          gap: "1.5rem",
                          padding: "0.625rem 1.5rem",
                        }}
                      >
                        <div>
                          <p style={{ margin: 0, fontSize: "0.875rem" }}>{POLICY_LABELS[key].label}</p>
                          <p className="cg-table__muted" style={{ margin: "0.15rem 0 0", fontSize: "0.75rem" }}>
                            {POLICY_LABELS[key].purpose}
                          </p>
                        </div>
                        <Tag type="high-contrast" size="lg">
                          {POLICY_LABELS[key].format(p[key])}
                        </Tag>
                      </div>
                    ))}
                  </div>
                ))
              ) : (
                <div className="cg-placeholder">
                  <p>No policies configured yet. Add the organisation-wide default to get started.</p>
                </div>
              )}
            </div>
          </TabPanel>

          {/* ── About ───────────────────────────────────────────────────── */}
          <TabPanel>
            <div className="cg-section">
              <div className="cg-section__header">
                <p className="cg-section__title">Language</p>
              </div>
              <div style={{ padding: "1.25rem 1.5rem" }}>
                <Dropdown
                  id="settings-language"
                  titleText="Display language"
                  helperText="CoreGrid is currently available in English only."
                  label="English (US)"
                  items={["English (US)"]}
                  selectedItem="English (US)"
                  disabled
                  onChange={() => {}}
                />
              </div>
            </div>

            <div className="cg-section" style={{ marginTop: "1rem" }}>
              <div className="cg-section__header">
                <p className="cg-section__title">About CoreGrid</p>
              </div>
              <div style={{ padding: "1.25rem 1.5rem", display: "flex", flexDirection: "column", gap: "1rem" }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <p style={{ margin: 0, fontSize: "0.875rem" }}>Version</p>
                  <Tag type="high-contrast" size="lg">{`v${__APP_VERSION__}`}</Tag>
                </div>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <p style={{ margin: 0, fontSize: "0.875rem" }}>Source code</p>
                  <Link href={REPO_URL} target="_blank" rel="noreferrer" renderIcon={LogoGithub}>
                    CoreGrid-org/CoreGrid
                  </Link>
                </div>
              </div>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>

      {departmentModal && (
        <DepartmentModal
          department={departmentModal.department}
          onClose={() => setDepartmentModal(null)}
          onSaved={() => {
            setDepartmentModal(null);
            departments.refetch();
          }}
        />
      )}

      {locationModal && (
        <LocationModal
          location={locationModal.location}
          departments={departments.data ?? []}
          onClose={() => setLocationModal(null)}
          onSaved={() => {
            setLocationModal(null);
            locations.refetch();
          }}
        />
      )}

      {policyModal && (
        <PolicyModal
          policy={policyModal.policy}
          onClose={() => setPolicyModal(null)}
          onSaved={() => {
            setPolicyModal(null);
            policies.refetch();
          }}
        />
      )}
    </div>
  );
}
