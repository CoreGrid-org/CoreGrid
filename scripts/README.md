# Scripts

| Folder | Contents |
|---|---|
| [`thunderid/`](thunderid/) | Idempotent ThunderID configuration (`enable-password-recovery.sh`); see `docs/setup/thunderid.md` |
| [`perf/`](perf/) | Performance and load tests: dataset seed, k6 load test, agent workflow latency, slow-query capture (`make perf`) |
| [`db/`](db/) | Database helpers: per-migration SQL export (`make db-export`) |

Every script is runnable directly and through a `make` target (`make help`).
