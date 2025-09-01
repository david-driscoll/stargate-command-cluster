#!/bin/bash

# Required environment variables (to be set as secrets):
# GITHUB_APP_ID - GitHub App ID
# GITHUB_PRIV_KEY - GitHub App Private Key (PEM format) - note the variable name for go-github-apps
# GITHUB_INSTALLATION_ID - Installation ID for the GitHub App
# GITHUB_USERNAME - Username for docker registry authentication (can be any value for GitHub)

apt-get update
apt-get install -y curl jq
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x ./kubectl
./kubectl version

# Set defaults for optional variables
SECRET_NAME="ghcr-auth"
DOCKER_SERVER="ghcr.io"
DOCKER_USERNAME=${GITHUB_USERNAME:-github}

# Install go-github-apps tool using installation script
echo "Installing go-github-apps..."
# Force IPv4 to avoid network connectivity issues
export GOPROXY=https://proxy.golang.org,direct
# renovate: datasource=github-releases depName=nabeken/go-github-apps
curl -4 -sSLf https://raw.githubusercontent.com/nabeken/go-github-apps/master/install-via-release.sh | bash -s -- -v v0.2.4
chmod +x go-github-apps

echo "Getting installation access token using go-github-apps..."
# Use go-github-apps to get the token (it reads GITHUB_PRIV_KEY automatically)
access_token=$(./go-github-apps -app-id "$GITHUB_APP_ID" -inst-id "$GITHUB_INSTALLATION_ID")

if [ -z "$access_token" ] || [ "$access_token" = "null" ]; then
    echo "Error: Failed to get installation access token"
    exit 1
fi

echo "Installation access token obtained successfully"

# Create docker-registry secret
echo "Creating docker-registry secret: $SECRET_NAME"

kubectl_cmd="./kubectl create secret docker-registry $SECRET_NAME \
    --namespace=kube-system \
    --docker-server=$DOCKER_SERVER \
    --docker-username=$DOCKER_USERNAME \
    --docker-password=$access_token \
    --docker-email=noreply@github.com"

# Execute with dry-run first, then apply
$kubectl_cmd --dry-run=client -o yaml | ./kubectl apply -f -

# Add annotations for reflection and reloading
echo "Adding annotations to secret..."
./kubectl annotate secret $SECRET_NAME reloader.stakater.com/auto='true' --namespace=kube-system --overwrite
./kubectl annotate secret $SECRET_NAME reflector.v1.k8s.emberstack.com/reflection-allowed='true' --namespace=kube-system --overwrite
./kubectl annotate secret $SECRET_NAME reflector.v1.k8s.emberstack.com/reflection-auto-enabled='true' --namespace=kube-system --overwrite

echo "Docker registry secret '$SECRET_NAME' created successfully!"
echo "To use this secret in a pod, add the following to your pod spec:"
echo "  imagePullSecrets:"
echo "  - name: $SECRET_NAME"
