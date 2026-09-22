import { fetchAllPages } from "@/shared/lib/apiClient";

const API_URL = import.meta.env.VITE_API_URL;

// Represents a paginated API response.
export interface PagedResult<T> {
  items: T[];
  total_count: number;
  page: number;
  page_size: number;
  total_pages: number;
}

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

export interface PlannerPlanStep {
  seq: number;
  agent: string;
  purpose: string;
  expected_output: string;
}

export interface PlannerExecutionPlan {
  inScope: boolean;
  rejectionReason: string | null;
  steps: PlannerPlanStep[];
}

// SRS §7.3 node 2 (Maintenance Analysis Agent) output — repair count, MTBF,
// cost trend, 12-month projection. Facts only, never a recommendation.
export interface FailureStatistics {
  asset_id: string;
  asset_code: string;
  repair_count: number;
  mean_time_between_failures_days: number | null;
  cost_trend: "INCREASING" | "DECREASING" | "STABLE" | "INSUFFICIENT_DATA";
  projected_next_twelve_months_cost: number;
  evaluated_as_of: string;
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
  plan: PlannerExecutionPlan | null;
  validation_result: PolicyValidation | null;
  maintenance_analysis: FailureStatistics | null;
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

// backend/Features/Agents/Controllers/AgentWorkflowsController.cs — GetWorkflows
// is paginated (§7); WorkflowsPage itself has no page-navigation UI (it
// splits the full list into Active/Awaiting Approval/Completed tabs
// client-side), so this walks every page and returns the flattened list
// rather than silently truncating to the first page.
export async function listWorkflows(status: string | undefined, accessToken: string): Promise<AgentWorkflow[]> {
  const statusQuery = status ? `&status=${encodeURIComponent(status)}` : "";
  return fetchAllPages((page) =>
    fetch(`${API_URL}/agent-workflows?page=${page}&pageSize=100${statusQuery}`, {
      headers: authHeaders(accessToken),
    }).then((response) => handle<PagedResult<AgentWorkflow>>(response, "Could not load workflows.")),
  );
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

export async function runPolicyAgent(id: string, accessToken: string): Promise<AgentWorkflow> {
  const response = await fetch(`${API_URL}/agent-workflows/${id}/run-policy-agent`, {
    method: "POST",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not run the Policy Compliance Agent.");
}

// Node 2 (Maintenance Analysis Agent) now also runs automatically right
// after Planner accepts a new workflow's objective — this manual endpoint
// stays available to re-run it (e.g. after a revision cycle sends the
// workflow back to ANALYZING), same as /run-policy-agent for node 4.
export async function runMaintenanceAgent(id: string, accessToken: string): Promise<AgentWorkflow> {
  const response = await fetch(`${API_URL}/agent-workflows/${id}/run-maintenance-agent`, {
    method: "POST",
    headers: authHeaders(accessToken),
  });
  return handle(response, "Could not run the Maintenance Analysis Agent.");
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

// SRS §9.6 — one row per node execution (which agent, what it produced, how
// long it took, whether it succeeded) plus the approval decision, if any.
export interface AgentExecutionStep {
  id: string;
  agent: string;
  sequence: number;
  input_hash: string | null;
  output_summary: string | null;
  duration_ms: number | null;
  status: "SUCCESS" | "FAILED";
  error: string | null;
  created_at: string;
}

export interface AgentApproval {
  id: string;
  decision: "APPROVE" | "REJECT" | "REVISE";
  decided_by_user_id: string;
  decided_by_email: string | null;
  reason: string;
  decided_at: string;
}

export interface WorkflowExecutionSummary {
  workflow: AgentWorkflow;
  steps: AgentExecutionStep[];
  approvals: AgentApproval[];
}

export async function getExecutionSummary(id: string, accessToken: string): Promise<WorkflowExecutionSummary> {
  const response = await fetch(`${API_URL}/agent-workflows/${id}/execution-summary`, { headers: authHeaders(accessToken) });
  return handle(response, "Could not load the execution trace.");
}
