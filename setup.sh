#!/usr/bin/env bash
# CoreGrid — first-time local development setup. Safe to re-run.
#
#   ./setup.sh               full setup, prompts where input is needed
#   ./setup.sh --yes         non-interactive: never prompt, skip steps that need input
#   ./setup.sh --skip-users  do not create the test accounts
#
# What it does:
#   1. Checks prerequisites (.NET 10 SDK, Node 20+, Docker with Compose v2, curl, jq)
#   2. Installs dotnet-ef and restores backend and frontend dependencies
#   3. Creates backend/.env and frontend/.env from their examples (never overwrites)
#   4. Starts PostgreSQL and ThunderID in Docker (first run bootstraps ThunderID)
#   5. Applies the EF Core migrations
#   6. Checks the ThunderID configuration (docs/setup/thunderid.md) and asks for the
#      client credentials it cannot discover itself
#   7. Creates one test account per role (password Login@123456) — local use only
#
# The ThunderID console steps themselves are manual; see docs/setup/thunderid.md.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

ASSUME_YES=0; SKIP_USERS=0
for arg in "$@"; do
  case "$arg" in
    --yes|-y) ASSUME_YES=1 ;;
    --skip-users) SKIP_USERS=1 ;;
    -h|--help) sed -n '2,20p' "$0"; exit 0 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done
[[ -t 0 ]] || ASSUME_YES=1

THUNDERID_URL="https://localhost:8090"
API_URL="http://localhost:5083"
TEST_PASSWORD="Login@123456"
TEST_ORG_NAME="CoreGrid Demo Organisation"
# email | given name | family name | CoreGrid role
TEST_USERS=(
  "admin@coregrid.test|Ada|Administrator|Administrator"
  "officer@coregrid.test|Oscar|Officer|InventoryOfficer"
  "auditor@coregrid.test|Audrey|Auditor|Auditor"
  "staff@coregrid.test|Sam|Staff|Staff"
)

c_blue=$'\033[0;34m'; c_green=$'\033[0;32m'; c_yellow=$'\033[1;33m'; c_red=$'\033[0;31m'; c_off=$'\033[0m'
step() { echo; echo "${c_blue}==>${c_off} $*"; }
ok()   { echo "  ${c_green}✔${c_off} $*"; }
warn() { echo "  ${c_yellow}!${c_off} $*"; }
die()  { echo "${c_red}error:${c_off} $*" >&2; exit 1; }
ask()  { # ask "Prompt" default -> echoes answer
  local prompt="$1" def="${2:-}" reply
  if [[ $ASSUME_YES == 1 ]]; then echo "$def"; return; fi
  read -r -p "  $prompt${def:+ [$def]}: " reply
  echo "${reply:-$def}"
}
confirm() { [[ $ASSUME_YES == 1 ]] && return 1; local r; read -r -p "  $1 [y/N] " r; [[ $r =~ ^[Yy] ]]; }

# Reads a KEY=value from an env file (empty if missing).
env_get() { [[ -f "$1" ]] && sed -n "s/^$2=//p" "$1" | tail -1; }
# Sets KEY=value in an env file, replacing an existing line or appending.
env_set() {
  local file="$1" key="$2" value="$3"
  if grep -q "^$key=" "$file" 2>/dev/null; then
    local tmp; tmp="$(mktemp)"
    awk -v k="$key" -v v="$value" 'BEGIN{FS=OFS="="} $1==k{print k"="v; next} {print}' "$file" >"$tmp" && mv "$tmp" "$file"
  else
    echo "$key=$value" >>"$file"
  fi
}
# Backend setting: backend/.env, then dotnet user-secrets.
backend_setting() {  # backend_setting ThunderID:ScimClientId
  local key="$1" envkey="${1//:/__}" v
  v="$(env_get backend/.env "$envkey")"
  [[ -z "$v" ]] && v="$(cd backend && dotnet user-secrets list 2>/dev/null | sed -n "s/^$key = //p")"
  echo "$v"
}

# ── 1. Prerequisites ─────────────────────────────────────────────────────────
step "Checking prerequisites"
for cmd in dotnet node npm docker curl jq; do
  command -v "$cmd" >/dev/null || die "$cmd is required. See CONTRIBUTING.md § Prerequisites."
done
docker compose version >/dev/null 2>&1 || die "Docker Compose v2 ('docker compose') is required."
docker info >/dev/null 2>&1 || die "Docker is not running."
dotnet --list-sdks | grep -q '^10\.' || die ".NET 10 SDK is required (dotnet --list-sdks)."
node_major="$(node -p 'process.versions.node.split(".")[0]')"
(( node_major >= 20 )) || die "Node.js 20 or newer is required (found $(node -v))."
ok "dotnet $(dotnet --version), node $(node -v), $(docker compose version --short | sed 's/^/compose /')"
command -v psql >/dev/null || warn "psql not found — only needed for 'make perf' and 'make db-shell'."

# ── 2. Tools and dependencies ────────────────────────────────────────────────
step "Installing tools and dependencies"
if ! command -v dotnet-ef >/dev/null && ! dotnet ef --version >/dev/null 2>&1; then
  dotnet tool install --global dotnet-ef --version "10.*" >/dev/null || die "Could not install dotnet-ef."
  export PATH="$PATH:$HOME/.dotnet/tools"
fi
ok "dotnet-ef $(dotnet ef --version 2>/dev/null | tail -1)"
dotnet restore backend >/dev/null && dotnet restore backend.Tests >/dev/null || die "dotnet restore failed."
ok "NuGet packages restored"
(cd frontend && npm ci --no-audit --no-fund >/dev/null) || die "npm ci failed in frontend/."
ok "npm packages installed"

# ── 3. Environment files ─────────────────────────────────────────────────────
step "Creating environment files (existing files are kept)"
for pair in "backend/.env.example:backend/.env" "frontend/.env.example:frontend/.env"; do
  src="${pair%%:*}"; dst="${pair##*:}"
  if [[ -f "$dst" ]]; then ok "$dst exists"; else cp "$src" "$dst" && ok "created $dst"; fi
done

# ── 4. Infrastructure ────────────────────────────────────────────────────────
step "Starting PostgreSQL and ThunderID"
TID_CONTAINER="$(docker ps -a --format '{{.Names}}' | grep -xE 'coregrid-thunderid(-1)?' | head -1)"
if [[ -n "$TID_CONTAINER" ]]; then
  # Already initialised: start only the server and the database. `docker compose up`
  # or `start` would also re-run ThunderID's one-shot setup container, which fails on
  # an initialised volume (docs/setup/thunderid.md).
  docker start "$TID_CONTAINER" >/dev/null || die "Could not start $TID_CONTAINER."
  if docker ps -a --format '{{.Names}}' | grep -qx coregrid-postgres; then
    docker start coregrid-postgres >/dev/null || die "Could not start coregrid-postgres."
  else
    docker compose up -d --no-deps coregrid-postgres >/dev/null || die "Could not create coregrid-postgres."
  fi
  ok "started $TID_CONTAINER and coregrid-postgres"
else
  echo "  First run: pulling and initialising ThunderID (several minutes)…"
  docker compose up -d || die "docker compose up failed."
  ok "containers created"
fi

printf "  waiting for PostgreSQL"
for _ in $(seq 1 60); do docker exec coregrid-postgres pg_isready -U coregrid -d coregrid >/dev/null 2>&1 && break; printf "."; sleep 2; done
docker exec coregrid-postgres pg_isready -U coregrid -d coregrid >/dev/null 2>&1 || die "PostgreSQL did not become ready."
echo; ok "PostgreSQL ready on localhost:5433"

printf "  waiting for ThunderID"
for _ in $(seq 1 90); do curl -sk --max-time 2 "$THUNDERID_URL/" >/dev/null 2>&1 && break; printf "."; sleep 2; done
curl -sk --max-time 2 "$THUNDERID_URL/" >/dev/null 2>&1 || die "ThunderID did not become ready (docker logs ${TID_CONTAINER:-coregrid-thunderid-1})."
echo; ok "ThunderID ready on $THUNDERID_URL"

# ── 5. Database schema ───────────────────────────────────────────────────────
step "Applying database migrations"
DB_CONNECTION="${DB_CONNECTION:-Host=localhost;Port=5433;Database=coregrid;Username=coregrid;Password=coregrid}"
(cd backend && dotnet ef database update --connection "$DB_CONNECTION" >/dev/null) || die "Migrations failed."
ok "schema up to date"

# ── 6. ThunderID configuration ───────────────────────────────────────────────
step "Checking ThunderID configuration (docs/setup/thunderid.md)"
admin_pw="$(docker logs coregrid-thunderid-setup-1 2>&1 | sed -n 's/^ *Password: //p' | tail -1)"
[[ -n "$admin_pw" ]] && ok "console: $THUNDERID_URL/console  (user: admin, password: $admin_pw)"

CLIENT_ID="$(backend_setting ThunderID:ScimClientId)"
CLIENT_SECRET="$(backend_setting ThunderID:ScimClientSecret)"
RESOURCE="$(backend_setting ThunderID:Resource)"; RESOURCE="${RESOURCE:-$THUNDERID_URL/mcp}"

mgmt_token() {
  curl -sk --max-time 10 -X POST "$THUNDERID_URL/oauth2/token" \
    -d grant_type=client_credentials -d "client_id=$CLIENT_ID" --data-urlencode "client_secret=$CLIENT_SECRET" \
    -d scope=system --data-urlencode "resource=$RESOURCE" | jq -r '.access_token // empty'
}

TOKEN=""
[[ -n "$CLIENT_ID" && -n "$CLIENT_SECRET" ]] && TOKEN="$(mgmt_token)"
if [[ -z "$TOKEN" ]]; then
  warn "The CoreGrid Backend client credentials are missing or rejected."
  echo "    Complete docs/setup/thunderid.md steps 1–7 in the console, then enter the values here"
  echo "    (stored in git-ignored backend/.env and frontend/.env)."
  if [[ $ASSUME_YES == 0 ]]; then
    CLIENT_ID="$(ask 'CoreGrid Backend Client ID' "$CLIENT_ID")"
    CLIENT_SECRET="$(ask 'CoreGrid Backend Client Secret' '')"
    [[ -n "$CLIENT_ID" ]] && env_set backend/.env ThunderID__ScimClientId "$CLIENT_ID"
    [[ -n "$CLIENT_SECRET" ]] && env_set backend/.env ThunderID__ScimClientSecret "$CLIENT_SECRET"
    [[ -n "$CLIENT_ID" && -n "$CLIENT_SECRET" ]] && TOKEN="$(mgmt_token)"
  fi
fi
if [[ -n "$TOKEN" ]]; then
  ok "CoreGrid Backend can obtain a management token"
else
  warn "Skipping account creation until the backend credentials work. Re-run ./setup.sh afterwards."
  SKIP_USERS=1
fi

if [[ -z "$(env_get frontend/.env VITE_THUNDERID_CLIENT_ID)" ]]; then
  fe_id="$(ask 'CoreGrid Frontend Client ID (docs step 4; blank to skip)' '')"
  [[ -n "$fe_id" ]] && env_set frontend/.env VITE_THUNDERID_CLIENT_ID "$fe_id" && ok "frontend/.env updated"
fi
[[ -n "$(env_get frontend/.env VITE_THUNDERID_BASE_URL)" ]] || env_set frontend/.env VITE_THUNDERID_BASE_URL "$THUNDERID_URL"

if [[ -n "$TOKEN" && -z "$(env_get frontend/.env VITE_THUNDERID_APPLICATION_ID)" ]] && confirm "Enable 'Forgot password?' on the web sign-in page now (docs step 8)?"; then
  THUNDERID_CLIENT_ID="$CLIENT_ID" THUNDERID_CLIENT_SECRET="$CLIENT_SECRET" scripts/thunderid/enable-password-recovery.sh \
    || warn "Password recovery setup failed; run 'make thunderid-recovery' later."
fi

# ── 7. Test accounts ─────────────────────────────────────────────────────────
if [[ $SKIP_USERS == 0 ]]; then
  step "Creating test accounts (local development only)"
  OU_ID="$(backend_setting ThunderID:OuId)"; USER_TYPE="$(backend_setting ThunderID:UserType)"
  USER_TYPE="${USER_TYPE:-CoreGridUser}"

  STARTED_API=0
  if ! curl -fs --max-time 2 "$API_URL/health" >/dev/null 2>&1; then
    mkdir -p .setup
    # Own process group, so the whole `dotnet run` tree can be stopped afterwards.
    run_api="cd backend && ASPNETCORE_ENVIRONMENT=Development exec dotnet run --no-launch-profile -- --urls '$API_URL'"
    if command -v setsid >/dev/null; then setsid bash -c "$run_api" >"$ROOT/.setup/api.log" 2>&1 &
    else bash -c "$run_api" >"$ROOT/.setup/api.log" 2>&1 &   # macOS: no setsid
    fi
    API_PID=$!; STARTED_API=1
    printf "  starting the API"
    for _ in $(seq 1 90); do curl -fs --max-time 2 "$API_URL/health" >/dev/null 2>&1 && break; printf "."; sleep 2; done
    echo
  fi
  stop_api() {
    [[ $STARTED_API == 1 ]] || return 0
    kill -- "-$API_PID" 2>/dev/null || { pkill -P "$API_PID" 2>/dev/null; kill "$API_PID" 2>/dev/null; }
    wait "$API_PID" 2>/dev/null; STARTED_API=0
  }
  trap stop_api EXIT

  if ! curl -fs --max-time 2 "$API_URL/health" >/dev/null 2>&1; then
    warn "The API did not start (see .setup/api.log); skipping account creation."
  else
    # First-run Setup creates the organisation and the Administrator (in ThunderID and CoreGrid).
    IFS='|' read -r a_email a_given a_family _ <<<"${TEST_USERS[0]}"
    if [[ "$(curl -fs "$API_URL/api/setup/status" | jq -r '.needs_setup')" == "true" ]]; then
      body="$(jq -n --arg e "$a_email" --arg g "$a_given" --arg f "$a_family" --arg p "$TEST_PASSWORD" --arg o "$TEST_ORG_NAME" \
        '{admin:{email:$e,given_name:$g,family_name:$f,password:$p},organisation:{name:$o}}')"
      if curl -fs -X POST "$API_URL/api/setup/complete" -H 'Content-Type: application/json' -d "$body" >/dev/null; then
        ok "organisation '$TEST_ORG_NAME' and $a_email created"
      else
        warn "First-run Setup failed (see .setup/api.log)."
      fi
    else
      ok "organisation already set up — Administrator account not changed"
    fi

    # The other roles are created in ThunderID exactly as ThunderIdIdentityDirectory does;
    # CoreGrid mirrors each user automatically on their first sign-in.
    for entry in "${TEST_USERS[@]:1}"; do
      IFS='|' read -r email given family role <<<"$entry"
      role_id="$(backend_setting "ThunderID:RoleIds:$role")"
      [[ -n "$role_id" && -n "$OU_ID" ]] || { warn "ThunderID:OuId or RoleIds:$role not configured — skipped $email"; continue; }
      payload="$(jq -n --arg ou "$OU_ID" --arg t "$USER_TYPE" --arg e "$email" --arg g "$given" --arg f "$family" --arg p "$TEST_PASSWORD" \
        '{ouId:$ou,type:$t,attributes:{email:$e,username:$e,given_name:$g,family_name:$f,password:$p}}')"
      resp="$(curl -sk -w '\n%{http_code}' -X POST "$THUNDERID_URL/users" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "$payload")"
      code="${resp##*$'\n'}"
      case "$code" in
        201)
          uid="$(sed '$d' <<<"$resp" | jq -r '.id')"
          if curl -sk -o /dev/null -w '%{http_code}' -X POST "$THUNDERID_URL/roles/$role_id/assignments/add" \
               -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
               -d "$(jq -n --arg id "$uid" '{assignments:[{type:"user",id:$id}]}')" | grep -q '^204$'; then
            ok "$email ($role)"
          else
            warn "$email created but the $role role assignment failed — assign it in the console"
          fi ;;
        409) ok "$email already exists" ;;
        *)   warn "$email not created (HTTP $code)" ;;
      esac
    done
  fi
  stop_api; trap - EXIT
fi

# ── Done ─────────────────────────────────────────────────────────────────────
echo
echo "${c_green}Setup complete.${c_off}"
cat <<EOF

  Start everything:   make dev
  Web app:            http://localhost:5173
  API / Swagger:      $API_URL/swagger   (health: $API_URL/health)
  ThunderID console:  $THUNDERID_URL/console

  Test accounts (local only, password: $TEST_PASSWORD)
    Administrator       admin@coregrid.test      web
    Inventory Officer   officer@coregrid.test    web + mobile
    Auditor             auditor@coregrid.test    web
    Department Staff    staff@coregrid.test      mobile

  Later starts: 'make infra-up' then 'make dev'. Full guide: CONTRIBUTING.md.
EOF
