import type { ReactNode } from "react";
import { Tag } from "@carbon/react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import ExecutionTrace from "./ExecutionTrace";
import type { AgentWorkflow } from "../api/workflows";

// Displays a labelled workflow fact.
export function KeyFact({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="cg-workflow-card__fact">
      <p className="cg-workflow-card__fact-label">{label}</p>
      <p className="cg-workflow-card__fact-value">{value}</p>
    </div>
  );
}

// Provides a shared layout for workflow cards.
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
