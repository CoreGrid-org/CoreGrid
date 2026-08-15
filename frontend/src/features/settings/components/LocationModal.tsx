import { useState } from "react";
import { Modal, TextInput, Select, SelectItem, InlineNotification } from "@carbon/react";
import { useCreateLocation, useUpdateLocation } from "../hooks/useOrgConfig";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { Department, Location } from "@/features/assets/types/asset";

interface LocationModalProps {
  location?: Location;
  departments: Department[];
  onClose: () => void;
  onSaved: () => void;
}

// Create or amend a location (FR-011). "Type" is deliberately free text, not
// an enum — SRS system.md §F.6 leaves it unconstrained on purpose.
export default function LocationModal({ location, departments, onClose, onSaved }: LocationModalProps) {
  const [name, setName] = useState(location?.name ?? "");
  const [type, setType] = useState(location?.type ?? "");
  const [departmentId, setDepartmentId] = useState(location?.department_id ?? departments[0]?.id ?? "");

  const createLocation = useCreateLocation();
  const updateLocation = useUpdateLocation();
  const mutation = location ? updateLocation : createLocation;

  const canSubmit = name.trim().length > 0 && type.trim().length > 0 && departmentId.length > 0;

  const handleSubmit = () => {
    if (!canSubmit || mutation.isPending) return;
    const payload = { name: name.trim(), type: type.trim(), department_id: departmentId };
    if (location) {
      updateLocation.mutate({ id: location.id, payload }, { onSuccess: onSaved });
    } else {
      createLocation.mutate(payload, { onSuccess: onSaved });
    }
  };

  return (
    <Modal
      open
      modalLabel="Organisation Settings"
      modalHeading={location ? "Edit location" : "Add location"}
      primaryButtonText={mutation.isPending ? "Saving…" : "Save"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={!canSubmit || mutation.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      {mutation.isError && (
        <InlineNotification
          kind="error"
          title="Could not save location"
          subtitle={getErrorMessage(mutation.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}
      <div style={{ display: "grid", gap: "1rem" }}>
        <TextInput id="location-name" labelText="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <TextInput
          id="location-type"
          labelText="Type"
          helperText="e.g. store, workshop, office, ward — any label your organisation uses."
          value={type}
          onChange={(e) => setType(e.target.value)}
        />
        <Select
          id="location-department"
          labelText="Department"
          value={departmentId}
          onChange={(e) => setDepartmentId(e.target.value)}
        >
          {departments.map((d) => (
            <SelectItem key={d.id} value={d.id} text={d.name} />
          ))}
        </Select>
      </div>
    </Modal>
  );
}
