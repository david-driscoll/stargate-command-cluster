#!/bin/sh

curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.35/deb/Release.key | gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
chmod 644 /etc/apt/keyrings/kubernetes-apt-keyring.gpg
echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.35/deb/ /' | tee /etc/apt/sources.list.d/kubernetes.list
chmod 644 /etc/apt/sources.list.d/kubernetes.list

apt-get update
apt-get install -y curl jq ca-certificates kubectl
kubectl version

echo "Generating GitHub access token for App ID $GITHUB_APP_ID and Installation ID $GITHUB_APP_INSTALLATION_ID"

ACCESS_TOKEN="$(go run github.com/slawekzachcial/gha-token@latest --appId $GITHUB_APP_ID --keyPath /secrets/private-key.pem --repo ${GITHUB_REPOSITORY_OWNER})"

echo "access_token: $ACCESS_TOKEN"

kubectl create secret generic github-token --from-literal=token="$ACCESS_TOKEN" --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret github-token reflector.v1.k8s.emberstack.com/reflection-allowed='true' --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret github-token reflector.v1.k8s.emberstack.com/reflection-auto-enabled='true' --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret github-token reloader.stakater.com/auto='true' --dry-run=client -o yaml | kubectl apply -f -
