# Kubernetes Pullthrough Cache Setup and Cleanup Strategy

This document outlines the pullthrough cache solutions implemented for the Stargate Command Cluster to resolve issues with pulling images from remote registries and manage storage/cleanup requirements.

## Overview

The cluster now includes multiple pullthrough cache options to improve image pulling reliability and reduce external dependencies:

1. **Enhanced Spegel** - Distributed P2P registry mirror (already deployed)
2. **Docker Registry v2** - Simple registry with pullthrough proxy
3. **Harbor** - Enterprise-grade registry with advanced features
4. **Automated Cleanup** - Comprehensive cleanup automation

## Solution Options

### 1. Enhanced Spegel Configuration

**Location**: `kubernetes/apps/kube-system/spegel/`

Spegel is already deployed and has been enhanced with:
- Better resource limits (256Mi-512Mi memory, 100m-500m CPU)
- Improved mirror resolution (5 retries, 30ms timeout)
- Tag resolution for better caching
- Production-ready logging (INFO level)

**Access**: Nodes automatically use Spegel via containerd configuration
**Port**: 29999 (hostPort)

### 2. Docker Registry v2 Cache

**Location**: `kubernetes/apps/kube-system/registry-cache/app/docker-registry-cache.yaml`

Simple pullthrough cache with:
- 100Gi persistent storage
- TTL of 168 hours (7 days) for cached images
- Cleanup automation via garbage collection
- Docker Hub proxy configuration

**Access**: 
- Internal: `docker-registry-cache.kube-system.svc.cluster.local:5000`
- External: Node IP:30500

**Configuration Example**:
```yaml
# Add to containerd mirrors
[plugins."io.containerd.grpc.v1.cri".registry.mirrors]
  [plugins."io.containerd.grpc.v1.cri".registry.mirrors."docker.io"]
    endpoint = ["http://docker-registry-cache.kube-system.svc.cluster.local:5000"]
```

### 3. Harbor Registry Cache

**Location**: `kubernetes/apps/kube-system/registry-cache/app/harbor-cache.yaml`

Enterprise-grade solution with:
- Built-in garbage collection (daily at 2 AM)
- 30-day retention policy
- Multi-registry proxy (Docker Hub, GHCR, Quay, GCR)
- Web UI for management
- Advanced cleanup policies

**Access**: 
- Web UI: Node IP:30880
- Registry: Node IP:30500

## Cleanup and Storage Management

### Automated Cleanup Features

**Location**: `kubernetes/apps/kube-system/registry-cache/app/registry-cleanup.yaml`

#### 1. Registry Cleanup CronJob
- **Schedule**: Weekly on Sundays at 2 AM
- **Function**: Cleans up old registry tags (keeps last 10)
- **Target**: Docker Registry v2 cache

#### 2. Node-level Image Cleanup DaemonSet
- **Schedule**: Daily
- **Function**: Cleans containerd images, content, and snapshots
- **Target**: All cluster nodes
- **Privileges**: Runs with host access to containerd

#### 3. Kubernetes Image Monitoring
- **Function**: Identifies unused images across the cluster
- **Output**: Logs unused images for manual cleanup

### Storage Requirements

| Component | Storage Type | Size | Purpose |
|-----------|-------------|------|---------|
| Docker Registry Cache | PVC | 100Gi | Cached container images |
| Harbor PostgreSQL | emptyDir | - | Harbor metadata |
| Harbor Registry | emptyDir | - | Harbor cached images |
| Spegel | Node local | - | Distributed cache |

### Cleanup Policies

#### Docker Registry v2
- **Image TTL**: 7 days
- **Cleanup**: Weekly garbage collection
- **Storage**: Delete untagged manifests
- **Method**: Registry API + garbage collector

#### Harbor
- **Retention**: 30 days since last pull
- **Cleanup**: Daily garbage collection at 2 AM
- **Rules**: Configurable per project/repository
- **Method**: Built-in retention policies

#### Node-level (via DaemonSet)
- **Frequency**: Daily
- **Target**: Unused containerd images
- **Method**: `ctr images prune`, `ctr content prune`
- **Scope**: Per-node cleanup

## Implementation Guide

### Step 1: Enhanced Spegel (Already Active)
The enhanced Spegel configuration is already applied and should improve image pulling reliability through P2P caching.

### Step 2: Deploy Docker Registry Cache (Optional)
```bash
# Apply the registry cache
kubectl apply -f kubernetes/apps/kube-system/registry-cache/app/docker-registry-cache.yaml

# Verify deployment
kubectl get pods -n kube-system -l app=docker-registry-cache
```

### Step 3: Configure Containerd Mirrors (Node-level)
Add to `/etc/containerd/config.toml` or via Talos patches:
```toml
[plugins."io.containerd.grpc.v1.cri".registry.mirrors]
  [plugins."io.containerd.grpc.v1.cri".registry.mirrors."docker.io"]
    endpoint = ["http://docker-registry-cache.kube-system.svc.cluster.local:5000", "https://registry-1.docker.io"]
```

### Step 4: Deploy Harbor (Enterprise Option)
```bash
# Create harbor namespace and deploy
kubectl apply -f kubernetes/apps/kube-system/registry-cache/app/harbor-cache.yaml

# Access Harbor UI
# http://<node-ip>:30880
```

### Step 5: Enable Cleanup Automation
```bash
# Deploy cleanup automation
kubectl apply -f kubernetes/apps/kube-system/registry-cache/app/registry-cleanup.yaml

# Verify cleanup jobs
kubectl get cronjobs -n kube-system
kubectl get daemonsets -n kube-system -l app=node-image-cleanup
```

## Monitoring and Troubleshooting

### Health Checks
```bash
# Check Spegel status
kubectl get pods -n kube-system -l app.kubernetes.io/name=spegel

# Check registry cache
kubectl get pods -n kube-system -l app=docker-registry-cache
curl -I http://<node-ip>:30500/v2/

# Check Harbor
curl -I http://<node-ip>:30880/api/v2.0/ping

# Check cleanup jobs
kubectl get jobs -n kube-system -l app=registry-cleanup
kubectl logs -n kube-system -l app=node-image-cleanup
```

### Storage Monitoring
```bash
# Check registry storage usage
kubectl exec -n kube-system deployment/docker-registry-cache -- df -h /var/lib/registry

# Check node storage
kubectl get nodes -o jsonpath='{range .items[*]}{.metadata.name}{"\t"}{.status.allocatable.storage}{"\n"}{end}'
```

## Configuration Customization

### Adjust Cleanup Frequency
Edit the cleanup CronJob schedule:
```yaml
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM instead of weekly
```

### Modify Retention Policies
For Harbor, update the retention configuration:
```yaml
retention:
  policy:
    rules:
      - template: "always"
        params:
          days_since_pull: 14  # Keep for 14 days instead of 30
```

### Change Storage Sizes
Modify the PVC size for Docker Registry:
```yaml
spec:
  resources:
    requests:
      storage: 200Gi  # Increase from 100Gi
```

## Security Considerations

1. **Network Policies**: Consider implementing network policies to restrict registry access
2. **RBAC**: Cleanup jobs have minimal required permissions
3. **Storage**: Use encrypted storage classes for cached images
4. **Access**: Registry caches are internal-only by default

## Recommendations

### For Small Clusters (< 10 nodes)
- Use enhanced Spegel configuration
- Deploy Docker Registry v2 cache for critical registries
- Enable weekly cleanup automation

### For Large Clusters (> 10 nodes)
- Use enhanced Spegel for P2P distribution
- Deploy Harbor for enterprise features
- Enable daily cleanup automation
- Monitor storage usage closely

### For Production Environments
- Deploy multiple cache solutions for redundancy
- Use persistent storage with backup for caches
- Implement monitoring and alerting
- Regular cleanup policy reviews

## Cost-Benefit Analysis

### Benefits
- **Reliability**: Reduced dependency on external registries
- **Performance**: Faster image pulls from local caches
- **Bandwidth**: Reduced external bandwidth usage
- **Compliance**: Better control over image sources

### Costs
- **Storage**: 100-200Gi per registry cache
- **Compute**: ~1-2 CPU cores and 1-2Gi RAM per cache
- **Management**: Additional monitoring and maintenance

### ROI Timeline
- **Immediate**: Improved image pull reliability
- **Short-term** (1-4 weeks): Reduced external bandwidth costs
- **Long-term** (1+ months): Improved development velocity and reduced downtime