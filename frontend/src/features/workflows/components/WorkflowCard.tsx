import type { ReactNode } from "react";
import { Tag } from "@carbon/react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import ExecutionTrace from "./ExecutionTrace";
import type { AgentWorkflow } from "../api/workflows";

// A single labelled fact — reused across all three tabs' body content
// (repair count, cost trend, started date, recommendation, ...) so every
// card presents its key facts in the same visual language.
export function KeyFact({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="cg-workflow-card__fact">
      <p className="cg-workflow-card__fact-label">{label}</p>
      <p className="cg-workflow-card__fact-value">{value}</p>
    </div>
  );
}

// One shared shell for every workflow across all three Active/Awaiting
// Approval/Completed tabs — same header (asset, objective, status), same
// execution-trace footer, regardless of state. Only the body in between
// (passed as children) differs, because Active/Awaiting/Completed
// genuinely show different facts about a workflow — forcing identical
// fields there would hide information, not simplify the design.
export default function WorkflowCard({ workflow, children }: { workflow: AgentWorkflow; children: ReactNode }) {
  return (
    <div className="cg-workflow-card">
      <div className="cg-workflow-card__header">
        <div>
          <p className="cg-workflow-card__asset">{workflow.asset_code}</p>
          <p className="cg-workflow-card__objective">{workflow.objective}</p>
        </div>
        <div className="cg-workflow-card__tags">
          {workflow.is_high_impact && (
            <Tag type="magenta" size="sm">
              High impact
            </Tag>
          )}
          <Tag type={statusTagColor(workflow.status)}>{formatStatusLabel(workflow.status)}</Tag>
        </div>
      </div>

      <div className="cg-workflow-card__body">{children}</div>

      <ExecutionTrace workflowId={workflow.id} />
    </div>
  );
}
