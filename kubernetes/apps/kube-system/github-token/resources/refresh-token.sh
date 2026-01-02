#!/bin/sh
set -euo pipefail

KEY_PATH="/secrets/private-key.pem"
SECRET_NAME="github-status"
SA_TOKEN_PATH="/var/run/secrets/kubernetes.io/serviceaccount/token"
CA_CERT_PATH="/var/run/secrets/kubernetes.io/serviceaccount/ca.crt"

# Ensure dependencies are available (curl + git for go install)
apk add --no-cache curl ca-certificates git >/dev/null

echo "Generating GitHub access token for App ID $GITHUB_APP_ID and Installation ID $GITHUB_APP_INSTALLATION_ID"

ACCESS_TOKEN="$(go run github.com/slawekzachcial/gha-token@latest --appId $GITHUB_APP_ID --keyPath $KEY_PATH --installId $GITHUB_APP_INSTALLATION_ID)"

token_b64=$(printf '%s' "$ACCESS_TOKEN" | base64 | tr -d '\n')
service_account_token=$(cat "$SA_TOKEN_PATH")

PATCH_PAYLOAD="{\"metadata\":{\"annotations\":{\"reloader.stakater.com/auto\":\"true\",\"reflector.v1.k8s.emberstack.com/reflection-allowed\":\"true\",\"reflector.v1.k8s.emberstack.com/reflection-auto-enabled\":\"true\"}},\"data\":{\"token\":\"${token_b64}\"}}"

curl -sSf \
  --cacert "$CA_CERT_PATH" \
  -H "Authorization: Bearer $service_account_token" \
  -H "Content-Type: application/merge-patch+json" \
  -X PATCH \
  -d "$PATCH_PAYLOAD" \
  "https://kubernetes.default.svc.cluster.local/api/v1/namespaces/flux-system/secrets/$SECRET_NAME"

echo "Successfully updated GitHub token secret"
