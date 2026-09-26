import type { ReactNode } from "react";
import type { TransferResponse } from "../types";
import { ActorCell, AssetCell, StatusCell } from "./tableCells";
import { formatDate } from "../lib/format";

const place = (department: string | null, location: string | null) =>
  `${department || "-"}${location ? ` (${location})` : ""}`;

interface TransfersTableProps {
  transfers: TransferResponse[];
  /** Adds approved-by / confirmed-by / reason columns (auditor view). */
  showAuditTrail?: boolean;
  /** Renders the trailing Action column when given. */
  renderAction?: (transfer: TransferResponse) => ReactNode;
}

export default function TransfersTable({ transfers, showAuditTrail = false, renderAction }: TransfersTableProps) {
  return (
    <table className="cg-table cg-table--no-hover">
      <thead>
        <tr>
          <th>Asset</th>
          <th>From</th>
          <th>To</th>
          <th>Status</th>
          <th>Requested by</th>
          {showAuditTrail && <th>Approved by</th>}
          {showAuditTrail && <th>Confirmed by</th>}
          <th>Requested</th>
          {showAuditTrail && <th>Reason</th>}
          {renderAction && <th style={{ textAlign: "right" }}>Action</th>}
        </tr>
      </thead>
      <tbody>
        {transfers.map((t) => (
          <tr key={t.id}>
            <AssetCell code={t.asset_code} name={t.asset_name} />
            <td className="cg-table__muted">{place(t.from_department_name, t.from_location_name)}</td>
            <td className="cg-table__muted">{place(t.to_department_name, t.to_location_name)}</td>
            <StatusCell status={t.status} />
            <td className="cg-table__muted">{t.initiated_by_user_email || "-"}</td>
            {showAuditTrail && <ActorCell email={t.approved_by_user_email} at={t.approved_at} />}
            {showAuditTrail && <ActorCell email={t.confirmed_by_user_email} at={t.confirmed_at} />}
            <td className="cg-table__muted">{formatDate(t.requested_at)}</td>
            {showAuditTrail && <td className="cg-table__muted cg-table__note">{t.rejection_reason || "-"}</td>}
            {renderAction && <td style={{ textAlign: "right" }}>{renderAction(t)}</td>}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
