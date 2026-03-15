#!/bin/bash

NAME="$1"
NAMESPACE="$2"

if [ -z "$NAME" ] || [ -z "$NAMESPACE" ]; then
  echo "Usage: $0 <app-name> <namespace>"
  exit 1
fi

echo "Cleaning up all resources for: $NAME in namespace: $NAMESPACE"
echo ""

# Loop over all namespaced resource types
RESOURCE_TYPES=$(kubectl api-resources --namespaced=true -o name 2>/dev/null)

for resource in $RESOURCE_TYPES; do
  echo "Checking $resource..."

  # Try multiple label patterns
  kubectl get "$resource" -l app.kubernetes.io/instance=$NAME -n $NAMESPACE -o name 2>/dev/null | \
    xargs -r kubectl delete "$resource" -n $NAMESPACE 2>/dev/null

  kubectl get "$resource" -l helm.toolkit.fluxcd.io/name=$NAME -n $NAMESPACE -o name 2>/dev/null | \
    xargs -r kubectl delete "$resource" -n $NAMESPACE 2>/dev/null

  kubectl get "$resource" -l app.kubernetes.io/name=$NAME -n $NAMESPACE -o name 2>/dev/null | \
    xargs -r kubectl delete "$resource" -n $NAMESPACE 2>/dev/null
done

# Delete Helm release secrets explicitly
echo "Cleaning up Helm release secrets..."
kubectl get secret -n $NAMESPACE 2>/dev/null | grep "sh.helm.release.v1.$NAME" | awk '{print $1}' | \
  xargs -r kubectl delete secret -n $NAMESPACE 2>/dev/null

echo "✓ Cleanup completed"

