import { TileGroup, RadioTile } from "@carbon/react";
import { getRoleLabel, type CoreGridRole } from "@/features/auth/lib/roles";
import { ASSIGNABLE_ROLES, ROLE_INFO } from "../lib/roles";

// Role choice as tiles, each with a one-line description of what it can do.
export default function RolePicker({
  name,
  value,
  onChange,
}: {
  name: string;
  value: CoreGridRole;
  onChange: (role: CoreGridRole) => void;
}) {
  return (
    <fieldset className="cg-modal-form__tiles">
      <legend className="cds--label">Role</legend>
      <TileGroup name={name} legend="" valueSelected={value} onChange={(v) => onChange(v as CoreGridRole)}>
        {ASSIGNABLE_ROLES.map((role) => (
          <RadioTile key={role} id={`${name}-${role}`} value={role}>
            <span className="cg-modal-form__tile">
              <span>
                <span className="cg-modal-form__tile-title">{getRoleLabel(role)}</span>
                <span className="cg-modal-form__tile-text">{ROLE_INFO[role].summary}</span>
              </span>
            </span>
          </RadioTile>
        ))}
      </TileGroup>
    </fieldset>
  );
}
