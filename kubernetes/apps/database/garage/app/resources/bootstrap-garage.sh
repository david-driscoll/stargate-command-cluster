#!/bin/bash

set -x

NAMESPACE="database"
POD="garage-0"
GARAGE_CMD="kubectl exec -n $NAMESPACE $POD -- ./garage"
# Fetch all garage node IDs from the garagenodes.deuxfleurs.fr CRD in the cluster
NODES=$(kubectl get garagenodes.deuxfleurs.fr -A -o json | jq -r '.items[].metadata.name')
NODE_COUNT=$(echo "$NODES" | wc -l)

# Get garage status and count "pending..." phrases
STATUS=$($GARAGE_CMD status)
PENDING_COUNT=$(echo "$STATUS" | grep -c "pending...")
NO_ROLE_ASSIGNED=$(echo "$STATUS" | grep -c "NO ROLE ASSIGNED")
echo "Pending count: $PENDING_COUNT, No role assigned count: $NO_ROLE_ASSIGNED"
TOTAL_COUNT=$(echo $PENDING_COUNT + $NO_ROLE_ASSIGNED)

# Count running containers in the garage stateful set
RUNNING_CONTAINERS=$(kubectl get pods -n database -l app.kubernetes.io/controller=garage -o json | jq '[.items[].status.containerStatuses[] | select(.ready == true)] | length')

echo "Garage nodes: $NODE_COUNT, Waiting Nodes: $TOTAL_COUNT, Running containers: $RUNNING_CONTAINERS"
echo $STATUS

if [ "$TOTAL_COUNT" -eq "$NODE_COUNT" ] && [ "$RUNNING_CONTAINERS" -eq "$NODE_COUNT" ]; then
  echo "No layout assigned yet. Assigning layout..."
  for NODE_ID in $NODES; do
    $GARAGE_CMD layout assign "$NODE_ID" -z sgc -c 64G
  done

  $GARAGE_CMD layout show
  $GARAGE_CMD layout apply --version 1
else
  echo "Layout already assigned. Skipping assignment."
fi
