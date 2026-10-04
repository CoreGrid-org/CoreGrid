-- Five slowest statements (by mean execution time) recorded by pg_stat_statements
-- since the last reset, for the current database. Requires the extension to be
-- preloaded (docker-compose.yml sets shared_preload_libraries=pg_stat_statements;
-- on a managed server enable it in the server parameters).
--
--   psql "$PG_URL" -c "CREATE EXTENSION IF NOT EXISTS pg_stat_statements; SELECT pg_stat_statements_reset();"
--   ... run the load test ...
--   psql "$PG_URL" -f scripts/perf/slow-queries.sql

\pset format aligned
\pset border 2

SELECT round(mean_exec_time::numeric, 2)                         AS mean_ms,
       round(max_exec_time::numeric, 2)                          AS max_ms,
       calls,
       round(total_exec_time::numeric, 0)                        AS total_ms,
       left(regexp_replace(query, '\s+', ' ', 'g'), 140)         AS query
  FROM pg_stat_statements
 WHERE dbid = (SELECT oid FROM pg_database WHERE datname = current_database())
   AND query NOT ILIKE '%pg_stat_statements%'
   AND query NOT ILIKE 'BEGIN%' AND query NOT ILIKE 'COMMIT%'
 ORDER BY mean_exec_time DESC
 LIMIT 5;
