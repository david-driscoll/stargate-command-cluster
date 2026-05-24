# Cluster Operations Guide

**Last Updated:** 2026-05-24

This guide covers common operational tasks for the Stargate Command Cluster.

## Table of Contents

1. [Daily Operations](#daily-operations)
2. [Deployment Workflow](#deployment-workflow)
3. [Cluster Health](#cluster-health)
4. [Troubleshooting](#troubleshooting)
5. [Maintenance](#maintenance)
6. [Upgrade Process](#upgrade-process)

## Daily Operations

### Check Cluster Status

```bash
# Flux status and readiness
flux check
flux get sources git -A
flux get ks -A
flux get hr -A

# Kubernetes nodes
kubectl get nodes -o wide

# Pod status across cluster
kubectl get pods --all-namespaces --watch

# Cilium network status
cilium status
```

### Monitor Key Services

```bash
# Verify ingress controllers
kubectl -n network get svc -l app.kubernetes.io/name=ingress-nginx

# Check certificate status
kubectl -n cert-manager get cert -A

# Verify Flux is synced
kubectl -n flux-system get ks -o wide

# Check storage health
kubectl -n longhorn-system get nodes -o wide
```

### View Logs

```bash
# Flux controller logs
kubectl -n flux-system logs -l app=source-controller -f
kubectl -n flux-system logs -l app=kustomize-controller -f

# Ingress controller logs
kubectl -n network logs -l app.kubernetes.io/name=ingress-nginx -f --tail=50

# Application logs
kubectl -n sgc logs -l app=home-assistant -f

# Node system logs
talosctl -n 10.10.209.10 logs controller-runtime
```

## Deployment Workflow

### 1. Make Changes to Cluster Configuration

```bash
# Edit manifests
vim kubernetes/apps/sgc/home/home-assistant/helmrelease.yaml

# Or update Talos config
vim talos/talconfig.yaml
```

### 2. Local Validation

```bash
# Validate Kustomization builds
kustomize build kubernetes/apps/network > /dev/null && echo "Valid"

# Use flux-local for comprehensive check
flux-local get all --path kubernetes/apps/sgc

# For Talos changes
cd talos
talhelper genconfig --print 2>&1 | head -20
```

### 3. Commit and Push

```bash
# Stage changes
git add kubernetes/
git commit -m "feat(home-assistant): update to v2024.1"
git push origin main
```

### 4. Monitor Deployment

```bash
# Watch Flux reconcile
kubectl -n flux-system logs -l app=kustomize-controller -f

# Monitor Kustomization status
flux get ks home-apps --watch

# Check HelmRelease progress
flux get hr -n sgc home-assistant --watch

# Verify pod is running
kubectl -n sgc get pods -l app=home-assistant --watch
```

### 5. Verify Success

```bash
# Check pod is ready
kubectl -n sgc get pods -l app=home-assistant

# Verify service is accessible
curl -k https://home-assistant.sgc.driscoll.tech/api/

# Check application logs
kubectl -n sgc logs -l app=home-assistant --tail=50
```

## Cluster Health

### Active Alerts

The fastest way to see what's wrong cluster-wide is the Alertmanager API. Accessible via Tailscale (connect to tailnet first):

```bash
# View all active alerts
curl https://alertmanager.driscoll.tech/api/v2/alerts | jq '.[].labels'

# View alerts with full details (receivers, annotations, etc.)
curl https://alertmanager.driscoll.tech/api/v2/alerts | jq '.[] | {labels: .labels, annotations: .annotations, status: .status}'

# Filter to critical only
curl 'https://alertmanager.driscoll.tech/api/v2/alerts?filter=severity%3D%22critical%22' | jq '.[].labels'

# View alert groups
curl https://alertmanager.driscoll.tech/api/v2/alerts/groups | jq .
```

> **Note:** alertmanager is only reachable on the Tailscale tailnet. Ensure your device is connected to Tailscale before querying.

### Health Checks

**Run the verification suite:**

```bash
# Flux readiness
flux check && echo "Flux: OK" || echo "Flux: NOT OK"

# Kubernetes API
kubectl get cs && echo "API: OK" || echo "API: NOT OK"

# Node readiness
kubectl get nodes --no-headers | awk '{print $2}' | grep -q "Ready" && \
  echo "Nodes: OK" || echo "Nodes: NOT OK"

# Storage
kubectl -n longhorn-system get nodes --no-headers | grep -q "Ready" && \
  echo "Storage: OK" || echo "Storage: NOT OK"

# Ingress
kubectl -n network get svc -l app.kubernetes.io/name=ingress-nginx \
  --no-headers | grep -q "LoadBalancer" && \
  echo "Ingress: OK" || echo "Ingress: NOT OK"
```

### Pod Health

```bash
# Find pods with issues
kubectl get pods --all-namespaces --field-selector=status.phase!=Running

# Detailed status of problematic pod
kubectl describe pod <pod-name> -n <namespace>

# View events for namespace
kubectl get events -n <namespace> --sort-by='.lastTimestamp'
```

### Tailscale Access

Many cluster services are only accessible via Tailscale VPN. Ensure you are connected to the tailnet before accessing these services.

```bash
# Check Tailscale status (on your local machine)
tailscale status

# Verify connectivity to a cluster service
tailscale ping alertmanager

# Check tailscale operator status in cluster
kubectl -n tailscale-system get pods
kubectl -n tailscale-system get svc

# View proxy group status
kubectl -n tailscale-system get proxygroups

# Check tsidp (Tailscale IDP) logs
kubectl -n tailscale-system logs -l app.kubernetes.io/name=tsidp -f
```

**Key Tailscale-only services:**
| Service | Tailscale Hostname | Port |
|---------|--------------------|------|
| Alertmanager | `alertmanager.driscoll.tech` | 443 |
| Tailscale IDP | `idp` (tailnet only) | 443 |
| glance-k8s | `glance-k8s.sgc.internal` | 443 |

### Network Connectivity

```bash
# DNS resolution
nslookup home-assistant.sgc.driscoll.tech

# Internal service discovery
kubectl exec -it <pod> -c <container> -- nslookup home-assistant.sgc.svc.cluster.local

# Ingress routing
curl -k https://home-assistant.sgc.driscoll.tech/ | head -20

# Check network policies
kubectl get networkpolicies -A
```

### Storage Health

```bash
# Longhorn volume status
kubectl -n longhorn-system get volume

# Check available storage per node
kubectl -n longhorn-system get nodes -o custom-columns=\
NAME:.metadata.name,\
AVAILABLE:.status.storageAvailable,\
CAPACITY:.status.storageCapacity

# Replica status
kubectl -n longhorn-system get replica -o wide
```

## Troubleshooting

### Flux Not Syncing

**Symptoms:** Kustomizations stuck in reconciling

**Diagnosis:**
```bash
# Check Flux controller status
flux check

# View reconciliation logs
kubectl -n flux-system logs -l app=kustomize-controller | grep -i error

# Check specific Kustomization
flux describe ks cluster-apps
```

**Solutions:**
```bash
# Force reconciliation
task reconcile

# Or manually
flux reconcile kustomization cluster-apps --with-source

# Check Git credentials
kubectl -n flux-system get secret flux-system -o yaml

# Verify cluster can reach GitHub
kubectl exec -it <flux-pod> -- curl -k https://github.com
```

### Pod CrashLoopBackOff

```bash
# Get pod details
kubectl describe pod <pod-name> -n <namespace>

# View logs
kubectl logs <pod-name> -n <namespace> --previous

# Check resource constraints
kubectl top pod <pod-name> -n <namespace>

# Check node resources
kubectl top nodes

# Increase resources if needed
kubectl -n <namespace> edit deployment <app>
```

### PVC Not Mounting

```bash
# Check PVC status
kubectl describe pvc <pvc-name> -n <namespace>

# Check PV status
kubectl get pv

# Check Longhorn provisioner logs
kubectl -n longhorn-system logs -l app=longhorn-provisioner

# Manual provisioning troubleshooting
kubectl -n longhorn-system get volume <pvc-name>
```

### External DNS Not Updating

```bash
# Check external-dns logs
kubectl -n network logs -l app=external-dns

# Verify ingress annotation
kubectl -n <namespace> get ingress -o yaml | grep external-dns

# Check Cloudflare API token
kubectl -n network get secret cloudflare-api-token -o yaml | base64 -d

# Verify Cloudflare has record
curl -X GET "https://api.cloudflare.com/client/v4/zones/{zone_id}/dns_records" \
  -H "Authorization: Bearer {token}"
```

### Certificate Not Issuing

```bash
# Check certificate resource
kubectl describe cert <cert-name> -n <namespace>

# Check cert-manager logs
kubectl -n cert-manager logs -l app=cert-manager

# Verify ClusterIssuer
kubectl get clusterissuer -o yaml

# Check ACME challenge
kubectl get challenge -A

# Manual renewal
kubectl -n <namespace> delete cert <cert-name>
# cert-manager will recreate and trigger new challenge
```

### Nodes Not Ready

```bash
# Check node status
kubectl describe node <node-name>

# SSH via talosctl
talosctl -n <node-ip> dashboard

# Check system health
talosctl -n <node-ip> health

# View system logs
talosctl -n <node-ip> logs controller-runtime
talosctl -n <node-ip> logs kubelet

# Reboot node gracefully
talosctl -n <node-ip> reboot
```

### Network Connectivity Issues

```bash
# Test DNS
kubectl run debug --image=busybox -- nslookup home-assistant.sgc.svc.cluster.local

# Test pod-to-pod connectivity
kubectl run debug --image=busybox -- wget -O- http://home-assistant.sgc:8123

# Check Cilium
cilium status
cilium connectivity test

# Check network policies
kubectl get networkpolicies -A -o wide
```

## Maintenance

### Backup Current State

```bash
# Backup kubeconfig
cp kubeconfig kubeconfig.backup.$(date +%s)

# Backup Talos config
cp talos/talconfig.yaml talos/talconfig.yaml.backup.$(date +%s)

# Export cluster manifest
kubectl get all -A -o yaml > cluster-backup-$(date +%Y%m%d).yaml
```

### Update Dependencies

```bash
# Check for available updates
task update

# Review proposed changes
git diff

# If satisfied, commit and push
git add -A
git commit -m "chore(deps): update dependencies"
git push origin main
```

### Clean Up Old Resources

```bash
# Remove failed pods
kubectl delete pod --field-selector=status.phase=Failed -A

# Remove completed jobs
kubectl delete job --field-selector=status.successful=1 -A

# Remove old Longhorn snapshots
kubectl -n longhorn-system get snapshot
kubectl -n longhorn-system delete snapshot <old-snapshot>
```

### Database Maintenance

```bash
# Connect to PostgreSQL
kubectl -n database run -it debug --image=postgres -- \
  psql -h postgres-cluster.database.svc.cluster.local \
  -U postgres

# Vacuum (optimize)
VACUUM ANALYZE;

# Check size
SELECT pg_size_pretty(pg_database_size('postgres'));
```

## Upgrade Process

### Minor Kubernetes Upgrade (e.g., 1.35.2 → 1.35.3)

```bash
# 1. Update version in talos/talenv.yaml
vim talos/talenv.yaml
# Change: kubernetesVersion: 1.35.3

# 2. Regenerate configs
task talos:generate-config

# 3. Commit changes
git add talos/
git commit -m "chore(k8s): upgrade to 1.35.3"
git push origin main

# 4. Upgrade cluster
task talos:upgrade-k8s

# 5. Monitor upgrade
kubectl get nodes --watch
kubectl get pods -A --watch
```

### Minor Talos Upgrade (e.g., 1.12.4 → 1.12.5)

```bash
# 1. Update version
vim talos/talenv.yaml
# Change: talosVersion: 1.12.5

# 2. Regenerate and apply per node
task talos:generate-config

# For each node:
task talos:upgrade-node IP=10.10.209.10
task talos:upgrade-node IP=10.10.209.11
task talos:upgrade-node IP=10.10.209.12

# 3. Verify
kubectl get nodes
talosctl -n 10.10.209.10 version
```

### Major Upgrade (e.g., Kubernetes 1.34 → 1.35)

**BEFORE UPGRADE:**
1. Backup cluster state
2. Backup all databases
3. Test restore process
4. Plan maintenance window
5. Notify users

**DURING UPGRADE:**
```bash
# Monitor everything closely
watch kubectl get all -A
watch kubectl get nodes
flux logs --namespace flux-system --follow
```

**AFTER UPGRADE:**
1. Verify all pods are running
2. Test critical applications
3. Check data integrity
4. Document changes
5. Communicate completion

### Helm Chart Upgrades

**Automated by Renovate:**
- Creates PRs for new versions
- flux-local validates before merge
- Merge triggers automatic deployment

**Manual upgrade:**
```bash
# Edit helmrelease.yaml
vim kubernetes/apps/sgc/home/home-assistant/helmrelease.yaml
# Change: spec.chart.spec.version: 2024.1.0

# Verify locally
kustomize build kubernetes/apps/sgc | grep -A5 "spec.chart"

# Commit and push
git add -A
git commit -m "chore(deps): upgrade home-assistant to 2024.1.0"
git push origin main

# Watch deployment
flux get hr -n sgc home-assistant --watch
```

## Emergency Procedures

### Rollback Recent Change

```bash
# View recent commits
git log --oneline -10

# Revert problematic commit
git revert <commit-hash>
git push origin main

# Flux will automatically reconcile
flux logs --namespace flux-system --follow
```

### Full Cluster Reset

**DESTRUCTIVE - Use only as last resort**

```bash
# Reset all nodes to maintenance mode
task talos:reset

# Bootstrap from scratch
task bootstrap:talos
task bootstrap:apps
```

### Rescue a Failed Pod

```bash
# Get pod details
kubectl get pod <pod-name> -n <namespace> -o yaml > pod-backup.yaml

# Delete failed pod
kubectl delete pod <pod-name> -n <namespace>

# New pod created by deployment/statefulset

# If all pods failed, scale down and up
kubectl -n <namespace> scale deployment <app> --replicas=0
kubectl -n <namespace> scale deployment <app> --replicas=1
```

### Rescue Stuck Kustomization

```bash
# Force garbage collection
kubectl -n flux-system patch kustomization <ks-name> \
  --type merge -p '{"spec":{"prune":true}}'

# Reconcile with all resources
flux reconcile kustomization <ks-name> --with-source

# If still stuck, check conditions
kubectl describe ks <ks-name> -n flux-system
```

## Standard Operating Procedures (SOPs)

### SOP: Add New Application

1. Create directory: `kubernetes/apps/<namespace>/<app>/`
2. Create `ks.yaml` Kustomization
3. Create app manifests (HelmRelease or K8s resources)
4. Add to parent `kustomization.yaml`
5. Push to Git
6. Verify with `flux-local`
7. Merge and monitor deployment

### SOP: Modify Secret

1. Decrypt secret: `sops -d <file.sops.yaml>`
2. Edit in temporary file
3. Re-encrypt: `sops -e -i <file.sops.yaml>`
4. Verify file is encrypted (binary)
5. Commit and push
6. Flux auto-decrypts and applies

### SOP: Onboard New Team Member

1. Add GitHub access to repository
2. Provide kubeconfig from `./kubeconfig`
3. Provide SOPS age key via secure channel
4. Grant kubectl access via RBAC
5. Provide Talos API access
6. Walkthrough architecture via codemaps

---

**Last Updated:** 2026-05-24  
**Cluster Status:** Production  
**Maintainer:** David Driscoll
