# Kubernetes Image Pull Troubleshooting Guide

## Problem: storage-s3 Deployment Cannot Pull Image

This guide provides step-by-step troubleshooting for the `storage-s3` deployment in the `database` namespace that cannot pull its container image, despite the image being pullable locally.

### Quick Diagnosis

Run the troubleshooting script:
```bash
./scripts/troubleshoot-image-pull.sh database storage-s3
```

### Manual Step-by-Step Troubleshooting

#### 1. Check Current Deployment Status

```bash
# Check deployment status
kubectl -n database get deployment storage-s3 -o wide

# Check pods
kubectl -n database get pods -l app.kubernetes.io/name=storage-s3 -o wide

# Check recent events
kubectl -n database get events --sort-by='.metadata.creationTimestamp' | tail -20
```

#### 2. Examine Pod Details

```bash
# Get pod name
POD_NAME=$(kubectl -n database get pods -l app.kubernetes.io/name=storage-s3 -o jsonpath='{.items[0].metadata.name}')

# Describe the pod to see detailed error information
kubectl -n database describe pod $POD_NAME

# Check pod events specifically
kubectl -n database describe pod $POD_NAME | grep -A 20 "Events:"
```

#### 3. Check Image Configuration

Current image configuration from `helmrelease.yaml`:
- **Image**: `ghcr.io/rclone/rclone:latest`
- **Registry**: GitHub Container Registry (public)
- **Pull Policy**: `Always`

#### 4. Common Issues and Solutions

##### Issue 1: ErrImagePull / ImagePullBackOff

**Symptoms**: Pod stuck in `ErrImagePull` or `ImagePullBackOff` state

**Diagnosis**:
```bash
# Check if image exists and is accessible
docker pull ghcr.io/rclone/rclone:latest

# Check pod events for specific error
kubectl -n database describe pod $POD_NAME | grep -i "pull\|error\|failed"
```

**Solutions**:
1. **Verify image exists**: Ensure the image tag is correct and exists
2. **Network connectivity**: Check if cluster nodes can reach `ghcr.io`
3. **DNS resolution**: Verify DNS works from within the cluster:
   ```bash
   kubectl run dns-test --image=busybox --rm -it --restart=Never -- nslookup ghcr.io
   ```

##### Issue 2: Rate Limiting

**Symptoms**: Pull errors with "rate limit" or "too many requests" messages

**Diagnosis**:
```bash
# Check for rate limit errors in events
kubectl -n database get events --field-selector reason=Failed | grep -i "rate\|limit"
```

**Solutions**:
1. **Change pull policy**: Update `pullPolicy` from `Always` to `IfNotPresent`
2. **Use specific tags**: Replace `latest` with a specific version tag
3. **Configure registry credentials**: Add Docker Hub or GitHub registry credentials

##### Issue 3: Node Resource Issues

**Symptoms**: Pods cannot be scheduled or nodes show pressure

**Diagnosis**:
```bash
# Check node conditions
kubectl describe nodes | grep -A 5 "Conditions:"

# Check node resources
kubectl top nodes
kubectl describe nodes | grep -A 5 "Allocated resources:"
```

**Solutions**:
1. **Free up disk space**: Clean up unused images and containers on nodes
2. **Add more nodes**: Scale cluster if consistently resource-constrained
3. **Adjust resource requests**: Review and optimize resource requirements

##### Issue 4: Network Policies

**Symptoms**: Network-related pull failures

**Diagnosis**:
```bash
# Check network policies
kubectl -n database get networkpolicy
kubectl get networkpolicy --all-namespaces
```

**Solutions**:
1. **Review network policies**: Ensure they allow egress to container registries
2. **Test connectivity**: Run network troubleshooting pods to test external connectivity

#### 5. Fix Strategies

##### Strategy 1: Quick Fix - Change Image Configuration

Edit the helmrelease to use a more stable configuration:

```yaml
# In kubernetes/apps/database/storage/s3/helmrelease.yaml
containers:
  storage-s3:
    image:
      repository: ghcr.io/rclone/rclone
      tag: "1.64.2"  # Use specific version instead of latest
      pullPolicy: IfNotPresent  # Change from Always
```

##### Strategy 2: Add Image Pull Secret (if needed)

```bash
# Create image pull secret for GitHub registry
kubectl -n database create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<github-token>

# Add to service account
kubectl -n database patch serviceaccount storage-s3 \
  -p '{"imagePullSecrets": [{"name": "ghcr-secret"}]}'
```

##### Strategy 3: Force Reconciliation

```bash
# Force Flux to reconcile
flux --namespace flux-system reconcile kustomization flux-system --with-source

# Force specific kustomization
flux reconcile kustomization storage-s3 --namespace database
```

#### 6. Monitoring and Prevention

##### Monitor Image Pull Issues

```bash
# Watch pod status
kubectl -n database get pods -l app.kubernetes.io/name=storage-s3 --watch

# Monitor events
kubectl -n database get events --watch | grep -i "pull\|error\|failed"
```

##### Prevent Future Issues

1. **Pin image versions**: Avoid `latest` tags in production
2. **Set appropriate pull policies**: Use `IfNotPresent` for stable deployments  
3. **Monitor registry health**: Set up alerts for registry connectivity
4. **Regular health checks**: Implement automated deployment health monitoring

#### 7. Advanced Debugging

##### Container Runtime Issues

```bash
# Check container runtime on nodes
kubectl get nodes -o wide
ssh <node> "sudo crictl images | grep rclone"
ssh <node> "sudo crictl ps -a | grep storage-s3"
```

##### Registry Mirror/Proxy

If using registry mirrors or proxies:
```bash
# Check containerd/docker configuration on nodes
ssh <node> "sudo cat /etc/containerd/config.toml" | grep -A 10 mirrors
```

### Emergency Recovery

If the deployment is critical and needs immediate restoration:

1. **Use local image**: Push the working local image to a private registry
2. **Temporary workaround**: Use a different working image temporarily
3. **Scale to zero**: Scale deployment to 0 and back to 1 to force recreation

```bash
# Emergency scale down/up
kubectl -n database scale deployment storage-s3 --replicas=0
kubectl -n database scale deployment storage-s3 --replicas=1
```

### Getting Help

If the issue persists:
1. Collect logs: `kubectl -n database logs deployment/storage-s3`
2. Export configuration: `kubectl -n database get deployment storage-s3 -o yaml > debug-deployment.yaml`
3. Check cluster-wide issues: `kubectl get events --all-namespaces | grep -i error`

### Related Files

- Main deployment: `kubernetes/apps/database/storage/s3/helmrelease.yaml`
- Kustomization: `kubernetes/apps/database/storage/s3/kustomization.yaml`
- Troubleshooting script: `scripts/troubleshoot-image-pull.sh`