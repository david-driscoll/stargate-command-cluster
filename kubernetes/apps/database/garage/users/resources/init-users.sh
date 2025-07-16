#!/bin/sh
set -e

# Set the namespace and pod name for garage
NAMESPACE="database"
POD="garage-0"
GARAGE_CMD="kubectl exec -n $NAMESPACE $POD -- ./garage"

$GARAGE_CMD key import -n cluster-user --yes "$GARAGE_USER_CLUSTER_USER" "$GARAGE_PASSWORD_CLUSTER_USER"
$GARAGE_CMD key allow --create-bucket cluster-user
$GARAGE_CMD key import -n authentik "$GARAGE_USER_AUTHENTIK" "$GARAGE_PASSWORD_AUTHENTIK"
$GARAGE_CMD bucket create authentik/postgres
$GARAGE_CMD bucket allow --read --write --owner authentik/postgres --key authentik
$GARAGE_CMD bucket create iris
$GARAGE_CMD bucket allow --read --write --owner iris --key authentik
$GARAGE_CMD bucket website --allow iris
$GARAGE_CMD key import -n postgres-sgc "$GARAGE_USER_POSTGRES_SGC" "$GARAGE_PASSWORD_POSTGRES_SGC"
$GARAGE_CMD bucket create postgres
$GARAGE_CMD bucket allow --read --write --owner postgres --key postgres-sgc
$GARAGE_CMD key import -n tivi-sync "$GARAGE_USER_TIVI_SYNC" "$GARAGE_PASSWORD_TIVI_SYNC"
$GARAGE_CMD bucket create tivi-cache
$GARAGE_CMD bucket allow --read --write --owner tivi-cache --key tivi-sync
$GARAGE_CMD bucket create tivi-results
$GARAGE_CMD bucket allow --read --write --owner tivi-results --key tivi-sync
$GARAGE_CMD bucket website --allow tivi-results