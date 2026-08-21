const API_URL = import.meta.env.VITE_API_URL;

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

async function handle<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(detail || fallback);
  }
  return response.json();
}

export type WorkflowStatus =
  | "PLANNING"
  | "ANALYZING"
  | "VALIDATING"
  | "AWAITING_APPROVAL"
  | "APPROVED"
  | "REJECTED"
  | "COMPLETED_ADVISORY"
  | "REVISION_REQUESTED"
  | "FAILED_SAFE";

export interface PolicyRuleResult {
  rule_id: string;
  expected: string;
  actual: string;
  outcome: "PASS" | "FAIL" | "NEEDS_REVISION" | "N/A";
}

export interface PolicyValidation {
  verdict: "PASS" | "FAIL" | "NEEDS_REVISION";
  rule_results: PolicyRuleResult[];
  blocking_reasons: string[];
  is_high_impact: boolean;
}

export interface AgentWorkflow {
  id: string;
  asset_id: string;
  asset_code: string;
  objective: string;
  status: WorkflowStatus;
  recommendation: string | null;
  is_high_impact: boolean;
  approval_status: "NOT_REQUIRED" | "PENDING" | "APPROVED" | "REJECTED";
  revision_count: number;
  failure_reason: string | null;
  validation_result: PolicyValidation | null;
  correlation_id: string;
  initiated_by_user_id: string;
  initiated_by_email: string | null;
  started_at: string | null;
  completed_at: string | null;
  created_at: string;
}

export interface CreateWorkflowRequest {
  asset_id: string;
  objective: string;
}

export interface FinancialAssessmentFacts {
  repair_to_replace_ratio?: number;
  projected_repair_cost?: number;
  budget_headroom?: number;
  confidence?: number;
}

export interface EvaluatePolicyRequest {
  proposed_recommendation: string;
  financial_assessment?: FinancialAssessmentFacts;
}

export interface DecideWorkflowRequest {
  decision: "APPROVE" | "REJECT" | "REVISE";
  reason: string;
}

// backend/Features/Agents/Controllers/AgentWorkflowsController.cs
export async function listWorkflows(status: string | undefined, accessToken: string): Promise<AgentWorkflow[]> {
  const qs = status ? `?status=${encodeURIComponent(status)}` : "";
  const response = await fetch(`${API_URL}/agent-workflows${qs}`, { headers: authHeaders(accessToken) });
  return handle(response, "Could not load workflows.");
}

export async function createWorkflow(payload: CreateWorkflowRequest, accessToken: string): Promise<AgentWorkflow> {
  const response = await fetch(`${API_URL}/agent-workflows`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not initiate the evaluation.");
}

export async function evaluatePolicy(
  id: string,
  payload: EvaluatePolicyRequest,
  accessToken: string,
): Promise<AgentWorkflow> {
  const response = await fetch(`${API_URL}/agent-workflows/${id}/evaluate`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not run the policy evaluation.");
}

export async function decideWorkflow(
  id: string,
  payload: DecideWorkflowRequest,
  accessToken: string,
): Promise<AgentWorkflow> {
  const response = await fetch(`${API_URL}/agent-workflows/${id}/decide`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", ...authHeaders(accessToken) },
    body: JSON.stringify(payload),
  });
  return handle(response, "Could not record this decision.");
}
