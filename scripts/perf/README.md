# Performance Tests

Load and latency tests for the CoreGrid API, measuring the targets in SRS §10 (NFR-01 – NFR-06) and §13.5.

| File | Purpose |
|---|---|
| `run-perf.sh` | Runs everything below and writes `results/<timestamp>/report.md` |
| `seed-perf-data.sql` | Idempotent dataset: 600 assets, 1,800 maintenance records, default policy (`PERF-` prefix) |
| `load-test.js` | k6: 50 VUs × 5 min, 70:30 read/write, plus report exports |
| `agent-latency.js` | k6: N sequential end-to-end agent workflows |
| `slow-queries.sql` | Five slowest statements from `pg_stat_statements` |

## What is measured

| Metric | Target | How |
|---|---|---|
| Single-resource read p95 | ≤ 500 ms | `GET /api/assets/{id}`, 30 % of the mix |
| Paginated list p95 | ≤ 800 ms | `GET /api/assets?page=…&pageSize=25`, 25 % |
| QR lookup p95 | ≤ 1 s | `GET /api/assets/qr/{code}`, 15 % |
| Success rate, 50 concurrent users, 5 min, 70:30 read/write | ≥ 99 % | Writes: `PATCH /api/assets/{id}/condition` 20 %, `POST /api/maintenance/faults` 10 % |
| Report generation | ≤ 5 s | `GET /api/reports/audit/export?format=pdf`, 4 per minute alongside the load |
| Agent workflow | median ≤ 60 s, max 120 s | `POST /api/agent-workflows` + `POST …/run-policy-agent`, one every 7 s (initiation is rate-limited to 10/min) |
| Slowest 5 DB queries | — | `pg_stat_statements`, reset at the start of the run |

k6 thresholds encode these targets, so a missed target makes the run exit non-zero.

## Prerequisites

- The API running and healthy, with first-run Setup completed (`make dev`, or a deployed instance).
- `psql`, `jq`, `curl`; and [k6](https://grafana.com/docs/k6/latest/set-up/install-k6/), or Docker, in which case `grafana/k6` is used automatically.
- For slow-query capture: `pg_stat_statements` preloaded. The local `docker-compose.yml` already does this. On a managed PostgreSQL server, add it to `shared_preload_libraries` in the server parameters.
- **A staging or local database.** The seed and the write traffic add real rows. Never point this at production.

## Getting a token

The tests act as an **Administrator**:
1. Sign in to the web app as an Administrator.
2. Open DevTools → **Network**, and select any request to `/api/…`.
3. Copy the value after `Bearer ` in the `Authorization` request header.

Tokens are short-lived, so take a fresh one just before starting.

```bash
export CG_TOKEN='<paste token>'
```

For a Supabase database, pass the session-pooler URI as `PG_URL`: `postgresql://postgres.<ref>:<password>@aws-0-<region>.pooler.supabase.com:5432/postgres?sslmode=require`.

## Run

```bash
make perf                                   # local API + local Postgres
make perf API_URL=https://<api-host> PG_URL='postgresql://user:pass@host:5432/coregrid?sslmode=require'
VUS=20 DURATION=1m RUNS=3 make perf         # quick smoke run
SKIP_SEED=1 SKIP_AGENT=1 make perf          # load test only
```

Or step by step:

```bash
make perf-seed                              # dataset only
k6 run -e API_URL=… -e CG_TOKEN=… scripts/perf/load-test.js
k6 run -e API_URL=… -e CG_TOKEN=… -e RUNS=10 scripts/perf/agent-latency.js
make perf-slow-queries
```

## Output

`scripts/perf/results/<timestamp>/` (git-ignored) contains:
- `report.md`: the results table (target, measured, PASS/FAIL), the dataset size, the client host, and the slowest queries;
- the raw k6 summaries (`load-summary.json`, `agent-summary.json`), logs, and `slow-queries.txt`.

Latencies are measured by the client, so they include network time. Run the client close to the API (same host or region) when comparing against the server-side targets, and record where it ran.
