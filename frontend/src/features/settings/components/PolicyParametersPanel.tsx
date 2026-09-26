import { useState } from "react";
import { Tag, Button, InlineNotification, NumberInput, Search } from "@carbon/react";
import { Add, Edit, Checkmark, Close, ChevronDown, ChevronUp, WarningAltFilled } from "@carbon/icons-react";
import { useOrganizationPolicies, useUpdateOrganizationPolicy } from "../hooks/useOrgConfig";
import PolicyModal from "./PolicyModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { OrganizationPolicy } from "../api/orgConfig";
import { POLICY_FIELDS, POLICY_GROUPS, policyToPayload, policyValueError, type PolicyField } from "../lib/policyFields";

type EditingField = { policyId: string; field: PolicyField; value: number | "" };

export default function PolicyParametersPanel() {
  const policies = useOrganizationPolicies();
  const updatePolicyField = useUpdateOrganizationPolicy();
  const [policyModal, setPolicyModal] = useState<{ policy?: OrganizationPolicy } | null>(null);
  const [editing, setEditing] = useState<EditingField | null>(null);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [notice, setNotice] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  const all = policies.data ?? [];
  const defaultPolicy = all.find((p) => p.asset_type_id === null);
  const overrides = all
    .filter((p) => p.asset_type_id !== null)
    .sort((a, b) => (a.asset_type_name ?? "").localeCompare(b.asset_type_name ?? ""));
  const ordered = defaultPolicy ? [defaultPolicy, ...overrides] : overrides;

  const policyName = (p: OrganizationPolicy) => p.asset_type_name ?? "Organisation-wide default";

  // Search matches a policy by name or tag ("Default" / "Asset type
  // override"), showing all of its fields, or a parameter by label or
  // description, showing just those fields in every policy.
  const query = search.trim().toLowerCase();
  const matches = (text: string) => text.toLowerCase().includes(query);
  const policyMatches = (p: OrganizationPolicy) =>
    matches(policyName(p)) || matches(p.asset_type_id === null ? "default" : "asset type override");
  const results = ordered
    .map((p) => ({
      policy: p,
      fields: new Set(
        POLICY_FIELDS.filter((f) => !query || policyMatches(p) || matches(f.label) || matches(f.purpose)).map((f) => f.key),
      ),
    }))
    .filter((r) => r.fields.size > 0);

  // The default is open unless collapsed; overrides are closed unless opened.
  // While searching, every matching policy opens so the hits are visible.
  const isExpanded = (p: OrganizationPolicy) => (query ? true : (expanded[p.id] ?? p.asset_type_id === null));

  const editingError = editing ? (editing.value === "" ? "Enter a number." : policyValueError(editing.field, editing.value)) : undefined;

  const saveEditing = () => {
    if (!editing || editingError || editing.value === "" || updatePolicyField.isPending) return;
    const policy = all.find((p) => p.id === editing.policyId);
    if (!policy) return;
    const { field, value } = editing;
    updatePolicyField.mutate(
      { id: policy.id, payload: { ...policyToPayload(policy), [field.key]: value } },
      {
        onSuccess: () => {
          setEditing(null);
          setNotice(`${field.label} for ${policyName(policy)} set to ${field.format(value)}.`);
          policies.refetch();
        },
      },
    );
  };

  const startEditing = (policy: OrganizationPolicy, field: PolicyField) => {
    updatePolicyField.reset();
    setEditing({ policyId: policy.id, field, value: policy[field.key] });
  };

  return (
    <div className="cg-settings-list">
      {notice && <InlineNotification kind="success" title={notice} lowContrast onClose={() => setNotice(null)} style={{ maxWidth: "100%" }} />}

      <section className="cg-section">
        <header className="cg-section__header cg-settings-list__header">
          <div>
            <p className="cg-section__title">Policy parameters</p>
            <p className="cg-settings-list__description">
              Thresholds the AI agents and workflow rules use. An asset-type policy overrides the organisation-wide default for that type.
            </p>
          </div>
          <Button size="md" renderIcon={Add} onClick={() => setPolicyModal({})}>
            Add policy
          </Button>
        </header>

        <div className="cg-settings-list__toolbar">
          <Search
            id="policy-search"
            size="md"
            labelText="Search policies"
            placeholder="Search by asset type or parameter, e.g. disposal, confidence"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <p className="cg-settings-list__count">
            {query
              ? `${results.length} of ${ordered.length} ${ordered.length === 1 ? "policy" : "policies"} match`
              : `${ordered.length} ${ordered.length === 1 ? "policy" : "policies"}`}
          </p>
        </div>

        {policies.isError && (
          <InlineNotification
            kind="error"
            title="Could not load policy parameters"
            subtitle={getErrorMessage(policies.error, "Something went wrong. Please try again.")}
            lowContrast
            hideCloseButton
            className="cg-panel-notification cg-panel-notification--inset"
          />
        )}

        {!policies.isLoading && !policies.isError && !defaultPolicy && (
          <div className="cg-modal-form__callout cg-modal-form__callout--danger cg-policy__missing-default">
            <WarningAltFilled size={20} />
            <p>
              There's no organisation-wide default, so asset types without their own policy have no thresholds.{" "}
              <Button kind="ghost" size="sm" onClick={() => setPolicyModal({})}>
                Create the default
              </Button>
            </p>
          </div>
        )}

        {policies.isLoading ? (
          <div className="cg-placeholder">
            <p>Loading policy parameters…</p>
          </div>
        ) : ordered.length === 0 ? null : results.length === 0 ? (
          <div className="cg-placeholder">
            <p>No policies or parameters match "{search.trim()}".</p>
          </div>
        ) : (
          <div className="cg-policy__list">
            {results.map(({ policy: p, fields: shownFields }) => {
              const open = isExpanded(p);
              const isDefault = p.asset_type_id === null;
              const differing = defaultPolicy && !isDefault ? POLICY_FIELDS.filter((f) => p[f.key] !== defaultPolicy[f.key]) : [];
              return (
                <article key={p.id} className="cg-policy">
                  <header className="cg-policy__header">
                    <button
                      type="button"
                      className="cg-policy__toggle"
                      aria-expanded={open}
                      onClick={() => setExpanded((e) => ({ ...e, [p.id]: !open }))}
                    >
                      {open ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                      <span className="cg-policy__name">{policyName(p)}</span>
                      <Tag size="sm" type={isDefault ? "purple" : "blue"}>
                        {isDefault ? "Default" : "Asset type override"}
                      </Tag>
                      {!isDefault && defaultPolicy && (
                        <span className="cg-policy__diff">
                          {differing.length === 0 ? "Same as default" : `${differing.length} differ from default`}
                        </span>
                      )}
                    </button>
                    <Button kind="ghost" size="sm" renderIcon={Edit} onClick={() => setPolicyModal({ policy: p })}>
                      Edit all
                    </Button>
                  </header>

                  {open &&
                    POLICY_GROUPS.filter((group) => group.fields.some((f) => shownFields.has(f.key))).map((group) => (
                      <div key={group.title} className="cg-policy__group">
                        <p className="cg-policy__group-title">{group.title}</p>
                        {group.fields.filter((f) => shownFields.has(f.key)).map((field) => {
                          const isEditingThis = editing?.policyId === p.id && editing.field.key === field.key;
                          const differsFromDefault = Boolean(defaultPolicy && !isDefault && p[field.key] !== defaultPolicy[field.key]);
                          return (
                            <div key={field.key} className="cg-policy-row">
                              <div>
                                <p className="cg-policy-row__label">{field.label}</p>
                                <p className="cg-table__muted cg-policy-row__purpose">{field.purpose}</p>
                              </div>
                              {isEditingThis ? (
                                <div className="cg-policy-row__edit">
                                  <NumberInput
                                    id={`policy-field-${p.id}-${field.key}`}
                                    size="sm"
                                    hideLabel
                                    label={field.label}
                                    hideSteppers
                                    allowEmpty
                                    step={field.step}
                                    min={field.min}
                                    max={field.max}
                                    value={editing.value}
                                    disabled={updatePolicyField.isPending}
                                    onChange={(_e, { value }) =>
                                      setEditing({ ...editing, value: value === "" || value === undefined ? "" : Number(value) })
                                    }
                                    onKeyDown={(e) => {
                                      if (e.key === "Enter") saveEditing();
                                      if (e.key === "Escape") setEditing(null);
                                    }}
                                    invalid={Boolean(editingError)}
                                    invalidText={editingError}
                                    helperText={field.unit ? `${field.unit} · ${field.min}–${field.max}` : `${field.min}–${field.max}`}
                                    autoFocus
                                  />
                                  <Button
                                    kind="primary"
                                    size="sm"
                                    iconDescription="Save"
                                    hasIconOnly
                                    disabled={updatePolicyField.isPending || Boolean(editingError)}
                                    onClick={saveEditing}
                                    renderIcon={Checkmark}
                                  />
                                  <Button
                                    kind="ghost"
                                    size="sm"
                                    iconDescription="Cancel"
                                    hasIconOnly
                                    disabled={updatePolicyField.isPending}
                                    onClick={() => setEditing(null)}
                                    renderIcon={Close}
                                  />
                                </div>
                              ) : (
                                <div className="cg-policy-row__value-wrap">
                                  <div className="cg-policy-row__value-text">
                                    <span className={differsFromDefault ? "cg-policy-row__value is-override" : "cg-policy-row__value"}>
                                      {field.format(p[field.key])}
                                    </span>
                                    {differsFromDefault && defaultPolicy && (
                                      <span className="cg-policy-row__default">Default {field.format(defaultPolicy[field.key])}</span>
                                    )}
                                  </div>
                                  <Button
                                    kind="ghost"
                                    size="sm"
                                    hasIconOnly
                                    renderIcon={Edit}
                                    iconDescription={`Edit ${field.label}`}
                                    tooltipPosition="left"
                                    onClick={() => startEditing(p, field)}
                                  />
                                </div>
                              )}
                            </div>
                          );
                        })}
                      </div>
                    ))}
                </article>
              );
            })}
          </div>
        )}

        {updatePolicyField.isError && (
          <InlineNotification
            kind="error"
            title="Could not save policy parameter"
            subtitle={getErrorMessage(updatePolicyField.error, "Something went wrong. Please try again.")}
            lowContrast
            hideCloseButton
            className="cg-panel-notification cg-panel-notification--inset"
          />
        )}
      </section>

      {policyModal && (
        <PolicyModal
          policy={policyModal.policy}
          existingPolicies={all}
          onClose={() => setPolicyModal(null)}
          onSaved={(name) => {
            setNotice(`${name} policy ${policyModal.policy ? "updated" : "added"}.`);
            setPolicyModal(null);
            policies.refetch();
          }}
        />
      )}
    </div>
  );
}
