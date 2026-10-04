// CoreGrid agentic-workflow latency test (NFR-05: median <= 60 s, hard maximum 120 s).
//
// Runs N complete evaluations one after another, each on a different seeded asset:
//   POST /api/agent-workflows                       Planner -> Maintenance Analysis -> Budget Analysis
//   POST /api/agent-workflows/{id}/run-policy-agent Policy Compliance + deterministic gate
// and records the end-to-end time of the pair. Workflow initiation is rate-limited
// to 10 per minute per user, so iterations are paced at one every 7 seconds.
//
//   k6 run -e API_URL=http://localhost:5083 -e CG_TOKEN=<token> -e RUNS=10 \
//          --summary-export results/agent-summary.json scripts/perf/agent-latency.js
//
// Each run leaves a real workflow behind (usually AWAITING_APPROVAL), so use a
// staging or local database seeded with seed-perf-data.sql.

import http from "k6/http";
import { check, fail, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

const API = (__ENV.API_URL || "http://localhost:5083").replace(/\/$/, "");
const TOKEN = __ENV.CG_TOKEN;
const RUNS = Number(__ENV.RUNS || 10);
const SEARCH = __ENV.ASSET_SEARCH || "PERF-";

const workflowTotal = new Trend("cg_agent_workflow", true);
const workflowCreate = new Trend("cg_agent_create", true);
const workflowPolicy = new Trend("cg_agent_policy", true);
const workflowOk = new Rate("cg_agent_success");

export const options = {
  summaryTrendStats: ["avg", "min", "med", "max", "p(95)"],
  scenarios: {
    agent: { executor: "per-vu-iterations", vus: 1, iterations: RUNS, maxDuration: "30m" },
  },
  thresholds: {
    cg_agent_workflow: ["med<60000", "max<120000"],
    cg_agent_success: ["rate>=0.9"],
  },
};

const headers = () => ({ Authorization: `Bearer ${TOKEN}`, "Content-Type": "application/json" });

export function setup() {
  if (!TOKEN) fail("CG_TOKEN is not set.");
  const res = http.get(`${API}/api/assets?search=${encodeURIComponent(SEARCH)}&status=ACTIVE&page=1&pageSize=100&sortBy=assetcode`, { headers: headers() });
  if (res.status !== 200) fail(`Asset list failed: ${res.status} ${res.body}`);
  const ids = (res.json().items || []).map((a) => a.id);
  if (ids.length < RUNS) fail(`Need ${RUNS} seeded ACTIVE assets, found ${ids.length}.`);
  // Start from a random offset so repeated runs use assets without an in-flight workflow.
  const offset = Math.floor(Math.random() * ids.length);
  return { ids: ids.slice(offset).concat(ids.slice(0, offset)) };
}

export default function (data) {
  const assetId = data.ids[__ITER % data.ids.length];
  const objective = "Evaluate whether this asset should be repaired, retained or disposed of, given its maintenance history and the department budget.";

  const created = http.post(`${API}/api/agent-workflows`, JSON.stringify({ asset_id: assetId, objective }), {
    headers: headers(), timeout: "130s", tags: { op: "agent_create" },
  });
  workflowCreate.add(created.timings.duration);

  let total = created.timings.duration;
  let passed = created.status === 201;

  if (passed) {
    const wf = created.json();
    if (wf.status === "ANALYZING" || wf.status === "PLANNING") {
      const policy = http.post(`${API}/api/agent-workflows/${wf.id}/run-policy-agent`, null, {
        headers: headers(), timeout: "130s", tags: { op: "agent_policy" },
      });
      workflowPolicy.add(policy.timings.duration);
      total += policy.timings.duration;
      passed = policy.status === 200;
    }
  }

  workflowTotal.add(total);
  workflowOk.add(passed);
  check(created, { "workflow created": (r) => r.status === 201 });
  if (!passed) console.warn(`run ${__ITER}: status ${created.status} ${created.body && created.body.slice(0, 200)}`);

  sleep(7);
}
