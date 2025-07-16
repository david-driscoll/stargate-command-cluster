#!/bin/sh
set -e

# Set the namespace and pod name for garage
NAMESPACE="database"
POD="garage-0"
GARAGE_CMD="kubectl exec -n $NAMESPACE $POD -- ./garage"

$GARAGE_CMD key import -n cluster-user --yes "$GARAGE_USER_CLUSTER_USER" "$GARAGE_PASSWORD_CLUSTER_USER" || true
$GARAGE_CMD key allow --create-bucket cluster-user || true
$GARAGE_CMD key import -n authentik "$GARAGE_USER_AUTHENTIK" "$GARAGE_PASSWORD_AUTHENTIK" || true
$GARAGE_CMD bucket create authentik/postgres || true
$GARAGE_CMD bucket allow --read --write --owner authentik/postgres --key authentik || true
$GARAGE_CMD bucket create iris || true
$GARAGE_CMD bucket allow --read --write --owner iris --key authentik || true
$GARAGE_CMD bucket website --allow iris || true
$GARAGE_CMD key import -n postgres-sgc "$GARAGE_USER_POSTGRES_SGC" "$GARAGE_PASSWORD_POSTGRES_SGC" || true
$GARAGE_CMD bucket create postgres || true
$GARAGE_CMD bucket allow --read --write --owner postgres --key postgres-sgc || true
$GARAGE_CMD key import -n tivi-sync "$GARAGE_USER_TIVI_SYNC" "$GARAGE_PASSWORD_TIVI_SYNC" || true
$GARAGE_CMD bucket create tivi-cache || true
$GARAGE_CMD bucket allow --read --write --owner tivi-cache --key tivi-sync || true
$GARAGE_CMD bucket create tivi-results || true
$GARAGE_CMD bucket allow --read --write --owner tivi-results --key tivi-sync || true
$GARAGE_CMD bucket website --allow tivi-results || true