#!/usr/bin/env bash
# Turns on ThunderID's self-service password recovery ("Forgot password?")
# for the CoreGrid Frontend application. Idempotent — safe to re-run.
#
# What it does, via ThunderID's management API (as the CoreGrid Backend
# client_credentials app — the same identity ThunderIdIdentityDirectory uses):
#   1. Creates a "CoreGrid Recovery Flow" (a copy of ThunderID's default
#      RECOVERY flow whose "Back to sign in" link returns to CoreGrid's own
#      sign-in flow instead of ThunderID's generic one).
#   2. Adds a "Forgot password?" link to the CoreGrid Frontend sign-in flow
#      that calls the recovery flow.
#   3. Sets the application's recoveryFlowId and isRecoveryFlowEnabled=true.
#
# Every CoreGridUser (Administrator, Inventory Officer, Auditor, Staff)
# recovers the same way: enter their email, receive a link, set a new
# password. The emails are only delivered once ThunderID's SMTP is configured
# (deferred to deployment — docs/setup/thunderid.md step 8).
#
# Usage:
#   THUNDERID_CLIENT_SECRET=<backend client secret> scripts/thunderid/enable-password-recovery.sh
# Optional env: THUNDERID_URL (https://localhost:8090), THUNDERID_CLIENT_ID,
#   THUNDERID_RESOURCE, FRONTEND_APP_NAME ("CoreGrid Frontend").
# Without them it reads ThunderID__ScimClientId / ThunderID__ScimClientSecret from
# backend/.env, falling back to the backend's dotnet user-secrets.

set -euo pipefail

command -v jq >/dev/null || { echo "jq is required." >&2; exit 1; }

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
THUNDERID_URL="${THUNDERID_URL:-https://localhost:8090}"
THUNDERID_RESOURCE="${THUNDERID_RESOURCE:-$THUNDERID_URL/mcp}"
FRONTEND_APP_NAME="${FRONTEND_APP_NAME:-CoreGrid Frontend}"
RECOVERY_FLOW_HANDLE="coregrid-recovery-flow"
RECOVERY_FLOW_NAME="CoreGrid Recovery Flow"

backend_value() {  # backend_value ThunderID:ScimClientId
  local v=""
  [[ -f "$REPO_ROOT/backend/.env" ]] && v="$(sed -n "s/^${1//:/__}=//p" "$REPO_ROOT/backend/.env" | tail -1)"
  [[ -z "$v" ]] && v="$(cd "$REPO_ROOT/backend" && dotnet user-secrets list 2>/dev/null | sed -n "s/^$1 = //p")"
  echo "$v"
}
THUNDERID_CLIENT_SECRET="${THUNDERID_CLIENT_SECRET:-$(backend_value ThunderID:ScimClientSecret)}"
THUNDERID_CLIENT_ID="${THUNDERID_CLIENT_ID:-$(backend_value ThunderID:ScimClientId)}"
[[ -n "$THUNDERID_CLIENT_ID" && -n "$THUNDERID_CLIENT_SECRET" ]] || {
  echo "Set THUNDERID_CLIENT_ID and THUNDERID_CLIENT_SECRET (the CoreGrid Backend app's credentials)." >&2
  exit 1
}

# -k: local ThunderID uses a self-signed certificate.
CURL=(curl -sSk --fail-with-body)

TOKEN="$("${CURL[@]}" -X POST "$THUNDERID_URL/oauth2/token" \
  -d grant_type=client_credentials \
  -d "client_id=$THUNDERID_CLIENT_ID" \
  --data-urlencode "client_secret=$THUNDERID_CLIENT_SECRET" \
  -d scope=system \
  --data-urlencode "resource=$THUNDERID_RESOURCE" | jq -r .access_token)"

api() { # api METHOD PATH [JSON_BODY]
  local method="$1" path="$2"
  if [[ $# -ge 3 ]]; then
    "${CURL[@]}" -X "$method" "$THUNDERID_URL$path" -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" --data-binary "$3"
  else
    "${CURL[@]}" -X "$method" "$THUNDERID_URL$path" -H "Authorization: Bearer $TOKEN"
  fi
}

# ── Locate the application and its sign-in flow ──────────────────────────────
APP_ID="$(api GET /applications | jq -r --arg n "$FRONTEND_APP_NAME" '.applications[] | select(.name == $n) | .id' | head -1)"
[[ -n "$APP_ID" ]] || { echo "No ThunderID application named '$FRONTEND_APP_NAME'." >&2; exit 1; }
APP="$(api GET "/applications/$APP_ID")"
SIGNIN_FLOW_ID="$(jq -r .authFlowId <<<"$APP")"
echo "Application: $FRONTEND_APP_NAME ($APP_ID), sign-in flow $SIGNIN_FLOW_ID"

FLOWS="$(api GET '/flows?limit=100')"

# ── 1. CoreGrid recovery flow ────────────────────────────────────────────────
RECOVERY_FLOW_ID="$(jq -r --arg h "$RECOVERY_FLOW_HANDLE" '.flows[] | select(.flowType == "RECOVERY" and .handle == $h) | .id' <<<"$FLOWS" | head -1)"
DEFAULT_RECOVERY_ID="$(jq -r '.flows[] | select(.flowType == "RECOVERY" and .handle == "default-flow") | .id' <<<"$FLOWS" | head -1)"
[[ -n "$DEFAULT_RECOVERY_ID" ]] || { echo "ThunderID's default recovery flow was not found." >&2; exit 1; }

RECOVERY_BODY="$(api GET "/flows/$DEFAULT_RECOVERY_ID" | jq --arg signin "$SIGNIN_FLOW_ID" \
  --arg handle "$RECOVERY_FLOW_HANDLE" --arg name "$RECOVERY_FLOW_NAME" '
  { handle: $handle, name: $name, flowType: "RECOVERY",
    nodes: (.nodes | map(if .type == "CALL" then .flow.ref = $signin else . end)) }')"

if [[ -z "$RECOVERY_FLOW_ID" ]]; then
  RECOVERY_FLOW_ID="$(api POST /flows "$RECOVERY_BODY" | jq -r .id)"
  echo "Created $RECOVERY_FLOW_NAME ($RECOVERY_FLOW_ID)"
else
  echo "$RECOVERY_FLOW_NAME already exists ($RECOVERY_FLOW_ID)"
fi

# ── 2. "Forgot password?" link on the sign-in flow ───────────────────────────
SIGNIN_FLOW="$(api GET "/flows/$SIGNIN_FLOW_ID")"
if jq -e '.nodes[] | select(.id == "call_recovery")' <<<"$SIGNIN_FLOW" >/dev/null; then
  echo "Sign-in flow already links to recovery"
else
  UPDATED_SIGNIN="$(jq --arg rec "$RECOVERY_FLOW_ID" '
    def forgot_link: {
      id: "rich_text_forgot_password", type: "RICH_TEXT", category: "DISPLAY", resourceType: "ELEMENT",
      action: { ref: "action_forgot_password" },
      label: "<p data-component-ref=\"recovery-link\" class=\"rich-text-paragraph\"><span class=\"rich-text-pre-wrap\">{{ t(signin:forms.credentials.links.forgot_password.prefix) }} </span><a href=\"#\" data-action-ref=\"action_forgot_password\" class=\"rich-text-link\"><span class=\"rich-text-pre-wrap\">{{ t(signin:forms.credentials.links.forgot_password.label) }}</span></a></p>"
    };
    # The prompt that collects the password is the one that gets the link,
    # placed just above its submit button.
    def is_password_prompt: .type == "PROMPT" and ([.prompts[]?.inputs[]?.type] | index("PASSWORD_INPUT"));
    { handle, name, flowType,
      nodes: ((.nodes | map(
        if is_password_prompt then
          .meta.components |= map(
            if .type == "BLOCK" and ([.components[]?.type] | index("PASSWORD_INPUT")) then
              .components |= (
                (map(.type == "ACTION" and .eventType == "SUBMIT") | index(true)) as $i
                | if $i == null then . + [forgot_link] else .[:$i] + [forgot_link] + .[$i:] end)
            else . end)
          | .prompts += [{ action: { ref: "action_forgot_password", nextNode: "call_recovery" } }]
        else . end))
        + [{ id: "call_recovery", type: "CALL", onSuccess: "end", flow: { ref: $rec },
             layout: { size: { width: 260, height: 129 }, position: { x: 1255, y: 773 } } }]) }' <<<"$SIGNIN_FLOW")"
  jq -e '[.nodes[] | select(.prompts[]?.action.ref == "action_forgot_password")] | length == 1' <<<"$UPDATED_SIGNIN" >/dev/null \
    || { echo "Could not find the password prompt in the sign-in flow; left it unchanged." >&2; exit 1; }
  api PUT "/flows/$SIGNIN_FLOW_ID" "$UPDATED_SIGNIN" >/dev/null
  echo "Added 'Forgot password?' to the sign-in flow"
fi

# ── 3. Enable recovery on the application ────────────────────────────────────
if jq -e --arg rec "$RECOVERY_FLOW_ID" '.isRecoveryFlowEnabled == true and .recoveryFlowId == $rec' <<<"$APP" >/dev/null; then
  echo "Recovery already enabled on $FRONTEND_APP_NAME"
else
  api PUT "/applications/$APP_ID" "$(jq --arg rec "$RECOVERY_FLOW_ID" '.recoveryFlowId = $rec | .isRecoveryFlowEnabled = true' <<<"$APP")" >/dev/null
  echo "Enabled recovery on $FRONTEND_APP_NAME"
fi

echo
echo "Done. Set VITE_THUNDERID_APPLICATION_ID=$APP_ID in frontend/.env so CoreGrid's"
echo "'Forgot password?' page can open $THUNDERID_URL/gate/recovery directly."
