import { Tag } from "@carbon/react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { formatDate } from "../lib/format";

export function AssetCell({ code, name }: { code: string; name: string }) {
  return (
    <td>
      <span className="cg-table__mono">{code}</span>
      <br />
      <span className="cg-table__muted">{name}</span>
    </td>
  );
}

export function StatusCell({ status }: { status: string }) {
  return (
    <td>
      <Tag type={statusTagColor(status)}>{formatStatusLabel(status)}</Tag>
    </td>
  );
}

// "Who did it, and when" for an audit-trail column.
export function ActorCell({ email, at }: { email: string | null; at: string | null }) {
  return (
    <td className="cg-table__muted">
      {email ? (
        <>
          <div>{email}</div>
          {at && <span className="cg-table__subtle">{formatDate(at)}</span>}
        </>
      ) : (
        "-"
      )}
    </td>
  );
}
