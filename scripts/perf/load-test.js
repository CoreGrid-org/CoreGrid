// CoreGrid API load test (SRS §13.5, NFR-01 – NFR-06).
//
// Profile: 50 virtual users for 5 minutes, 70:30 read/write mix, plus a slow
// background stream of report exports. Thresholds encode the acceptance targets,
// so k6 exits non-zero when a target is missed.
//
//   k6 run -e API_URL=http://localhost:5083 -e CG_TOKEN=<Administrator bearer token> \
//          --summary-export results/load-summary.json scripts/perf/load-test.js
//
// Normally started through scripts/perf/run-perf.sh, which also seeds data,
// collects slow queries and writes the results table.
//
// Environment:
//   API_URL    API origin, no trailing slash            (default http://localhost:5083)
//   CG_TOKEN   Access token of an Administrator account (required)
//   VUS        Concurrent virtual users                 (default 50)
//   DURATION   Test duration                            (default 5m)
//   ASSET_SEARCH  Search term that selects seeded assets (default PERF-)

import http from "k6/http";
import { check, fail, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

const API = (__ENV.API_URL || "http://localhost:5083").replace(/\/$/, "");
const TOKEN = __ENV.CG_TOKEN;
const VUS = Number(__ENV.VUS || 50);
const DURATION = __ENV.DURATION || "5m";
const SEARCH = __ENV.ASSET_SEARCH || "PERF-";

const singleRead = new Trend("cg_single_read", true);
const listRead = new Trend("cg_list_read", true);
const qrLookup = new Trend("cg_qr_lookup", true);
const writeOp = new Trend("cg_write", true);
const reportGen = new Trend("cg_report_generation", true);
const success = new Rate("cg_success");

export const options = {
  summaryTrendStats: ["avg", "min", "med", "max", "p(95)", "p(99)"],
  scenarios: {
    mixed: {
      executor: "constant-vus",
      exec: "mixed",
      vus: VUS,
      duration: DURATION,
    },
    reports: {
      // Report export is rate-limited to 20/min per user; stay well below it.
      executor: "constant-arrival-rate",
      exec: "reports",
      rate: 4,
      timeUnit: "1m",
      duration: DURATION,
      preAllocatedVUs: 2,
    },
  },
  thresholds: {
    cg_single_read: ["p(95)<500"],
    cg_list_read: ["p(95)<800"],
    cg_qr_lookup: ["p(95)<1000"],
    cg_report_generation: ["max<5000"],
    cg_success: ["rate>=0.99"],
  },
};

const headers = () => ({
  Authorization: `Bearer ${TOKEN}`,
  "Content-Type": "application/json",
  Accept: "application/json",
});

export function setup() {
  if (!TOKEN) fail("CG_TOKEN is not set — see scripts/perf/README.md for how to obtain an Administrator token.");

  const health = http.get(`${API}/health`);
  if (health.status !== 200) fail(`API health check failed: ${health.status} ${health.body}`);

  const assets = [];
  for (let page = 1; page <= 20 && assets.length < 1000; page++) {
    const res = http.get(`${API}/api/assets?search=${encodeURIComponent(SEARCH)}&page=${page}&pageSize=100`, { headers: headers() });
    if (res.status === 401 || res.status === 403) fail(`Token rejected (${res.status}). Use a fresh Administrator token.`);
    if (res.status !== 200) fail(`Asset list failed: ${res.status} ${res.body}`);
    const body = res.json();
    const items = body.items || [];
    items.forEach((a) => assets.push({ id: a.id, code: a.asset_code }));
    if (items.length < 100) break;
  }
  if (assets.length < 50) fail(`Only ${assets.length} seeded assets found for search "${SEARCH}". Run seed-perf-data.sql first.`);
  return { assets };
}

const pick = (arr) => arr[Math.floor(Math.random() * arr.length)];
const ok = (res, expected) => {
  const passed = expected.includes(res.status);
  success.add(passed);
  check(res, { [`status ${expected.join("/")}`]: () => passed });
  return passed;
};

export function mixed(data) {
  const asset = pick(data.assets);
  const r = Math.random();

  if (r < 0.30) {
    const res = http.get(`${API}/api/assets/${asset.id}`, { headers: headers(), tags: { op: "single_read" } });
    singleRead.add(res.timings.duration);
    ok(res, [200]);
  } else if (r < 0.55) {
    const page = 1 + Math.floor(Math.random() * 5);
    const res = http.get(`${API}/api/assets?page=${page}&pageSize=25&sortBy=name`, { headers: headers(), tags: { op: "list_read" } });
    listRead.add(res.timings.duration);
    ok(res, [200]);
  } else if (r < 0.70) {
    const res = http.get(`${API}/api/assets/qr/${encodeURIComponent(asset.code)}`, { headers: headers(), tags: { op: "qr_lookup" } });
    qrLookup.add(res.timings.duration);
    ok(res, [200]);
  } else if (r < 0.90) {
    const condition = pick(["GOOD", "FAIR", "POOR"]);
    const res = http.patch(`${API}/api/assets/${asset.id}/condition`, JSON.stringify({ condition }), { headers: headers(), tags: { op: "write_condition" } });
    writeOp.add(res.timings.duration);
    ok(res, [200, 204]);
  } else {
    const body = JSON.stringify({ asset_id: asset.id, description: "PERF load-test fault report", observed_condition: "FAIR" });
    const res = http.post(`${API}/api/maintenance/faults`, body, { headers: headers(), tags: { op: "write_fault" } });
    writeOp.add(res.timings.duration);
    ok(res, [200, 201]);
  }

  sleep(0.5 + Math.random());
}

export function reports() {
  const res = http.get(`${API}/api/reports/audit/export?format=pdf`, { headers: headers(), tags: { op: "report" }, timeout: "30s" });
  reportGen.add(res.timings.duration);
  ok(res, [200]);
}
