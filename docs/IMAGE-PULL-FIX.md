# Image Pull Issue Fix - Recommended Configuration Changes

Based on the analysis of the storage-s3 deployment configuration, here are the recommended fixes:

## Issue Analysis

Current problematic configuration in `helmrelease.yaml`:
```yaml
image:
  repository: ghcr.io/rclone/rclone
  tag: latest                # ❌ Problematic: unstable tag
  pullPolicy: Always         # ❌ Problematic: forces pulls every time
```

## Primary Fix: Pin Image Version and Optimize Pull Policy

The main issue is likely caused by:
1. Using `latest` tag which can be unstable
2. `pullPolicy: Always` which forces image pulls on every pod restart
3. Potential rate limiting from frequent pulls

## Recommended Configuration

Replace the image configuration in `kubernetes/apps/database/storage/s3/helmrelease.yaml`:

```yaml
image:
  repository: ghcr.io/rclone/rclone
  tag: "1.64.2"              # ✅ Fixed: stable version
  pullPolicy: IfNotPresent   # ✅ Fixed: only pull if not present
```

## Why This Fixes the Issue

1. **Stable Tag**: Using a specific version (`1.64.2`) instead of `latest` ensures:
   - Consistent behavior across deployments
   - No surprises from upstream changes
   - Predictable image pulls

2. **Optimized Pull Policy**: `IfNotPresent` instead of `Always`:
   - Reduces unnecessary network calls
   - Avoids rate limiting issues  
   - Faster pod startup times
   - Only pulls if image is not already on the node

3. **Reliability**: This configuration is more production-ready and less prone to external factors

## Alternative Configurations

### Option 1: Use Latest with Better Pull Policy
```yaml
image:
  repository: ghcr.io/rclone/rclone
  tag: latest
  pullPolicy: IfNotPresent   # Still better than Always
```

### Option 2: Use Specific Version with Always Policy (for testing)
```yaml
image:
  repository: ghcr.io/rclone/rclone
  tag: "1.64.2" 
  pullPolicy: Always         # Only if you need latest updates
```

## Implementation Steps

1. Edit the helmrelease.yaml file
2. Apply the changes using Flux
3. Monitor the deployment

## Verification Commands

After making changes:
```bash
# Check deployment status
kubectl -n database get deployment storage-s3

# Watch pod creation
kubectl -n database get pods -l app.kubernetes.io/name=storage-s3 --watch

# Check events for success
kubectl -n database get events | grep storage-s3
```

This fix addresses the most common cause of image pull issues in Kubernetes deployments using public registries.