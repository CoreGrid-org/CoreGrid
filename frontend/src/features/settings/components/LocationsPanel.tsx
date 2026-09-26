import { useMemo, useState } from "react";
import { Tag, ComboBox, OverflowMenu, OverflowMenuItem } from "@carbon/react";
import { useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import type { Department, Location } from "@/features/assets/types/asset";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { useSetLocationActive } from "../hooks/useOrgConfig";
import LocationModal from "./LocationModal";
import SettingsListSection from "./SettingsListSection";
import StatusConfirmModal from "./StatusConfirmModal";

export default function LocationsPanel() {
  const departments = useDepartments();
  const locations = useLocations(undefined);
  const setLocationActive = useSetLocationActive();

  const [search, setSearch] = useState("");
  const [department, setDepartment] = useState<Department | null>(null);
  const [showInactive, setShowInactive] = useState(true);
  const [editing, setEditing] = useState<{ location?: Location } | null>(null);
  const [statusTarget, setStatusTarget] = useState<Location | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const all = useMemo(() => locations.data ?? [], [locations.data]);
  const activeDepartments = (departments.data ?? []).filter((d) => d.is_active);
  // Types already in use, offered as suggestions so labels stay consistent.
  const knownTypes = useMemo(
    () => [...new Set(all.map((l) => l.type.trim()).filter(Boolean))].sort((a, b) => a.localeCompare(b)),
    [all],
  );

  const query = search.trim().toLowerCase();
  const visible = all
    .filter((l) => showInactive || l.is_active)
    .filter((l) => !department || l.department_id === department.id)
    .filter(
      (l) =>
        !query ||
        l.name.toLowerCase().includes(query) ||
        l.type.toLowerCase().includes(query) ||
        l.department_name.toLowerCase().includes(query),
    )
    .sort(
      (a, b) =>
        Number(b.is_active) - Number(a.is_active) ||
        a.department_name.localeCompare(b.department_name) ||
        a.name.localeCompare(b.name),
    );

  const confirmStatus = () => {
    if (!statusTarget || setLocationActive.isPending) return;
    const target = statusTarget;
    setLocationActive.mutate(
      { id: target.id, isActive: !target.is_active },
      {
        onSuccess: () => {
          setStatusTarget(null);
          setNotice(`${target.name} was ${target.is_active ? "deactivated" : "reactivated"}.`);
          locations.refetch();
        },
      },
    );
  };

  return (
    <>
      <SettingsListSection
        title="Locations"
        description="Where assets are kept. Each location belongs to one department."
        noun="location"
        activeCount={all.filter((l) => l.is_active).length}
        totalCount={all.length}
        search={search}
        onSearchChange={setSearch}
        showInactive={showInactive}
        onShowInactiveChange={setShowInactive}
        filters={
          <ComboBox<Department>
            id="locations-department-filter"
            aria-label="Filter by department"
            placeholder="All departments"
            items={departments.data ?? []}
            itemToString={(d) => d?.name ?? ""}
            selectedItem={department}
            shouldFilterItem={comboBoxFilter(department)}
            onChange={({ selectedItem }) => setDepartment(selectedItem ?? null)}
            size="md"
            className="cg-settings-list__filter"
          />
        }
        addLabel="Add location"
        addDisabledReason={
          !departments.isLoading && activeDepartments.length === 0 ? "Add an active department first; every location belongs to one." : undefined
        }
        onAdd={() => setEditing({})}
        isLoading={locations.isLoading}
        error={locations.error}
        isEmpty={visible.length === 0}
        emptyText={all.length === 0 ? "No locations yet. Add the first one to get started." : "No locations match these filters."}
        notice={notice}
        onDismissNotice={() => setNotice(null)}
      >
        <table className="cg-table cg-table--no-hover">
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Department</th>
              <th>Status</th>
              <th aria-label="Actions" />
            </tr>
          </thead>
          <tbody>
            {visible.map((l) => (
              <tr key={l.id} className={l.is_active ? undefined : "cg-table__row--inactive"}>
                <td>{l.name}</td>
                <td>
                  <Tag type="cool-gray" size="sm">
                    {l.type}
                  </Tag>
                </td>
                <td className="cg-table__muted">{l.department_name}</td>
                <td>
                  <Tag type={l.is_active ? "green" : "gray"}>{l.is_active ? "Active" : "Inactive"}</Tag>
                </td>
                <td style={{ textAlign: "right" }}>
                  <OverflowMenu aria-label={`Actions for ${l.name}`} flipped size="sm">
                    <OverflowMenuItem itemText="Edit" onClick={() => setEditing({ location: l })} />
                    <OverflowMenuItem
                      itemText={l.is_active ? "Deactivate" : "Reactivate"}
                      isDelete={l.is_active}
                      hasDivider
                      onClick={() => {
                        setLocationActive.reset();
                        setStatusTarget(l);
                      }}
                    />
                  </OverflowMenu>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </SettingsListSection>

      {editing && (
        <LocationModal
          location={editing.location}
          departments={activeDepartments}
          defaultDepartmentId={department?.id}
          knownTypes={knownTypes}
          onClose={() => setEditing(null)}
          onSaved={(saved) => {
            setEditing(null);
            setNotice(`${saved.name} was ${editing.location ? "updated" : "added"}.`);
            locations.refetch();
          }}
        />
      )}

      {statusTarget && (
        <StatusConfirmModal
          noun="location"
          name={statusTarget.name}
          isActive={statusTarget.is_active}
          consequence="will no longer be offered when registering or transferring assets. Existing records keep it."
          isPending={setLocationActive.isPending}
          error={setLocationActive.isError ? setLocationActive.error : undefined}
          onConfirm={confirmStatus}
          onClose={() => setStatusTarget(null)}
        />
      )}
    </>
  );
}
