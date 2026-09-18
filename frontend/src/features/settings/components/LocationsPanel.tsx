import { useState } from "react";
import { Tag, Button, InlineNotification } from "@carbon/react";
import { Add, Edit } from "@carbon/icons-react";
import { useDepartments, useLocations } from "@/features/assets/hooks/useAssets";
import { useSetLocationActive } from "../hooks/useOrgConfig";
import LocationModal from "./LocationModal";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Location } from "@/features/assets/types/asset";

export default function LocationsPanel() {
  const departments = useDepartments();
  const locations = useLocations(undefined);
  const setLocationActive = useSetLocationActive();
  const [locationModal, setLocationModal] = useState<{ location?: Location } | null>(null);

  return (
    <>
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
            className="cg-panel-notification cg-panel-notification--inset"
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
                  <td className="cg-row-actions">
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
          className="cg-panel-notification"
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
    </>
  );
}
