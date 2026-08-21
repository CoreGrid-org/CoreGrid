import { useState } from "react";
import { Modal, Select, SelectItem, NumberInput, InlineNotification } from "@carbon/react";
import { useEvaluatePolicy } from "../hooks/useWorkflows";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { AgentWorkflow } from "../api/workflows";

const RECOMMENDATIONS = ["REPAIR", "REPLACE", "TRANSFER", "DISPOSE", "RETAIN"];

interface EvaluatePolicyModalProps {
  workflow: AgentWorkflow;
  onClose: () => void;
  onEvaluated: (workflow: AgentWorkflow) => void;
}

// Stands in for nodes 2-4 (Maintenance Analysis, Budget Analysis, Policy
// Compliance, §7.2) until those agents exist: you supply the proposed
// recommendation and its supporting financial facts directly, in exactly
// the shape the Budget Analysis Agent will eventually hand off, and this
// runs the same deterministic gate (§7.6, PR-01 to PR-09) those agents feed.
export default function EvaluatePolicyModal({ workflow, onClose, onEvaluated }: EvaluatePolicyModalProps) {
  const evaluatePolicy = useEvaluatePolicy();

  const [recommendation, setRecommendation] = useState("DISPOSE");
  const [repairToReplaceRatio, setRepairToReplaceRatio] = useState<number | undefined>();
  const [projectedRepairCost, setProjectedRepairCost] = useState<number | undefined>();
  const [budgetHeadroom, setBudgetHeadroom] = useState<number | undefined>();
  const [confidence, setConfidence] = useState<number | undefined>();

  const handleSubmit = () => {
    if (evaluatePolicy.isPending) return;
    evaluatePolicy.mutate(
      {
        id: workflow.id,
        payload: {
          proposed_recommendation: recommendation,
          financial_assessment: {
            repair_to_replace_ratio: repairToReplaceRatio,
            projected_repair_cost: projectedRepairCost,
            budget_headroom: budgetHeadroom,
            confidence,
          },
        },
      },
      { onSuccess: onEvaluated },
    );
  };

  return (
    <Modal
      open
      modalLabel="Agentic Workflows"
      modalHeading={`Evaluate policy compliance — ${workflow.asset_code}`}
      primaryButtonText={evaluatePolicy.isPending ? "Evaluating…" : "Run evaluation"}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={evaluatePolicy.isPending}
      onRequestClose={onClose}
      onRequestSubmit={handleSubmit}
    >
      <p className="cg-table__muted" style={{ margin: "0 0 1rem", fontSize: "0.8125rem" }}>
        Runs the deterministic rule engine (PR-01 to PR-09) against this asset's compliance state and the
        figures below, then either completes advisory, pauses for approval, or sends the workflow back to
        analysis — exactly what the Policy Compliance Agent's node does once the rest of the graph exists.
      </p>

      {evaluatePolicy.isError && (
        <InlineNotification
          kind="error"
          title="Could not run the evaluation"
          subtitle={getErrorMessage(evaluatePolicy.error, "Something went wrong. Please try again.")}
          hideCloseButton
          lowContrast
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div style={{ display: "grid", gap: "1rem" }}>
        <Select
          id="evaluate-recommendation"
          labelText="Proposed recommendation"
          value={recommendation}
          onChange={(e) => setRecommendation(e.target.value)}
        >
          {RECOMMENDATIONS.map((r) => (
            <SelectItem key={r} value={r} text={r} />
          ))}
        </Select>

        <p className="cg-table__muted" style={{ margin: 0, fontSize: "0.8125rem" }}>
          Financial facts (optional — only REPLACE/REPAIR rules and the confidence check use these)
        </p>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <NumberInput
            id="evaluate-ratio"
            label="Repair-to-replace ratio"
            value={repairToReplaceRatio ?? ""}
            onChange={(_, { value }) => setRepairToReplaceRatio(value === "" ? undefined : Number(value))}
            step={0.01}
            allowEmpty
          />
          <NumberInput
            id="evaluate-confidence"
            label="Confidence (0-1)"
            value={confidence ?? ""}
            onChange={(_, { value }) => setConfidence(value === "" ? undefined : Number(value))}
            step={0.01}
            allowEmpty
          />
          <NumberInput
            id="evaluate-repair-cost"
            label="Projected repair cost (LKR)"
            value={projectedRepairCost ?? ""}
            onChange={(_, { value }) => setProjectedRepairCost(value === "" ? undefined : Number(value))}
            allowEmpty
          />
          <NumberInput
            id="evaluate-budget-headroom"
            label="Budget headroom (LKR)"
            value={budgetHeadroom ?? ""}
            onChange={(_, { value }) => setBudgetHeadroom(value === "" ? undefined : Number(value))}
            allowEmpty
          />
        </div>
      </div>
    </Modal>
  );
}
