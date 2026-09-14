import { useState } from "react";
import { Tag, Button, InlineNotification } from "@carbon/react";
import { Add, Edit } from "@carbon/icons-react";
import { useDepartments } from "@/features/assets/hooks/useAssets";
import { useSetDepartmentActive } from "../hooks/useOrgConfig";
import DepartmentModal from "./DepartmentModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Department } from "@/features/assets/types/asset";

export default function DepartmentsPanel() {
  const departments = useDepartments();
  const setDepartmentActive = useSetDepartmentActive();
  const [departmentModal, setDepartmentModal] = useState<{ department?: Department } | null>(null);

  return (
    <>
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
            className="cg-panel-notification cg-panel-notification--inset"
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
                  <td className="cg-row-actions">
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
          className="cg-panel-notification"
        />
      )}

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
    </>
  );
}
