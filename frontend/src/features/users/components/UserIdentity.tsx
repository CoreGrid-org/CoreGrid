import { Tag } from "@carbon/react";
import type { CoreGridUser } from "../services/users";
import { initials } from "../lib/roles";

// Avatar initials, name and email, with a "You" tag on the signed-in user's own row.
export default function UserIdentity({ user, isSelf }: { user: CoreGridUser; isSelf: boolean }) {
  return (
    <div className={`cg-user-identity${user.is_active ? "" : " is-inactive"}`}>
      <span className="cg-user-identity__avatar" aria-hidden="true">
        {initials(user.given_name, user.family_name)}
      </span>
      <span className="cg-user-identity__text">
        <span className="cg-user-identity__name">
          {user.given_name} {user.family_name}
          {isSelf && <Tag size="sm" type="cool-gray">You</Tag>}
        </span>
        <span className="cg-user-identity__email">{user.email}</span>
      </span>
    </div>
  );
}
