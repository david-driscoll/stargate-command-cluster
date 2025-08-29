#!/usr/bin/env bash
set -Eeuo pipefail

# Troubleshooting script for Kubernetes image pull issues
# Usage: ./troubleshoot-image-pull.sh <namespace> <deployment-name>

NAMESPACE="${1:-database}"
DEPLOYMENT="${2:-storage-s3}"

echo "🔍 Troubleshooting image pull issues for deployment: ${DEPLOYMENT} in namespace: ${NAMESPACE}"
echo "=================================================="

# Function to run a command and capture both success/failure
run_check() {
    local description="$1"
    local command="$2"
    echo -n "⏳ ${description}... "
    
    if eval "$command" >/dev/null 2>&1; then
        echo "✅ OK"
        return 0
    else
        echo "❌ FAILED"
        echo "   Command: $command"
        # Run command again to show error
        eval "$command" 2>&1 | sed 's/^/   /'
        return 1
    fi
}

# Function to show information
show_info() {
    local description="$1"
    local command="$2"
    echo "📋 ${description}:"
    eval "$command" 2>&1 | sed 's/^/   /'
    echo
}

echo "1. Checking basic connectivity and cluster status..."
run_check "Cluster connectivity" "kubectl cluster-info --request-timeout=10s"
run_check "Namespace exists" "kubectl get namespace ${NAMESPACE}"

echo -e "\n2. Checking deployment and pod status..."
show_info "Deployment status" "kubectl -n ${NAMESPACE} get deployment ${DEPLOYMENT} -o wide"
show_info "ReplicaSet status" "kubectl -n ${NAMESPACE} get rs -l app.kubernetes.io/name=${DEPLOYMENT}"
show_info "Pod status" "kubectl -n ${NAMESPACE} get pods -l app.kubernetes.io/name=${DEPLOYMENT} -o wide"

echo "3. Checking for image pull errors..."
PODS=$(kubectl -n ${NAMESPACE} get pods -l app.kubernetes.io/name=${DEPLOYMENT} -o name 2>/dev/null || echo "")
if [ -n "$PODS" ]; then
    for pod in $PODS; do
        pod_name=$(echo "$pod" | cut -d'/' -f2)
        echo "📋 Events for pod: ${pod_name}"
        kubectl -n ${NAMESPACE} describe pod "${pod_name}" | grep -A 10 -B 2 -i "events:\|error\|failed\|pull"
        echo
        
        echo "📋 Pod conditions for: ${pod_name}"
        kubectl -n ${NAMESPACE} get pod "${pod_name}" -o jsonpath='{.status.conditions[*].type}{"\n"}{.status.conditions[*].status}{"\n"}{.status.conditions[*].message}' 2>/dev/null | xargs -n 1 echo "   " || true
        echo
    done
else
    echo "⚠️  No pods found for deployment ${DEPLOYMENT}"
fi

echo "4. Checking namespace events..."
show_info "Recent namespace events" "kubectl -n ${NAMESPACE} get events --sort-by='.metadata.creationTimestamp' --field-selector type!=Normal | tail -10"

echo "5. Checking node status and resources..."
show_info "Node status" "kubectl get nodes -o wide"
show_info "Node conditions" "kubectl describe nodes | grep -A 5 'Conditions:'"

echo "6. Checking image and registry information..."
# Extract image info from deployment
IMAGE_INFO=$(kubectl -n ${NAMESPACE} get deployment ${DEPLOYMENT} -o jsonpath='{.spec.template.spec.containers[0].image}' 2>/dev/null || echo "unknown")
echo "📋 Image being pulled: ${IMAGE_INFO}"

if [[ "${IMAGE_INFO}" == *"ghcr.io"* ]]; then
    echo "📋 Registry: GitHub Container Registry (public)"
    echo "   ✅ Should not require image pull secrets"
elif [[ "${IMAGE_INFO}" == *"docker.io"* ]] || [[ "${IMAGE_INFO}" != *"."* ]]; then
    echo "📋 Registry: Docker Hub"
    echo "   ⚠️  May be subject to rate limiting"
else
    echo "📋 Registry: Custom/Private registry"
    echo "   ⚠️  May require image pull secrets"
fi

echo -e "\n7. Checking for image pull secrets..."
show_info "Service account" "kubectl -n ${NAMESPACE} get serviceaccount ${DEPLOYMENT} -o yaml"
show_info "Image pull secrets in namespace" "kubectl -n ${NAMESPACE} get secrets | grep docker"

echo -e "\n8. Network and DNS checks..."
run_check "CoreDNS pods running" "kubectl -n kube-system get pods -l k8s-app=kube-dns"
run_check "DNS resolution from cluster" "kubectl run dns-test --image=busybox --rm -it --restart=Never -- nslookup ghcr.io" || true

echo -e "\n9. Checking resource quotas and limits..."
show_info "Resource quotas" "kubectl -n ${NAMESPACE} get resourcequota" 
show_info "Limit ranges" "kubectl -n ${NAMESPACE} get limitrange"

echo -e "\n🔧 TROUBLESHOOTING RECOMMENDATIONS:"
echo "=================================================="
echo "If you see image pull errors, try these solutions in order:"
echo
echo "1. **ErrImagePull/ImagePullBackOff errors:**"
echo "   - Check if the image exists: docker pull ${IMAGE_INFO}"
echo "   - Verify network connectivity from nodes to registry"
echo "   - Check if using correct image tag (avoid 'latest' in production)"
echo
echo "2. **Rate limiting (especially Docker Hub):**"
echo "   - Configure registry credentials to increase rate limits"
echo "   - Use alternative registries or mirror images"
echo "   - Change pullPolicy from 'Always' to 'IfNotPresent'"
echo
echo "3. **Private registry authentication:**"
echo "   - Create image pull secret: kubectl create secret docker-registry <name> ..."
echo "   - Add imagePullSecrets to ServiceAccount or Pod spec"
echo
echo "4. **Node resource issues:**"
echo "   - Check node disk space: kubectl describe nodes | grep -A 5 'Conditions:'"
echo "   - Verify nodes have sufficient resources"
echo
echo "5. **Network policies:**"
echo "   - Check if network policies are blocking registry access"
echo "   - Verify firewall/proxy settings on nodes"
echo
echo "📝 For more detailed debugging:"
echo "   kubectl -n ${NAMESPACE} describe deployment ${DEPLOYMENT}"
echo "   kubectl -n ${NAMESPACE} logs deployment/${DEPLOYMENT} --previous"
echo "   kubectl -n ${NAMESPACE} get events --field-selector reason=Failed"