import { useState } from "react";
import { Modal, TextInput, ComboBox, InlineNotification } from "@carbon/react";
import { useCreateLocation, useUpdateLocation } from "../hooks/useOrgConfig";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import type { Department, Location } from "@/features/assets/types/asset";

interface LocationModalProps {
  location?: Location;
  /** Active departments a location can be placed in. */
  departments: Department[];
  /** Pre-selected department for a new location (e.g. the list's current filter). */
  defaultDepartmentId?: string;
  /** Types other locations already use, offered as suggestions. */
  knownTypes: string[];
  onClose: () => void;
  onSaved: (location: Location) => void;
}

type Touched = Partial<Record<"name" | "type" | "department", boolean>>;

// Create or amend a location. "Type" is deliberately free text, not an enum
// (SRS appendix-f-physical-database-schema.md §F.6 leaves it unconstrained); existing types are suggested
// so the same kind of place gets the same label.
export default function LocationModal({ location, departments, defaultDepartmentId, knownTypes, onClose, onSaved }: LocationModalProps) {
  const [name, setName] = useState(location?.name ?? "");
  const [type, setType] = useState(location?.type ?? "");
  const [departmentId, setDepartmentId] = useState(location?.department_id ?? defaultDepartmentId ?? "");
  const [touched, setTouched] = useState<Touched>({});

  const markTouched = (field: keyof Touched) => setTouched((t) => ({ ...t, [field]: true }));

  const createLocation = useCreateLocation();
  const updateLocation = useUpdateLocation();
  const mutation = location ? updateLocation : createLocation;

  // A location being edited may sit in a since-deactivated department; keep it selectable.
  const departmentOptions =
    location && !departments.some((d) => d.id === location.department_id)
      ? [{ id: location.department_id, code: "", name: `${location.department_name} (inactive)`, is_active: false }, ...departments]
      : departments;
  const selectedDepartment = departmentOptions.find((d) => d.id === departmentId) ?? null;

  // Mirrors Create/UpdateLocationRequest's [Required]/[MaxLength] (backend/Features/OrgConfig/DTOs).
  const nameError = !name.trim() ? "Name is required." : undefined;
  const typeError = !type.trim() ? "Type is required." : type.trim().length > 50 ? "Keep it to 50 characters or fewer." : undefined;
  const departmentError = !departmentId ? "Choose the department it belongs to." : undefined;
  const isUnchanged =
    Boolean(location) &&
    name.trim() === location?.name &&
    type.trim() === location?.type &&
    departmentId === location?.department_id;

  const handleSubmit = () => {
    setTouched({ name: true, type: true, department: true });
    if (nameError || typeError || departmentError || isUnchanged || mutation.isPending) return;
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
      primaryButtonText={mutation.isPending ? "Saving…" : location ? "Save changes" : "Add location"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={mutation.isPending || isUnchanged}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <div className="cg-modal-form">
        {mutation.isError && (
          <InlineNotification
            kind="error"
            title="Could not save location"
            subtitle={getErrorMessage(mutation.error, "Something went wrong. Please try again.")}
            hideCloseButton
            lowContrast
            style={{ maxWidth: "100%" }}
          />
        )}

        <TextInput
          id="location-name"
          labelText="Name"
          placeholder="e.g. Main Store, Ward 3"
          maxLength={200}
          value={name}
          onChange={(e) => setName(e.target.value)}
          onBlur={() => markTouched("name")}
          invalid={Boolean(touched.name && nameError)}
          invalidText={nameError}
        />

        <div className="cg-modal-form__row">
          <ComboBox<Department>
            id="location-department"
            titleText="Department"
            placeholder="Type to search departments…"
            autoAlign
            items={departmentOptions}
            itemToString={(d) => d?.name ?? ""}
            selectedItem={selectedDepartment}
            shouldFilterItem={comboBoxFilter(selectedDepartment)}
            onChange={({ selectedItem }) => {
              setDepartmentId(selectedItem?.id ?? "");
              markTouched("department");
            }}
            invalid={Boolean(touched.department && departmentError)}
            invalidText={departmentError}
          />
          <ComboBox<string>
            id="location-type"
            titleText="Type"
            helperText="Pick an existing type or type a new one."
            placeholder="e.g. store, workshop, ward"
            autoAlign
            allowCustomValue
            items={knownTypes}
            selectedItem={knownTypes.includes(type) ? type : null}
            shouldFilterItem={comboBoxFilter(knownTypes.includes(type) ? type : null)}
            onInputChange={(text) => setType(text ?? "")}
            onChange={({ selectedItem, inputValue }) => setType(selectedItem ?? inputValue ?? "")}
            onBlur={() => markTouched("type")}
            invalid={Boolean(touched.type && typeError)}
            invalidText={typeError}
          />
        </div>
      </div>
    </Modal>
  );
}
