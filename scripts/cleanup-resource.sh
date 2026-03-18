#!/bin/bash

# Function to try deleting and remove finalizers from a resource type
try_delete_and_remove_finalizers() {
  local resource="$1"
  shift
  echo "Trying to delete: $resource"
  for selector in "$@"; do
    kubectl get "$resource" -l "$selector" -n "$NAMESPACE" -o name 2>/dev/null | \
      xargs -r kubectl patch -n "$NAMESPACE" --type json -p='[{"op":"remove","path":"/metadata/finalizers"}]' 2>/dev/null

    # Try deletion first
    kubectl get "$resource" -l "$selector" -n "$NAMESPACE" -o name 2>/dev/null | \
      xargs -r kubectl delete -n "$NAMESPACE" 2>/dev/null
  done
}

NAME="$1"
NAMESPACE="$2"

if [ -z "$NAME" ] || [ -z "$NAMESPACE" ]; then
  echo "Usage: $0 <app-name> <namespace>"
  exit 1
fi

echo "Cleaning up all resources for: $NAME in namespace: $NAMESPACE"
echo ""

# Define label selectors to try
LABEL_SELECTORS=(
  "app.kubernetes.io/instance=$NAME"
  "app.kubernetes.io/name=$NAME"
  "driscoll.dev/name=$NAME"
  "helm.toolkit.fluxcd.io/name=$NAME"
  "kustomize.toolkit.fluxcd.io/name=$NAME"
)

# Loop over all namespaced resource types
RESOURCE_TYPES=$(kubectl api-resources --namespaced=true -o name 2>/dev/null)

for resource in $RESOURCE_TYPES; do
  echo "Checking $resource..."

  try_delete_and_remove_finalizers "$resource" "${LABEL_SELECTORS[@]}"
done

# Delete Helm release secrets explicitly
echo "Cleaning up Helm release secrets..."
kubectl get secret -n $NAMESPACE 2>/dev/null | grep "sh.helm.release.v1.$NAME" | awk '{print $1}' | \
  xargs -r kubectl delete secret -n $NAMESPACE 2>/dev/null

echo "✓ Cleanup completed"

