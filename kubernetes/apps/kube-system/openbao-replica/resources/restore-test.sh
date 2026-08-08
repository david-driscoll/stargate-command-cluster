#!/usr/bin/env bash
#
# restore-test.sh — monthly proof that the break-glass backup works
# (migration Phase 5; RUNBOOK Scenario D: an unverified backup is not a
# backup).
#
# This is Scenario B as a drill, end to end, with nothing mocked:
#
#   1. fetch the NEWEST dump back from alpha-site over the same SFTP path the
#      nightly job ships through — proving the dump left the building and is
#      retrievable
#   2. refuse a stale dump (older than MAX_DUMP_AGE_DAYS) — a clean restore
#      of last month's data is still a failed backup
#   3. age-decrypt with the estate identity — the exact key a human would use
#   4. restore into the scratch Postgres sidecar
#   5. start a throwaway bao against it and let it unseal via the SAME
#      transit key on alpha-site the real cluster uses
#   6. log in with the restore-test AppRole and read the canary key
#   7. report to Gatus — which alerts on failure AND on silence
#
# Environment (set in helmrelease.yaml): SFTP_HOST/PORT/KEY_FILE,
# AGE_KEY_FILE, MAX_DUMP_AGE_DAYS, SCRATCH_PG_URI/USER/PASSWORD/DB,
# TRANSIT_ADDR, VAULT_TRANSIT_SEAL_TOKEN, BAO_ROLE_ID, BAO_SECRET_ID,
# CANARY_PATH, GATUS_URL/CONNECT_TO/TOKEN.
#
# NOTE for editors: delivered through a Flux-substituted ConfigMap — keep
# shell expansions $unbraced so no $${UPPERCASE} can collide with a cluster
# substitution variable.
set -Eeuo pipefail

bao_pid=""

report() {
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

cleanup() { [ -n "$bao_pid" ] && kill "$bao_pid" 2>/dev/null || true; }

fail() {
  echo "RESTORE TEST FAILED: $*" >&2
  echo "Treat the backup as UNTRUSTED until this passes — RUNBOOK Scenario D." >&2
  report false "$*"
  cleanup
  exit 1
}
trap 'fail "restore-test.sh aborted at line $LINENO"' ERR

# ── 1. Fetch the newest dump back from alpha-site ───────────────────────────
export RCLONE_CONFIG=/tmp/rclone.conf
export RCLONE_CONFIG_BAO_TYPE=sftp
export RCLONE_CONFIG_BAO_HOST="$SFTP_HOST"
export RCLONE_CONFIG_BAO_PORT="$SFTP_PORT"
export RCLONE_CONFIG_BAO_USER=sftp
export RCLONE_CONFIG_BAO_KEY_FILE="$SFTP_KEY_FILE"

latest="$(rclone lsf bao: --include 'openbao-*.sql.age' | sort | tail -1)"
[ -n "$latest" ] || fail "no dumps found on alpha-site at all"
echo "newest dump on alpha-site: $latest"

# ── 2. Freshness gate ───────────────────────────────────────────────────────
# The timestamp is authoritative from the FILENAME (openbao-YYYYMMDD-HHMMSS),
# which the dump job wrote from its own clock — mtime over sftp is not
# trustworthy enough to gate on.
dump_date="$(printf '%s' "$latest" | sed -n 's/^openbao-\([0-9]\{8\}\)-.*/\1/p')"
[ -n "$dump_date" ] || fail "cannot parse a date out of dump name '$latest'"
# Epoch arithmetic instead of `date -d "-N days"` — that flag does not exist
# in busybox and this container's date is coreutils only because the command
# wrapper installs it.
cutoff="$(date -u -d "@$(( $(date -u +%s) - MAX_DUMP_AGE_DAYS * 86400 ))" +%Y%m%d)"
[ "$dump_date" -ge "$cutoff" ] || fail "newest dump $latest is older than $MAX_DUMP_AGE_DAYS days — nightly replication has stopped"

rclone copyto "bao:$latest" /tmp/dump.sql.age

# ── 3. Decrypt with the estate identity ─────────────────────────────────────
age --decrypt -i "$AGE_KEY_FILE" -o /tmp/dump.sql /tmp/dump.sql.age
size="$(wc -c < /tmp/dump.sql)"
[ "$size" -ge 65536 ] || fail "decrypted dump is only $size bytes"

# ── 4. Restore into the scratch sidecar ─────────────────────────────────────
export PGPASSWORD="$SCRATCH_PG_PASSWORD"
for _ in $(seq 1 60); do
  pg_isready -h localhost -U "$SCRATCH_PG_USER" -d "$SCRATCH_PG_DB" >/dev/null 2>&1 && break
  sleep 2
done
pg_isready -h localhost -U "$SCRATCH_PG_USER" -d "$SCRATCH_PG_DB" >/dev/null || fail "scratch postgres never became ready"

# ON_ERROR_STOP: a partial restore that limps into a working-looking bao is
# the worst possible outcome for a verification job.
psql "$SCRATCH_PG_URI" -q -v ON_ERROR_STOP=1 -f /tmp/dump.sql
tables="$(psql "$SCRATCH_PG_URI" -tA -c "select count(*) from information_schema.tables where table_schema='openbao'")"
echo "restored $tables table(s) into schema openbao"
entries="$(psql "$SCRATCH_PG_URI" -tA -c "select count(*) from openbao.openbao_kv_store")"
[ "$entries" -gt 0 ] || fail "openbao_kv_store restored empty"
echo "openbao_kv_store has $entries rows"

# ── 5. Throwaway bao, unsealed via the real transit path ────────────────────
# Single node, no HA, no service registration. connection_url and the transit
# token arrive via BAO_PG_CONNECTION_URL / VAULT_TRANSIT_SEAL_TOKEN — the
# same env-beats-config behaviour the production config relies on.
cat > /tmp/config.hcl <<EOF
listener "tcp" {
  address     = "127.0.0.1:8200"
  tls_disable = true
}
storage "postgresql" {
  table      = "openbao_kv_store"
  ha_enabled = "false"
}
seal "transit" {
  address    = "$TRANSIT_ADDR"
  key_name   = "openbao-equestria-unseal"
  mount_path = "transit/"
}
EOF
export BAO_PG_CONNECTION_URL="$SCRATCH_PG_URI?sslmode=disable"
bao server -config=/tmp/config.hcl &
bao_pid=$!

# Auto-unseal happens during startup; then there is a window where non-status
# requests 500 while the (single) node elects itself — the same ~30s window
# equestria-init.sh waits out. Gate on health 200 = initialised, unsealed,
# active.
export BAO_ADDR=http://127.0.0.1:8200
unsealed=""
for _ in $(seq 1 90); do
  code="$(curl -s -o /tmp/health.json -w '%{http_code}' "$BAO_ADDR/v1/sys/health" || true)"
  if [ "$code" = "200" ]; then unsealed=yes; break; fi
  kill -0 "$bao_pid" 2>/dev/null || fail "bao server exited during startup — transit seal unreachable or restored keyring unusable"
  sleep 2
done
[ -n "$unsealed" ] || fail "bao never reached health 200 (last: $(cat /tmp/health.json 2>/dev/null || echo none)) — transit unseal of the RESTORED data failed"
echo "throwaway bao is unsealed and active (via $TRANSIT_ADDR)"

# ── 6. Canary read with the restore-test AppRole ────────────────────────────
# The AppRole lives INSIDE the backup (minted against the live cluster by
# vault/bootstrap/openbao/restore-test.sh), so a successful login also proves
# the auth backends survived the round trip.
token="$(curl -sf -X POST "$BAO_ADDR/v1/auth/approle/login" \
  -d "{\"role_id\":\"$BAO_ROLE_ID\",\"secret_id\":\"$BAO_SECRET_ID\"}" | jq -r '.auth.client_token // empty')"
[ -n "$token" ] || fail "approle login failed against the restored instance — was vault/bootstrap/openbao/restore-test.sh ever run?"

canary="$(curl -sf -H "X-Vault-Token: $token" "$BAO_ADDR/v1/$CANARY_PATH" | jq -r '.data.data | length')"
[ -n "$canary" ] && [ "$canary" -gt 0 ] || fail "canary read of $CANARY_PATH returned no data"
echo "canary read OK ($canary field(s) at $CANARY_PATH)"

# ── 7. Report ───────────────────────────────────────────────────────────────
cleanup
echo "restore test PASSED: $latest restores, unseals via transit, and serves reads"
report true
