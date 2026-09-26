import type { ReactNode } from "react";
import { formatStatusLabel } from "@/shared/lib/statusTag";
import type { DisposalResponse } from "../types";
import { ActorCell, AssetCell, StatusCell } from "./tableCells";
import { formatDate, formatLkr } from "../lib/format";

interface DisposalsTableProps {
  disposals: DisposalResponse[];
  /** Adds the approved-by column (auditor view). */
  showAuditTrail?: boolean;
  /** Adds the notes / revision comments column. */
  showNotes?: boolean;
  /** Renders the trailing Actions column when given. */
  renderAction?: (disposal: DisposalResponse) => ReactNode;
}

export default function DisposalsTable({ disposals, showAuditTrail = false, showNotes = false, renderAction }: DisposalsTableProps) {
  return (
    <table className="cg-table cg-table--no-hover">
      <thead>
        <tr>
          <th>Asset</th>
          <th>Proposed method</th>
          <th>Status</th>
          <th>Estimated residual value</th>
          <th>Requested by</th>
          {showAuditTrail && <th>Approved by</th>}
          {showNotes && <th>Notes / revision</th>}
          <th>Requested</th>
          {renderAction && <th style={{ textAlign: "right" }}>Actions</th>}
        </tr>
      </thead>
      <tbody>
        {disposals.map((d) => (
          <tr key={d.id}>
            <AssetCell code={d.asset_code} name={d.asset_name} />
            <td className="cg-table__muted">{formatStatusLabel(d.disposal_method)}</td>
            <StatusCell status={d.status} />
            <td className="cg-table__muted">{formatLkr(d.estimated_residual_value)}</td>
            <td className="cg-table__muted">{d.initiated_by_user_email || "-"}</td>
            {showAuditTrail && <ActorCell email={d.approved_by_user_email} at={d.approved_at} />}
            {showNotes && <td className="cg-table__muted cg-table__note">{d.notes || "-"}</td>}
            <td className="cg-table__muted">{formatDate(d.requested_at)}</td>
            {renderAction && <td style={{ textAlign: "right" }}>{renderAction(d)}</td>}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
