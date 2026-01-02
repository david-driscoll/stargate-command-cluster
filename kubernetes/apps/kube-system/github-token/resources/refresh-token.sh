#!/bin/sh

apt-get update
apt-get install -y curl jq ca-certificates
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x ./kubectl
./kubectl version

echo "Generating GitHub access token for App ID $GITHUB_APP_ID and Installation ID $GITHUB_APP_INSTALLATION_ID"

ACCESS_TOKEN="$(go run github.com/slawekzachcial/gha-token@latest --appId $GITHUB_APP_ID --keyPath /secrets/private-key.pem --installId $GITHUB_APP_INSTALLATION_ID)"

echo "access_token: $ACCESS_TOKEN"

./kubectl create secret generic github-token --from-literal=token="$ACCESS_TOKEN" --dry-run=client -o yaml | ./kubectl apply -f -
./kubectl annotate secret github-token reflector.v1.k8s.emberstack.com/reflection-allowed='true' --dry-run=client -o yaml | ./kubectl apply -f -
./kubectl annotate secret github-token reflector.v1.k8s.emberstack.com/reflection-auto-enabled='true' --dry-run=client -o yaml | ./kubectl apply -f -
./kubectl annotate secret github-token reloader.stakater.com/auto='true' --dry-run=client -o yaml | ./kubectl apply -f -
