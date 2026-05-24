# Storage & Persistence

**Evidence:** `kubernetes/apps/longhorn-system/`, `kubernetes/apps/openebs-system/`, `kubernetes/apps/nfs-system/`, `kubernetes/apps/volsync-system/`, `kubernetes/components/volsync/`, `talos/talconfig.yaml`

## Storage Architecture

```
┌───────────────────┬──────────────────┬──────────────────┐
│ Replicated (HA)   │ Local (perf)     │ Shared (network) │
│                   │                  │                  │
│ Longhorn          │ OpenEBS          │ NFS              │
│ 3x replication    │ Node-local       │ ReadWriteMany    │
│ Block storage     │ No HA            │ NAS-backed       │
├───────────────────┼──────────────────┼──────────────────┤
│ Home Assistant    │ Cache/temp data  │ Backups          │
│ PostgreSQL        │ Build caches     │ Shared config    │
│ Media databases   │ Dev/test         │ Archives         │
└───────────────────┴──────────────────┴──────────────────┘
         ▼                  ▼                  ▼
    /var/mnt/longhorn   /var/openebs/local  NAS NFS mount
    1TB XFS per node    Host path           (remote)
```

## Longhorn — Distributed Block Storage

**Namespace:** longhorn-system | **Chart:** longhorn/longhorn

Primary storage for all HA workloads. Provides 3x replication across the three cluster nodes.

### Physical Layout (per node)

| Disk | Path | Size | Format | Purpose |
|------|------|------|--------|---------|
| `/dev/nvme0n1` | — | — | — | OS (Talos root) |
| `/dev/sda` | `/var/mnt/longhorn` | 1TB | XFS | Longhorn storage |

### StorageClass

```yaml
apiVersion: storage.k8s.io/v1
kind: StorageClass
metadata:
  name: longhorn
provisioner: driver.longhorn.io
parameters:
  numberOfReplicas: "3"
  staleReplicaTimeout: "2880"   # 48h before replica cleanup
allowVolumeExpansion: true
reclaimPolicy: Delete
```

### Node Configuration (from talconfig.yaml)

```yaml
userVolumes:
  - name: longhorn
    filesystem:
      type: xfs
    provisioning:
      diskSelector:
        match: disk.dev_path == "/dev/sda"
      minSize: 1TB
      maxSize: 1TB
nodeLabels:
  node.longhorn.io/create-default-disk: 'config'
nodeAnnotations:
  node.longhorn.io/default-disks-config: |
    {"disks":[{"path":"/var/mnt/longhorn","allowScheduling":true,"tags":["ssd"]}]}
```

### PVC Example

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: home-assistant-data
  namespace: sgc
spec:
  accessModes: [ReadWriteOnce]
  storageClassName: longhorn
  resources:
    requests:
      storage: 50Gi
```

### Volume Expansion

```bash
kubectl -n sgc patch pvc home-assistant-data \
  -p '{"spec":{"resources":{"requests":{"storage":"100Gi"}}}}'
# Expands online — no pod restart needed
```

### Storage Allocation (approximate)

| Workload | Size | Replication | Node total |
|----------|------|-------------|------------|
| Home Assistant | 50Gi | 3x | 150Gi |
| PostgreSQL | 50Gi | 3x | 150Gi |
| Media metadata | 20Gi | 3x | 60Gi |
| Cache / temp | 30Gi | 1x | 30Gi |
| **Total per node** | | | ~390Gi of 1TB |

## OpenEBS — Local Storage

**Namespace:** openebs-system | **Chart:** openebs/openebs

High-performance node-local storage. No replication — pod is tied to the node.

- **StorageClass:** `openebs-local`
- **Binding:** `WaitForFirstConsumer` (binds to scheduling node)
- **Expansion:** Not supported
- **Use cases:** Caches, temp storage, etcd scratch

## NFS — Shared Network Storage

**Namespace:** nfs-system

NFS shares provisioned from a NAS. Supports `ReadWriteMany` for shared access across pods/nodes.

- **StorageClass:** `nfs`
- **Use cases:** Volsync backup destinations, shared archives
- **Reclain Policy:** Retain (data preserved when PVC deleted)

## Volsync — Backup & Replication

**Namespace:** volsync-system | **Chart:** backube/volsync

Automates PVC backups via ReplicationSource CRDs. Component template at `kubernetes/components/volsync/`.

### Backup Pattern

```yaml
apiVersion: volsync.backube/v1alpha1
kind: ReplicationSource
metadata:
  name: home-assistant-backup
  namespace: sgc
spec:
  sourcePVC: home-assistant-data
  trigger:
    schedule: "0 2 * * *"    # Daily at 2am
  rsync:
    address: nfs-backup.cluster.local
    sshKeys: backup-ssh-key
    path: /backups/home-assistant
```

### Restore Pattern

```bash
# Trigger a manual restore
kubectl -n sgc patch replicationdestination home-assistant-restore \
  --type merge -p '{"spec":{"trigger":{"manual":"restore-now"}}}'

# Watch completion
kubectl -n sgc get replicationdestination -w
```

### Backup Strategy

- **Schedule:** Daily at 2am
- **Retention:** 30-day snapshots, 90-day backups on NFS
- **Critical services:** Home Assistant state, PostgreSQL databases

## Health Checks

```bash
# Longhorn node status
kubectl -n longhorn-system get nodes -o wide

# Volume health
kubectl -n longhorn-system get volume -o wide

# Replica status
kubectl -n longhorn-system get replica

# Prometheus queries
# Volume usage:        longhorn_volume_usage_bytes
# Disk free space:     longhorn_disk_available_bytes / longhorn_disk_capacity_bytes
# Replica status:      longhorn_replica_status{status!="replica"}
```

## Troubleshooting

```bash
# PVC stuck Pending
kubectl -n sgc describe pvc <name>
kubectl -n longhorn-system logs -l app=longhorn-provisioner

# Volume degraded (replica count < 3)
kubectl -n longhorn-system describe volume <name>
kubectl -n longhorn-system get replica

# Longhorn UI (port-forward)
kubectl -n longhorn-system port-forward svc/longhorn-frontend 8080:80
# Open http://localhost:8080

# NFS mount issues
kubectl -n nfs-system logs -l app=nfs-provisioner

# Node disk failure (Longhorn auto-heals)
# 1. Longhorn detects unreachable disk
# 2. Marks replicas unhealthy
# 3. Creates new replicas on healthy nodes
# 4. Rebalances automatically — no manual action
```
