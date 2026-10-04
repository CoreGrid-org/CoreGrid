import { useState } from "react";
import { Button, Tag } from "@carbon/react";
import { CheckmarkFilled, CloseFilled, WarningAltFilled, ArrowRight } from "@carbon/icons-react";
import { formatStatusLabel, statusTagColor } from "@/shared/lib/statusTag";
import { AgentDisplay } from "./agentDisplay";
import type {
  FailureStatistics,
  FinancialAssessment,
  FleetEvaluation,
  LifecycleAction,
  PlannerExecutionPlan,
  PolicyValidation,
} from "../api/workflows";

// Structured renderings of each agent's output, shared by the workflow
// cards and the execution trace so a fact always looks the same.

export const ACTION_ORDER: LifecycleAction[] = ["DISPOSE", "REPLACE", "REPAIR", "TRANSFER", "RETAIN"];

const ACTION_TAG: Record<LifecycleAction, "red" | "magenta" | "blue" | "cyan" | "green"> = {
  DISPOSE: "red",
  REPLACE: "magenta",
  REPAIR: "blue",
  TRANSFER: "cyan",
  RETAIN: "green",
};

const OUTCOME_ICON: Record<string, typeof CheckmarkFilled> = {
  PASS: CheckmarkFilled,
  FAIL: CloseFilled,
  NEEDS_REVISION: WarningAltFilled,
};

export const formatLkr = (value: number) => `LKR ${Math.round(value).toLocaleString()}`;

export function ActionTag({ action, size = "md" }: { action: string; size?: "sm" | "md" }) {
  return (
    <Tag type={ACTION_TAG[action as LifecycleAction] ?? "gray"} size={size}>
      {formatStatusLabel(action)}
    </Tag>
  );
}

function Stat({ label, value, hint }: { label: string; value: React.ReactNode; hint?: string }) {
  return (
    <div className="cg-agent-stat">
      <p className="cg-agent-stat__label">{label}</p>
      <p className="cg-agent-stat__value">{value}</p>
      {hint && <p className="cg-agent-stat__hint">{hint}</p>}
    </div>
  );
}

export function PlanSteps({ plan }: { plan: PlannerExecutionPlan }) {
  if (!plan.inScope) return <p className="cg-agent-output__note">{plan.rejectionReason}</p>;
  return (
    <ol className="cg-plan-steps">
      {plan.steps.map((step) => (
        <li key={step.seq} className="cg-plan-steps__item">
          <span className="cg-plan-steps__agent">
            <AgentDisplay agent={step.agent} size={16} />
          </span>
          <ArrowRight size={14} className="cg-plan-steps__arrow" />
          <span className="cg-plan-steps__purpose">{step.purpose}</span>
        </li>
      ))}
    </ol>
  );
}

export function MaintenanceFacts({ stats }: { stats: FailureStatistics }) {
  const isFleet = stats.asset_count > 1;
  return (
    <div className="cg-agent-stats">
      {isFleet && <Stat label="Assets" value={stats.asset_count} hint={`${stats.assets_with_repairs} with repairs`} />}
      <Stat label="Repairs" value={stats.repair_count} />
      <Stat
        label="Mean time between failures"
        value={stats.mean_time_between_failures_days !== null ? `${stats.mean_time_between_failures_days} days` : "N/A"}
      />
      <Stat label="Cost trend" value={formatStatusLabel(stats.cost_trend)} />
      <Stat label="Projected next 12 months" value={formatLkr(stats.projected_next_twelve_months_cost)} />
    </div>
  );
}

export function BudgetFacts({ assessment }: { assessment: FinancialAssessment }) {
  const top = Math.max(...assessment.ranked_options.map((o) => o.score), 0.01);
  return (
    <>
      <div className="cg-agent-stats">
        <Stat label="Residual value" value={formatLkr(assessment.residual_value)} />
        <Stat label="Projected repairs" value={formatLkr(assessment.projected_repair_cost ?? 0)} />
        <Stat
          label="Repair-to-residual"
          value={assessment.repair_to_replace_ratio !== null ? assessment.repair_to_replace_ratio.toFixed(2) : "N/A"}
        />
        <Stat
          label="Budget headroom"
          value={assessment.budget_headroom !== null ? formatLkr(assessment.budget_headroom) : "Not tracked"}
        />
      </div>
      <div className="cg-option-ranking">
        <p className="cg-workflow-card__section-label">
          Ranked options{" "}
          <Tag size="sm" type={assessment.source === "MODEL" ? "purple" : "gray"}>
            {assessment.source === "MODEL" ? "Model-scored" : "Deterministic"}
          </Tag>
        </p>
        {assessment.ranked_options.map((option) => (
          <div key={option.action} className="cg-option-ranking__row">
            <span className="cg-option-ranking__action">{formatStatusLabel(option.action)}</span>
            <div className="cg-option-ranking__bar" aria-hidden="true">
              <div
                className={`cg-option-ranking__fill cg-option-ranking__fill--${option.action.toLowerCase()}`}
                style={{ width: `${Math.max(4, (option.score / top) * 100)}%` }}
              />
            </div>
            <span className="cg-option-ranking__score">{option.score.toFixed(2)}</span>
            <p className="cg-option-ranking__why">{option.rationale}</p>
          </div>
        ))}
      </div>
    </>
  );
}

export function PolicyRules({ validation }: { validation: PolicyValidation }) {
  // N/A rules are noise to an approver; show what was actually checked.
  const applied = validation.rule_results.filter((r) => r.outcome !== "N/A");
  return (
    <div className="cg-workflow-card__rules">
      {applied.map((r) => {
        const Icon = OUTCOME_ICON[r.outcome];
        return (
          <div key={r.rule_id} className="cg-workflow-card__rule">
            {Icon && <Icon size={16} className={`cg-workflow-card__rule-icon cg-workflow-card__rule-icon--${r.outcome.toLowerCase()}`} />}
            <span className="cg-workflow-card__rule-id">{r.rule_id}</span>
            <span className="cg-workflow-card__rule-values">
              {r.expected} → {r.actual}
            </span>
            <Tag type={r.outcome === "PASS" ? "green" : r.outcome === "FAIL" ? "red" : "gray"} size="sm">
              {r.outcome}
            </Tag>
          </div>
        );
      })}
      {validation.blocking_reasons.map((reason) => (
        <p key={reason} className="cg-agent-output__note">
          {reason}
        </p>
      ))}
    </div>
  );
}

// Stacked bar of how many assets got each permitted action, plus deferrals.
export function ActionMix({ fleet }: { fleet: FleetEvaluation }) {
  const segments = [
    ...ACTION_ORDER.map((action) => ({ key: action, label: formatStatusLabel(action), count: fleet.action_counts[action] ?? 0 })),
    { key: "DEFERRED", label: "Deferred", count: fleet.deferred_count },
    { key: "BLOCKED", label: "Blocked", count: fleet.blocked_count },
  ].filter((s) => s.count > 0);

  return (
    <div className="cg-action-mix">
      <div className="cg-action-mix__bar" role="img" aria-label={segments.map((s) => `${s.count} ${s.label}`).join(", ")}>
        {segments.map((s) => (
          <div
            key={s.key}
            className={`cg-action-mix__segment cg-action-mix__segment--${s.key.toLowerCase()}`}
            style={{ flexGrow: s.count }}
            title={`${s.count} ${s.label}`}
          />
        ))}
      </div>
      <ul className="cg-action-mix__legend">
        {segments.map((s) => (
          <li key={s.key}>
            <span className={`cg-action-mix__swatch cg-action-mix__segment--${s.key.toLowerCase()}`} />
            {s.label} <strong>{s.count}</strong>
          </li>
        ))}
      </ul>
    </div>
  );
}

const FLEET_PREVIEW = 6;

export function FleetTable({ fleet }: { fleet: FleetEvaluation }) {
  const [showAll, setShowAll] = useState(false);
  const rows = showAll ? fleet.assets : fleet.assets.slice(0, FLEET_PREVIEW);

  return (
    <div className="cg-fleet-table">
      <table className="cg-table cg-table--no-hover">
        <thead>
          <tr>
            <th>Asset</th>
            <th>Condition</th>
            <th>Action</th>
            <th>Policy</th>
            <th>Projected</th>
            <th>Why</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((a) => (
            <tr key={a.asset_id}>
              <td className="cg-fleet-table__code">{a.asset_code}</td>
              <td>
                <Tag size="sm" type={statusTagColor(a.condition)}>
                  {formatStatusLabel(a.condition)}
                </Tag>
              </td>
              <td>
                <ActionTag action={a.action} size="sm" />
              </td>
              <td>
                <Tag size="sm" type={a.verdict === "PASS" ? "green" : a.verdict === "FAIL" ? "red" : "warm-gray"}>
                  {a.verdict === "NEEDS_REVISION" ? "Deferred" : formatStatusLabel(a.verdict)}
                </Tag>
              </td>
              <td className="cg-table__muted">{a.projected_cost > 0 ? formatLkr(a.projected_cost) : "N/A"}</td>
              <td className="cg-fleet-table__reason">{a.reason}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {fleet.assets.length > FLEET_PREVIEW && (
        <Button kind="ghost" size="sm" onClick={() => setShowAll((v) => !v)}>
          {showAll ? "Show fewer" : `Show all ${fleet.assets.length} assets`}
        </Button>
      )}
      {fleet.asset_count > fleet.assets.length && (
        <p className="cg-table__muted cg-text-small">
          Showing the {fleet.assets.length} most consequential of {fleet.asset_count} assets.
        </p>
      )}
    </div>
  );
}
