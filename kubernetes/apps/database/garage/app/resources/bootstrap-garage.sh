#!/bin/bash

set -ex

handle_error() {
  echo "Error on line $1"
  exit 1
}

trap 'handle_error $LINENO' ERR

NAMESPACE="database"
POD=$(kubectl get pods -n database -l app.kubernetes.io/controller=garage -o json | jq -r '.items[0].metadata.name')
GARAGE_CMD="kubectl exec -n $NAMESPACE $POD -- ./garage"
# Fetch all garage node IDs from the garagenodes.deuxfleurs.fr CRD in the cluster
NODES=$(kubectl get garagenodes.deuxfleurs.fr -A -o json | jq -r '.items[].metadata.name')
NODE_COUNT=$(echo "$NODES" | wc -l)

# Get garage status and count "NO ROLE ASSIGNED" phrases
NO_ROLE_ASSIGNED=$($GARAGE_CMD status | grep -c "NO ROLE ASSIGNED" || echo 0)
# if no role assigned is zero exit 0
if [ "$NO_ROLE_ASSIGNED" -eq 0 ]; then
  echo "All nodes have roles assigned."
  exit 0
fi

echo "No role assigned count: $NO_ROLE_ASSIGNED"

# Count running containers in the garage stateful set
RUNNING_CONTAINERS=$(kubectl get pods -n database -l app.kubernetes.io/controller=garage -o json | jq '[.items[].status.containerStatuses[] | select(.ready == true)] | length')

echo "Garage nodes: $NODE_COUNT, No role assigned: $NO_ROLE_ASSIGNED, Running containers: $RUNNING_CONTAINERS"

if [ -z "$NODES" ]; then
  echo "No garage nodes found. Exiting."
  exit 1
fi

if [ "$NO_ROLE_ASSIGNED" -eq "$NODE_COUNT" ] && [ "$RUNNING_CONTAINERS" -eq "$NO_ROLE_ASSIGNED" ]; then
  echo "No layout assigned yet. Assigning layout..."
  for NODE_ID in $NODES; do
    $GARAGE_CMD layout assign "$NODE_ID" -z sgc -c 64G
  done

  $GARAGE_CMD layout show
  $GARAGE_CMD layout apply --version 1
else
  echo "Layout already assigned. Skipping assignment."
fi

