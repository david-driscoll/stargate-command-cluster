#!/bin/bash

apt install kubelet kubeadm kubectl -y

authkey=$(go run tailscale.com/cmd/get-authkey@latest -reusable -preauthorized -tags "tag:${CLUSTER_CNAME}")

echo "authkey: $authkey"

token_response=$(curl -d "client_id=$OAUTH_CLIENT_ID" -d "client_secret=$OAUTH_CLIENT_SECRET" "https://api.tailscale.com/api/v2/oauth/token")
access_token=$(echo $token_response | jq -r '.access_token')
echo "access_token: $access_token"

kubectl create secret generic tailscale-access-token --from-literal=token="$access_token" --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret tailscale-access-token reflector.v1.k8s.emberstack.com/reflection-allowed='true' --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret tailscale-access-token reloader.stakater.com/auto='true' --dry-run=client -o yaml | kubectl apply -f -

kubectl create secret generic tailscale-authkey --from-literal=authkey="$authkey" --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret tailscale-authkey reflector.v1.k8s.emberstack.com/reflection-allowed='true' --dry-run=client -o yaml | kubectl apply -f -
kubectl annotate secret tailscale-authkey reloader.stakater.com/auto='true' --dry-run=client -o yaml | kubectl apply -f -
