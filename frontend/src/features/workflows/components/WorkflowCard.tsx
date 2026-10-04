import type { ReactNode } from "react";
import { Tag } from "@carbon/react";
import { Time, User } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import ExecutionTrace from "./ExecutionTrace";
import { workflowTitle } from "../api/workflows";
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
  const assetCount = workflow.fleet?.asset_count ?? workflow.maintenance_analysis?.asset_count;
  const scopeLabel =
    workflow.scope === "ASSET"
      ? `${workflow.asset_type_name} · ${workflow.category_name}`
      : `${workflow.category_name}${assetCount ? ` · ${assetCount} active asset${assetCount === 1 ? "" : "s"}` : ""}`;

  const startedAt = workflow.started_at ?? workflow.created_at;

  return (
    <article className={`cg-workflow-card cg-workflow-card--${workflow.status.toLowerCase()}`}>
      <header className="cg-workflow-card__header">
        <div className="cg-workflow-card__heading">
          <div className="cg-workflow-card__title-row">
            <h3 className="cg-workflow-card__asset">{workflowTitle(workflow)}</h3>
            <Tag size="sm" type={workflow.scope === "ASSET" ? "cool-gray" : "teal"} className="cg-workflow-card__scope">
              {workflow.scope === "ASSET" ? "Single asset" : "Asset type"}
            </Tag>
          </div>
          <p className="cg-workflow-card__meta">
            <span>{scopeLabel}</span>
            {workflow.initiated_by_email && (
              <span className="cg-workflow-card__meta-item">
                <User size={14} /> {workflow.initiated_by_email}
              </span>
            )}
            {startedAt && (
              <span className="cg-workflow-card__meta-item">
                <Time size={14} /> {new Date(startedAt).toLocaleString()}
              </span>
            )}
          </p>
          <p className="cg-workflow-card__objective">“{workflow.objective}”</p>
        </div>
        <div className="cg-workflow-card__tags">
          {workflow.is_high_impact && (
            <Tag type="magenta" size="sm">
              High impact
            </Tag>
          )}
          <Tag type={statusTagColor(workflow.status)}>{formatStatusLabel(workflow.status)}</Tag>
        </div>
      </header>

      <div className="cg-workflow-card__body">{children}</div>

      <ExecutionTrace workflowId={workflow.id} />
    </article>
  );
}
