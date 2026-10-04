#!/usr/bin/env bash
# Runs the full CoreGrid performance suite and writes a results table.
#
#   CG_TOKEN=<Administrator access token> scripts/perf/run-perf.sh
#
# Steps: health check -> seed dataset -> reset pg_stat_statements -> 50-VU load
# test -> agent workflow latency -> slowest queries -> results/<timestamp>/report.md
#
# Environment (all optional except CG_TOKEN):
#   API_URL     API origin                   default http://localhost:5083
#   PG_URL      PostgreSQL connection URI    default postgresql://coregrid:coregrid@localhost:5433/coregrid
#   VUS         virtual users                default 50
#   DURATION    load-test duration           default 5m
#   RUNS        agent workflow runs          default 10
#   SKIP_SEED=1   do not run seed-perf-data.sql
#   SKIP_AGENT=1  skip the agent latency test
#   K6_IMAGE    Docker image when k6 is not installed   default grafana/k6:latest
set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

API_URL="${API_URL:-http://localhost:5083}"
PG_URL="${PG_URL:-postgresql://coregrid:coregrid@localhost:5433/coregrid}"
VUS="${VUS:-50}"
DURATION="${DURATION:-5m}"
RUNS="${RUNS:-10}"
K6_IMAGE="${K6_IMAGE:-grafana/k6:latest}"
STAMP="$(date +%Y%m%d-%H%M%S)"
OUT="$HERE/results/$STAMP"

die()  { echo "error: $*" >&2; exit 1; }
info() { echo "==> $*"; }
warn() { echo "warning: $*" >&2; }

[[ -n "${CG_TOKEN:-}" ]] || die "CG_TOKEN is not set. See scripts/perf/README.md (Getting a token)."
command -v psql >/dev/null || die "psql is required (PostgreSQL client)."
command -v jq   >/dev/null || die "jq is required."
command -v curl >/dev/null || die "curl is required."
mkdir -p "$OUT"

run_k6() {  # run_k6 <script> <summary-file> [extra -e args...]
  local script="$1" summary="$2"; shift 2
  local envs=(-e "API_URL=$API_URL" -e "CG_TOKEN=$CG_TOKEN" "$@")
  if command -v k6 >/dev/null; then
    k6 run "${envs[@]}" --summary-export "$OUT/$summary" "$HERE/$script"
  else
    command -v docker >/dev/null || die "Install k6 (https://k6.io) or Docker."
    docker run --rm --network host --user "$(id -u):$(id -g)" \
      -v "$HERE:/scripts:ro" -v "$OUT:/out" "$K6_IMAGE" \
      run "${envs[@]}" --summary-export "/out/$summary" "/scripts/$script"
  fi
}

info "Health check: $API_URL/health"
curl -fsS "$API_URL/health" >"$OUT/health.json" || die "API is not healthy at $API_URL."

if [[ "${SKIP_SEED:-0}" != "1" ]]; then
  info "Seeding performance dataset (idempotent)"
  psql "$PG_URL" -q -v ON_ERROR_STOP=1 -f "$HERE/seed-perf-data.sql" || die "Seeding failed."
fi

PGSS=0
if psql "$PG_URL" -qAt -c "CREATE EXTENSION IF NOT EXISTS pg_stat_statements; SELECT pg_stat_statements_reset();" >/dev/null 2>&1; then
  PGSS=1; info "pg_stat_statements reset"
else
  warn "pg_stat_statements is not available (needs shared_preload_libraries); slow-query capture skipped."
fi

info "Load test: $VUS VUs for $DURATION"
run_k6 load-test.js load-summary.json -e "VUS=$VUS" -e "DURATION=$DURATION" | tee "$OUT/load-test.log"
LOAD_EXIT=${PIPESTATUS[0]}

AGENT_EXIT=-1
if [[ "${SKIP_AGENT:-0}" != "1" ]]; then
  info "Agent workflow latency: $RUNS runs"
  run_k6 agent-latency.js agent-summary.json -e "RUNS=$RUNS" | tee "$OUT/agent-latency.log"
  AGENT_EXIT=${PIPESTATUS[0]}
fi

if [[ $PGSS == 1 ]]; then
  psql "$PG_URL" -q -f "$HERE/slow-queries.sql" >"$OUT/slow-queries.txt" 2>&1
fi

# ---------------------------------------------------------------- report ----
ms()   { jq -r --arg m "$1" --arg s "$2" '.metrics[$m][$s] // empty | (. * 1 | round)' "$3" 2>/dev/null; }
rate() { jq -r --arg m "$1" '.metrics[$m].value // empty | (. * 10000 | round / 100)' "$2" 2>/dev/null; }
verdict() { [[ -z "$1" ]] && echo "not measured" && return; awk -v v="$1" -v t="$2" -v op="$3" 'BEGIN{ok=(op=="<"?v<t:v>=t); print (ok?"PASS":"FAIL")}'; }

L="$OUT/load-summary.json"; A="$OUT/agent-summary.json"
SR95=$(ms cg_single_read 'p(95)' "$L"); LR95=$(ms cg_list_read 'p(95)' "$L"); QR95=$(ms cg_qr_lookup 'p(95)' "$L")
RPMAX=$(ms cg_report_generation max "$L"); RPMED=$(ms cg_report_generation med "$L")
OKPCT=$(rate cg_success "$L"); REQS=$(jq -r '.metrics.http_reqs.count // empty' "$L" 2>/dev/null)
RPS=$(jq -r '.metrics.http_reqs.rate // empty | . * 10 | round / 10' "$L" 2>/dev/null)
AGMED=$(ms cg_agent_workflow med "$A"); AGMAX=$(ms cg_agent_workflow max "$A"); AGP95=$(ms cg_agent_workflow 'p(95)' "$A")
DATASET=$(psql "$PG_URL" -qAt -c "SELECT (SELECT count(*) FROM \"Assets\") || ' assets, ' || (SELECT count(*) FROM \"MaintenanceRecords\") || ' maintenance records'" 2>/dev/null)

sec() { [[ -n "$1" ]] && awk -v v="$1" 'BEGIN{printf "%.1f s", v/1000}'; }

{
  echo "# CoreGrid Performance Results — $STAMP"
  echo
  echo "| Item | Value |"
  echo "|---|---|"
  echo "| API | \`$API_URL\` |"
  echo "| Dataset | ${DATASET:-unknown} |"
  echo "| Load profile | $VUS virtual users, $DURATION, 70:30 read/write, report export 4/min |"
  echo "| Requests | ${REQS:-?} total, ${RPS:-?} req/s |"
  echo "| Client host | $(uname -sr), $(nproc 2>/dev/null || echo ?) CPU, $(free -g 2>/dev/null | awk '/Mem:/{print $2" GB RAM"}') |"
  echo
  echo "| Metric | Target | Measured | Result |"
  echo "|---|---|---|---|"
  echo "| Single-resource read p95 | ≤ 500 ms | ${SR95:+$SR95 ms} | $(verdict "$SR95" 500 '<') |"
  echo "| Paginated list p95 | ≤ 800 ms | ${LR95:+$LR95 ms} | $(verdict "$LR95" 800 '<') |"
  echo "| QR lookup p95 | ≤ 1 s | ${QR95:+$QR95 ms} | $(verdict "$QR95" 1000 '<') |"
  echo "| $VUS concurrent users, $DURATION, 70:30 read/write | ≥ 99 % success | ${OKPCT:+$OKPCT %} | $(verdict "$OKPCT" 99 '>=') |"
  echo "| Report generation (audit PDF) | ≤ 5 s | ${RPMAX:+max $(sec "$RPMAX"), median $(sec "$RPMED")} | $(verdict "$RPMAX" 5000 '<') |"
  if [[ -n "$AGMED" ]]; then
    AGRES=$( [[ $(verdict "$AGMED" 60000 '<') == PASS && $(verdict "$AGMAX" 120000 '<') == PASS ]] && echo PASS || echo FAIL )
    echo "| Agent workflow ($RUNS runs) | median ≤ 60 s, max 120 s | median $(sec "$AGMED"), p95 $(sec "$AGP95"), max $(sec "$AGMAX") | $AGRES |"
  else
    echo "| Agent workflow | median ≤ 60 s, max 120 s | | not measured |"
  fi
  echo "| Slowest 5 DB queries | — | see below | — |"
  echo
  echo "## Slowest 5 database queries (pg_stat_statements)"
  echo
  echo '```'
  if [[ -s "$OUT/slow-queries.txt" ]]; then cat "$OUT/slow-queries.txt"; else echo "not captured (pg_stat_statements unavailable)"; fi
  echo '```'
  echo
  echo "Raw k6 summaries: \`load-summary.json\`, \`agent-summary.json\`; logs: \`load-test.log\`, \`agent-latency.log\`."
} >"$OUT/report.md"

info "Report: $OUT/report.md"
cat "$OUT/report.md"

# k6 exits 99 when a threshold fails; surface that as the script's status.
[[ $LOAD_EXIT == 0 && ( $AGENT_EXIT == 0 || $AGENT_EXIT == -1 ) ]]
