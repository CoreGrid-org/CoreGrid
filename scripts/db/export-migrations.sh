#!/usr/bin/env bash
# Generates the numbered, idempotent SQL export (backend/db/migrations/NNNN_name.sql)
# for every EF Core migration that does not have one yet, then regenerates
# backend/db/schema.sql. See backend/db/README.md — these files are generated,
# never hand-edited.
#
#   scripts/db/export-migrations.sh          (or: make db-export)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT/backend"
OUT_DIR="db/migrations"

snake() { sed -E 's/([a-z0-9])([A-Z])/\1_\2/g; s/([A-Z]+)([A-Z][a-z])/\1_\2/g' <<<"$1" | tr '[:upper:]' '[:lower:]'; }

echo "==> Listing migrations"
mapfile -t MIGRATIONS < <(dotnet ef migrations list --no-color 2>/dev/null | grep -E '^[0-9]{14}_' | awk '{print $1}')
[[ ${#MIGRATIONS[@]} -gt 0 ]] || { echo "error: no migrations found (does the project build?)" >&2; exit 1; }

last_num=$(ls "$OUT_DIR" | grep -Eo '^[0-9]{4}' | sort -n | tail -1)
next=$((10#${last_num:-0} + 1))
prev="0"
created=0

for id in "${MIGRATIONS[@]}"; do
  name="${id#*_}"
  file_suffix="$(snake "$name").sql"
  if ! ls "$OUT_DIR"/[0-9][0-9][0-9][0-9]_"$file_suffix" >/dev/null 2>&1; then
    target="$OUT_DIR/$(printf '%04d' "$next")_$file_suffix"
    echo "==> $id -> $target"
    dotnet ef migrations script "$prev" "$id" --idempotent --no-build -o "$target"
    next=$((next + 1)); created=$((created + 1))
  fi
  prev="$id"
done

echo "==> Regenerating db/schema.sql"
dotnet ef migrations script --no-build -o db/schema.sql
echo "Done: $created new export(s)."
