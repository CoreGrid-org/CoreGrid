import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, Dropdown, Link, InlineNotification, NumberInput, Accordion, AccordionItem } from "@carbon/react";
import { Add, Edit, LogoGithub, Checkmark, Close } from "@carbon/icons-react";
import { useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import { useOrganizationPolicies, useSetDepartmentActive, useSetLocationActive, useUpdateOrganizationPolicy } from "../hooks/useOrgConfig";
import DepartmentModal from "../components/DepartmentModal";
import LocationModal from "../components/LocationModal";
import PolicyModal from "../components/PolicyModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Department, Location } from "@/features/assets/types/asset";
import type { OrganizationPolicy, SaveOrganizationPolicyRequest } from "../api/orgConfig";

const REPO_URL = "https://github.com/CoreGrid-org/CoreGrid";

type PolicyFieldKey = keyof Omit<OrganizationPolicy, "id" | "asset_type_id" | "asset_type_name">;

// FR-015 field descriptions — genuinely useful context the backend doesn't
// (and shouldn't) send over the wire; keyed to OrganizationPolicy's fields.
const POLICY_LABELS: Record<PolicyFieldKey, { label: string; purpose: string; format: (v: number) => string; step: number }> = {
  repair_to_replace_cost_threshold: {
    label: "Repair-to-replace cost threshold",
    purpose: "Above this ratio, Budget Analysis favours REPLACE over REPAIR.",
    format: (v) => v.toFixed(2),
    step: 0.01,
  },
  minimum_service_life_years: {
    label: "Minimum service life before disposal",
    purpose: "A disposal recommendation requires elapsed service life at or above this.",
    format: (v) => `${v} years`,
    step: 1,
  },
  max_acceptable_failure_frequency: {
    label: "Maximum acceptable failure frequency",
    purpose: "Feeds the Maintenance Analysis Agent's cost-trend assessment.",
    format: (v) => `${v} / year`,
    step: 1,
  },
  valuation_validity_window_days: {
    label: "Valuation validity window",
    purpose: "A disposal valuation older than this forces NEEDS_REVISION.",
    format: (v) => `${v} days`,
    step: 1,
  },
  confidence_floor: {
    label: "Confidence floor",
    purpose: "Below this, human review is forced regardless of the recommended action.",
    format: (v) => v.toFixed(2),
    step: 0.01,
  },
  cost_variance_tolerance_percent: {
    label: "Cost variance tolerance",
    purpose: "Maintenance completion is rejected above this without a recorded justification.",
    format: (v) => `${v}%`,
    step: 1,
  },
  outstanding_transfer_days: {
    label: "Outstanding transfer threshold",
    purpose: "An approved but unconfirmed transfer is flagged on the dashboard past this.",
    format: (v) => `${v} days`,
    step: 1,
  },
  approval_overdue_period_hours: {
    label: "Approval overdue period",
    purpose: "A workflow awaiting approval past this is surfaced as overdue.",
    format: (v) => `${v} hours`,
    step: 1,
  },
};

function policyToPayload(p: OrganizationPolicy): SaveOrganizationPolicyRequest {
  return {
    asset_type_id: p.asset_type_id,
    repair_to_replace_cost_threshold: p.repair_to_replace_cost_threshold,
    minimum_service_life_years: p.minimum_service_life_years,
    max_acceptable_failure_frequency: p.max_acceptable_failure_frequency,
    valuation_validity_window_days: p.valuation_validity_window_days,
    confidence_floor: p.confidence_floor,
    cost_variance_tolerance_percent: p.cost_variance_tolerance_percent,
    outstanding_transfer_days: p.outstanding_transfer_days,
    approval_overdue_period_hours: p.approval_overdue_period_hours,
  };
}

export default function SettingsPage() {
  const departments = useDepartments();
  const locations = useLocations(undefined);
  const policies = useOrganizationPolicies();

  const setDepartmentActive = useSetDepartmentActive();
  const setLocationActive = useSetLocationActive();
  const updatePolicyField = useUpdateOrganizationPolicy();

  const [departmentModal, setDepartmentModal] = useState<{ department?: Department } | null>(null);
  const [locationModal, setLocationModal] = useState<{ location?: Location } | null>(null);
  const [policyModal, setPolicyModal] = useState<{ policy?: OrganizationPolicy } | null>(null);
  const [editingField, setEditingField] = useState<{ policyId: string; key: PolicyFieldKey; value: number } | null>(null);

  const saveEditingField = () => {
    if (!editingField || updatePolicyField.isPending) return;
    const policy = policies.data?.find((p) => p.id === editingField.policyId);
    if (!policy) return;
    updatePolicyField.mutate(
      { id: policy.id, payload: { ...policyToPayload(policy), [editingField.key]: editingField.value } },
      {
        onSuccess: () => {
          setEditingField(null);
          policies.refetch();
        },
      },
    );
  };

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Organisation Settings</h1>
          <p className="cg-page__subtitle">Departments, locations and policy thresholds.</p>
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
                <Accordion>
                  {policies.data.map((p) => (
                    <AccordionItem key={p.id} title={p.asset_type_name ?? "Organisation-wide default"}>
                      <div style={{ display: "flex", justifyContent: "flex-end", padding: "0 0 0.5rem" }}>
                        <Button kind="ghost" size="sm" onClick={() => setPolicyModal({ policy: p })}>
                          <Edit size={16} />
                          &nbsp;Edit all
                        </Button>
                      </div>
                      {(Object.keys(POLICY_LABELS) as PolicyFieldKey[]).map((key) => {
                        const isEditingThis = editingField?.policyId === p.id && editingField.key === key;
                        return (
                          <div
                            key={key}
                            style={{
                              display: "flex",
                              justifyContent: "space-between",
                              alignItems: "flex-start",
                              gap: "1.5rem",
                              padding: "0.625rem 0",
                              borderTop: "1px solid #e0e0e0",
                            }}
                          >
                            <div>
                              <p style={{ margin: 0, fontSize: "0.875rem" }}>{POLICY_LABELS[key].label}</p>
                              <p className="cg-table__muted" style={{ margin: "0.15rem 0 0", fontSize: "0.75rem" }}>
                                {POLICY_LABELS[key].purpose}
                              </p>
                            </div>
                            {isEditingThis ? (
                              <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
                                <NumberInput
                                  id={`policy-field-${p.id}-${key}`}
                                  size="sm"
                                  hideLabel
                                  label={POLICY_LABELS[key].label}
                                  step={POLICY_LABELS[key].step}
                                  value={editingField.value}
                                  disabled={updatePolicyField.isPending}
                                  onChange={(_e, { value }) => setEditingField({ ...editingField, value: Number(value) })}
                                  style={{ width: "8rem" }}
                                />
                                <Button
                                  kind="ghost"
                                  size="sm"
                                  iconDescription="Save"
                                  hasIconOnly
                                  disabled={updatePolicyField.isPending}
                                  onClick={saveEditingField}
                                  renderIcon={Checkmark}
                                />
                                <Button
                                  kind="ghost"
                                  size="sm"
                                  iconDescription="Cancel"
                                  hasIconOnly
                                  disabled={updatePolicyField.isPending}
                                  onClick={() => setEditingField(null)}
                                  renderIcon={Close}
                                />
                              </div>
                            ) : (
                              <Tag
                                type="high-contrast"
                                size="lg"
                                style={{ cursor: "pointer" }}
                                onClick={() => setEditingField({ policyId: p.id, key, value: p[key] })}
                              >
                                {POLICY_LABELS[key].format(p[key])}
                              </Tag>
                            )}
                          </div>
                        );
                      })}
                    </AccordionItem>
                  ))}
                </Accordion>
              ) : (
                <div className="cg-placeholder">
                  <p>No policies configured yet. Add the organisation-wide default to get started.</p>
                </div>
              )}
            </div>
            {updatePolicyField.isError && (
              <InlineNotification
                kind="error"
                title="Could not save policy parameter"
                subtitle={getErrorMessage(updatePolicyField.error, "Something went wrong. Please try again.")}
                lowContrast
                hideCloseButton
                style={{ marginTop: "1rem", maxWidth: "100%" }}
              />
            )}
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
