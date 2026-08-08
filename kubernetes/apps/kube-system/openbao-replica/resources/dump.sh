#!/usr/bin/env bash
#
# dump.sh — nightly break-glass replication of the `openbao` database to
# alpha-site (migration Phase 5; PLAN §E, RUNBOOK Scenario B).
#
#   pg_dump → age encrypt → SFTP to the bao-standby dump receiver → prune 30d
#
# The age layer is ON TOP of OpenBao's own encryption, deliberately:
# alpha-site also hosts the transit key, so possession of that one host must
# never be enough to read the estate's secrets. Recipients are the three
# estate age keys plus the dedicated restore-test key — see
# age-recipients.txt, which is the authoritative list.
#
# Environment (set in helmrelease.yaml):
#   PG_URI            connection string for the openbao database
#   SFTP_HOST/PORT    the bao-standby dump receiver on dockge-as (port 2023)
#   SFTP_KEY_FILE     the estate rclone SFTP client key
#   RETENTION_DAYS    prune horizon on alpha-site (30)
#   GATUS_URL, GATUS_CONNECT_TO, GATUS_TOKEN   heartbeat reporting
#
# NOTE for editors: this file is delivered through a Flux-substituted
# ConfigMap. Keep every shell variable lowercase or $unbraced — a $${UPPERCASE}
# that collides with a cluster substitution variable would be rewritten at
# deploy time.
set -Eeuo pipefail

report() {
  # Report failures loudly, successes quietly. Never let the report itself
  # fail the job (|| true): Gatus's heartbeat catches a job that could not
  # even report.
  local success="$1" msg="${2:-}"
  local url="$GATUS_URL/api/v1/endpoints/$GATUS_TOKEN/external?success=$success"
  if [ "$success" != "true" ] && [ -n "$msg" ]; then
    url="$url&error=$(printf '%s' "$msg" | head -c 512 | od -An -tx1 -v | tr -d ' \n' | sed 's/../%&/g')"
  fi
  curl -sf -X POST \
    --connect-to "$GATUS_CONNECT_TO" \
    -H "Authorization: Bearer $GATUS_TOKEN" \
    "$url" >/dev/null || echo "WARN: failed to report to Gatus"
}

fail() {
  echo "FAILED: $*" >&2
  report false "$*"
  exit 1
}
trap 'fail "dump.sh aborted at line $LINENO"' ERR

stamp="$(date -u +%Y%m%d-%H%M%S)"
name="openbao-$stamp.sql.age"
plain=/tmp/openbao.sql
enc="/tmp/$name"

# rclone remote for the dump receiver. No host-key pinning, matching the
# estate's other rclone-over-sftp jobs (Playground.cs, backrest preSync).
export RCLONE_CONFIG=/tmp/rclone.conf
export RCLONE_CONFIG_BAO_TYPE=sftp
export RCLONE_CONFIG_BAO_HOST="$SFTP_HOST"
export RCLONE_CONFIG_BAO_PORT="$SFTP_PORT"
export RCLONE_CONFIG_BAO_USER=sftp
export RCLONE_CONFIG_BAO_KEY_FILE="$SFTP_KEY_FILE"

# 1. Dump. --schema=openbao because that is where every OpenBao table lives
#    (the role's search_path puts them there — see STATUS.md, "tables live in
#    the openbao schema"). --no-owner/--no-privileges so the dump restores
#    into a scratch server that has no CNPG roles. Plain format on purpose:
#    RUNBOOK Scenario B pipes it straight into psql.
pg_dump "$PG_URI" --schema=openbao --format=plain --no-owner --no-privileges > "$plain"

# A dump of a healthy openbao database is never tiny. An empty-ish file here
# means we dumped the wrong thing (or a wiped database) — do not ship it over
# the last known-good ones.
size="$(wc -c < "$plain")"
[ "$size" -ge 65536 ] || fail "dump is only $size bytes — refusing to ship it"

# 2. Encrypt. -R takes the recipients file; comments and blank lines are fine.
age -R /scripts/age-recipients.txt -o "$enc" "$plain"
rm -f "$plain"

# 3. Ship, then read it back from the listing — the upload only counts if the
#    receiver actually has it at the right size.
rclone copyto "$enc" "bao:$name"
remote_size="$(rclone lsl bao: --include "$name" | awk '{print $1}')"
[ "$remote_size" = "$(wc -c < "$enc")" ] || fail "uploaded size mismatch for $name (local $(wc -c < "$enc"), remote ${remote_size:-missing})"

# 4. Prune to the 30-day retention. Scoped to our own filename pattern so a
#    stray file in the receiver directory can never be collateral.
rclone delete bao: --min-age "$RETENTION_DAYS"d --include 'openbao-*.sql.age'

count="$(rclone lsf bao: --include 'openbao-*.sql.age' | wc -l | tr -d ' ')"
echo "shipped $name ($size bytes plaintext); $count dump(s) retained on alpha-site"
report true
