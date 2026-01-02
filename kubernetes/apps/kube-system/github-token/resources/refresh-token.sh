#!/bin/sh
set -euo pipefail

APP_ID="${GITHUB_APP_ID:?GITHUB_APP_ID is required}"
INSTALL_ID="${GITHUB_INSTALLATION_ID:?GITHUB_INSTALLATION_ID is required}"
KEY_PATH="/secrets/private-key.pem"
NAMESPACE="${NAMESPACE:-flux-system}"
SECRET_NAME="github-status-token-secret"
GOBIN="/tmp/bin"
BINARY="${GOBIN}/gha-token"
SA_TOKEN_PATH="/var/run/secrets/kubernetes.io/serviceaccount/token"
CA_CERT_PATH="/var/run/secrets/kubernetes.io/serviceaccount/ca.crt"

# Ensure dependencies are available (curl + git for go install)
apk add --no-cache curl ca-certificates git >/dev/null

mkdir -p "${GOBIN}"
export GOBIN
if [ ! -x "${BINARY}" ]; then
  go install github.com/slawekzachcial/gha-token@latest
fi

ACCESS_TOKEN="$("${BINARY}" --appId "${APP_ID}" --keyPath "${KEY_PATH}" --installId "${INSTALL_ID}")"

token_b64=$(printf '%s' "${ACCESS_TOKEN}" | base64 | tr -d '\n')
service_account_token=$(cat "${SA_TOKEN_PATH}")

PATCH_PAYLOAD="{\"metadata\":{\"annotations\":{\"reloader.stakater.com/auto\":\"true\",\"reflector.v1.k8s.emberstack.com/reflection-allowed\":\"true\",\"reflector.v1.k8s.emberstack.com/reflection-auto-enabled\":\"true\"}},\"data\":{\"token\":\"${token_b64}\"}}"

curl -sSf \
  --cacert "${CA_CERT_PATH}" \
  -H "Authorization: Bearer ${service_account_token}" \
  -H "Content-Type: application/merge-patch+json" \
  -X PATCH \
  -d "${PATCH_PAYLOAD}" \
  "https://kubernetes.default.svc.cluster.local/api/v1/namespaces/${NAMESPACE}/secrets/${SECRET_NAME}"

echo "Successfully updated GitHub token secret"
