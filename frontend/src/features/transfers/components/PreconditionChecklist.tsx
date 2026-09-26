import { InlineNotification } from "@carbon/react";
import { CheckmarkFilled, CloseFilled } from "@carbon/icons-react";
import type { DisposalPreconditionResult } from "../types";

// The disposal precondition checks (P1–P6) plus the separation-of-duties
// check, as evaluated by the backend for one disposal request.
export default function PreconditionChecklist({ evaluation }: { evaluation: DisposalPreconditionResult }) {
  return (
    <div className="cg-checklist">
      {!evaluation.separation_of_duties_passed && (
        <InlineNotification
          kind="warning"
          title="Separation of duties"
          subtitle={evaluation.separation_of_duties_failure_reason || "Approver cannot be the requester of the disposal."}
          lowContrast
          hideCloseButton
          style={{ maxWidth: "100%" }}
        />
      )}

      <ul className="cg-checklist__items">
        {(evaluation.checks ?? []).map((check) => (
          <li key={check.code} className={check.passed ? "is-passed" : "is-failed"}>
            {check.passed ? <CheckmarkFilled size={18} /> : <CloseFilled size={18} />}
            <div>
              <span className="cg-checklist__code">{check.code}</span>
              <span className="cg-checklist__text">{check.description}</span>
              {!check.passed && check.failure_reason && <p className="cg-checklist__reason">{check.failure_reason}</p>}
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
