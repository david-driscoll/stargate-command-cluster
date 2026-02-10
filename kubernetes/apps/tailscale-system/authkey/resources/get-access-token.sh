#!/bin/bash

curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.35/deb/Release.key | gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
chmod 644 /etc/apt/keyrings/kubernetes-apt-keyring.gpg
echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.35/deb/ /' | tee /etc/apt/sources.list.d/kubernetes.list
chmod 644 /etc/apt/sources.list.d/kubernetes.list

apt-get update
apt-get install -y curl jq ca-certificates kubectl
kubectl version

authkey=$(go run tailscale.com/cmd/get-authkey@latest -ephemeral -reusable -preauth -tags "tag:${CLUSTER_CNAME}")

echo "authkey: $authkey"

token_response=$(curl -d "client_id=$TS_API_CLIENT_ID" -d "client_secret=$TS_API_CLIENT_SECRET" "https://api.tailscale.com/api/v2/oauth/token")
access_token=$(echo $token_response | jq -r '.access_token')
echo "access_token: $access_token"

kubectl create secret generic access-token --from-literal=token="$access_token" --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret access-token reflector.v1.k8s.emberstack.com/reflection-allowed='true' --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret access-token reloader.stakater.com/auto='true' --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret generic authkey --from-literal=authkey="$authkey" --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret authkey reflector.v1.k8s.emberstack.com/reflection-allowed='true' --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret authkey reloader.stakater.com/auto='true' --dry-run=client -o yaml | kubectl apply -f -
