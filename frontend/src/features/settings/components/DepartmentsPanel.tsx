import { useMemo, useState } from "react";
import { Tag, OverflowMenu, OverflowMenuItem } from "@carbon/react";
import { useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import type { Department } from "@/features/assets/types/asset";
import { useSetDepartmentActive } from "../hooks/useOrgConfig";
import DepartmentModal from "./DepartmentModal";
import SettingsListSection from "./SettingsListSection";
import StatusConfirmModal from "./StatusConfirmModal";

export default function DepartmentsPanel() {
  const departments = useDepartments();
  const locations = useLocations(undefined);
  const setDepartmentActive = useSetDepartmentActive();

  const [search, setSearch] = useState("");
  const [showInactive, setShowInactive] = useState(true);
  const [editing, setEditing] = useState<{ department?: Department } | null>(null);
  const [statusTarget, setStatusTarget] = useState<Department | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const all = useMemo(() => departments.data ?? [], [departments.data]);
  const locationCounts = useMemo(() => {
    const counts = new Map<string, number>();
    for (const l of locations.data ?? []) {
      if (l.is_active) counts.set(l.department_id, (counts.get(l.department_id) ?? 0) + 1);
    }
    return counts;
  }, [locations.data]);

  const query = search.trim().toLowerCase();
  const visible = all
    .filter((d) => showInactive || d.is_active)
    .filter((d) => !query || d.name.toLowerCase().includes(query) || d.code.toLowerCase().includes(query))
    .sort((a, b) => Number(b.is_active) - Number(a.is_active) || a.name.localeCompare(b.name));

  const confirmStatus = () => {
    if (!statusTarget || setDepartmentActive.isPending) return;
    const target = statusTarget;
    setDepartmentActive.mutate(
      { id: target.id, isActive: !target.is_active },
      {
        onSuccess: () => {
          setStatusTarget(null);
          setNotice(`${target.name} was ${target.is_active ? "deactivated" : "reactivated"}.`);
          departments.refetch();
        },
      },
    );
  };

  return (
    <>
      <SettingsListSection
        title="Departments"
        description="Every asset belongs to a department. The code is used in reports and exports."
        noun="department"
        activeCount={all.filter((d) => d.is_active).length}
        totalCount={all.length}
        search={search}
        onSearchChange={setSearch}
        showInactive={showInactive}
        onShowInactiveChange={setShowInactive}
        addLabel="Add department"
        onAdd={() => setEditing({})}
        isLoading={departments.isLoading}
        error={departments.error}
        isEmpty={visible.length === 0}
        emptyText={all.length === 0 ? "No departments yet. Add the first one to get started." : "No departments match your search."}
        notice={notice}
        onDismissNotice={() => setNotice(null)}
      >
        <table className="cg-table cg-table--no-hover">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Active locations</th>
              <th>Status</th>
              <th aria-label="Actions" />
            </tr>
          </thead>
          <tbody>
            {visible.map((d) => {
              const count = locationCounts.get(d.id) ?? 0;
              return (
                <tr key={d.id} className={d.is_active ? undefined : "cg-table__row--inactive"}>
                  <td>
                    <span className="cg-code-chip">{d.code}</span>
                  </td>
                  <td>{d.name}</td>
                  <td className="cg-table__muted">
                    {count}
                    {count === 0 && d.is_active && (
                      <Tag type="warm-gray" size="sm" className="cg-settings-list__inline-tag">
                        No locations yet
                      </Tag>
                    )}
                  </td>
                  <td>
                    <Tag type={d.is_active ? "green" : "gray"}>{d.is_active ? "Active" : "Inactive"}</Tag>
                  </td>
                  <td style={{ textAlign: "right" }}>
                    <OverflowMenu aria-label={`Actions for ${d.name}`} flipped size="sm">
                      <OverflowMenuItem itemText="Edit" onClick={() => setEditing({ department: d })} />
                      <OverflowMenuItem
                        itemText={d.is_active ? "Deactivate" : "Reactivate"}
                        isDelete={d.is_active}
                        hasDivider
                        onClick={() => {
                          setDepartmentActive.reset();
                          setStatusTarget(d);
                        }}
                      />
                    </OverflowMenu>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </SettingsListSection>

      {editing && (
        <DepartmentModal
          department={editing.department}
          existingCodes={all.filter((d) => d.id !== editing.department?.id).map((d) => d.code)}
          onClose={() => setEditing(null)}
          onSaved={(saved) => {
            setEditing(null);
            setNotice(`${saved.name} was ${editing.department ? "updated" : "added"}.`);
            departments.refetch();
          }}
        />
      )}

      {statusTarget && (
        <StatusConfirmModal
          noun="department"
          name={statusTarget.name}
          isActive={statusTarget.is_active}
          consequence="will no longer be offered when registering or transferring assets. Existing records keep it."
          isPending={setDepartmentActive.isPending}
          error={setDepartmentActive.isError ? setDepartmentActive.error : undefined}
          onConfirm={confirmStatus}
          onClose={() => setStatusTarget(null)}
        />
      )}
    </>
  );
}
