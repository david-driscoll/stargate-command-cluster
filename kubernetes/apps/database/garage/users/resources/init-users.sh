#!/bin/sh
set -x
# Set the namespace and pod name for garage
NAMESPACE="database"
POD="garage-0"
GARAGE_CMD="kubectl exec -n $NAMESPACE $POD -- ./garage"

$GARAGE_CMD key import -n cluster-user --yes "$GARAGE_USER_CLUSTER_USER" "$GARAGE_PASSWORD_CLUSTER_USER" || true
$GARAGE_CMD key allow --create-bucket cluster-user
$GARAGE_CMD key import -n authentik --yes "$GARAGE_USER_AUTHENTIK" "$GARAGE_PASSWORD_AUTHENTIK" || true
$GARAGE_CMD bucket create authentik || true
$GARAGE_CMD bucket allow --read --write --owner authentik --key authentik
$GARAGE_CMD bucket allow --read --write authentik --key cluster-user
$GARAGE_CMD bucket create iris || true
$GARAGE_CMD bucket allow --read --write --owner iris --key authentik
$GARAGE_CMD bucket allow --read --write iris --key cluster-user
$GARAGE_CMD bucket website --allow iris
$GARAGE_CMD key import -n tivi-sync --yes "$GARAGE_USER_TIVI_SYNC" "$GARAGE_PASSWORD_TIVI_SYNC" || true
$GARAGE_CMD bucket create tivi-cache || true
$GARAGE_CMD bucket allow --read --write --owner tivi-cache --key tivi-sync
$GARAGE_CMD bucket allow --read --write tivi-cache --key cluster-user
$GARAGE_CMD bucket create tivi-results || true
$GARAGE_CMD bucket allow --read --write --owner tivi-results --key tivi-sync
$GARAGE_CMD bucket allow --read --write tivi-results --key cluster-user
$GARAGE_CMD bucket website --allow tivi-results