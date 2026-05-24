# Talos OS Configuration

**Evidence:** `talos/talconfig.yaml`, `talos/talenv.yaml`, `talos/patches/`, `.mise.toml`

## Overview

Talos is an immutable, minimal Linux OS purpose-built for Kubernetes. No SSH, no package manager, no shell access — all configuration is declarative YAML applied via `talosctl` or `talhelper`.

## Key Files

| File | Purpose |
|------|---------|
| `talos/talenv.yaml` | Version pins (Talos, Kubernetes) — source of truth for upgrades |
| `talos/talconfig.yaml` | Full cluster + per-node config template |
| `talos/talsecret.sops.yaml` | Cluster CA, API certs, bootstrap token (SOPS encrypted) |
| `talos/clusterconfig/` | Generated per-node configs (never edit directly) |
| `talos/patches/` | Additional MachineConfig patches |

## Current Versions

| Component | Version | Managed by |
|-----------|---------|------------|
| Talos OS | v1.13.2 | `talos/talenv.yaml` + Renovate |
| Kubernetes | v1.36.1 | `talos/talenv.yaml` + Renovate |
| prometheus-operator | v0.91.0 | `talos/talenv.yaml` + Renovate |

## Configuration Flow

```
talos/talenv.yaml        (version pins)
       ↓
talos/talconfig.yaml     (node + cluster template)
       ↓
talhelper genconfig      (.mise.toml: mise run task)
       ↓
talos/clusterconfig/     (per-node YAML, never edit)
       ↓
talosctl apply-config    (push to node)
```

## Cluster Settings

```yaml
clusterName: sgc
endpoint: https://10.10.209.201:6443    # API VIP
clusterPodNets: ["10.209.0.0/16"]
clusterSvcNets: ["10.199.0.0/16"]
cniConfig:
  name: none                            # CNI disabled — Cilium installed separately

additionalApiServerCertSans:
  - "127.0.0.1"
  - "10.10.209.201"
  - "apiserver.sgc.driscoll.tech"
  - "k8s-sgc.sgc.svc.cluster.local"
```

## Node Configuration

All three nodes are identical in role — control-plane nodes that also run workloads (no dedicated workers).

| Hostname | IP | Install disk | Storage disk | MAC |
|----------|-----|-------------|-------------|-----|
| milky-way | 10.10.209.10 | `/dev/nvme0n1` | `/dev/sda` → 1TB XFS | e0:51:d8:19:93:18 |
| pegasus | 10.10.209.11 | `/dev/nvme0n1` | `/dev/sda` → 1TB XFS | (per node) |
| othalla | 10.10.209.12 | `/dev/nvme0n1` | `/dev/sda` → 1TB XFS | (per node) |

YAML anchors (`&userVolumes`, `&nodeLabels`, `&schematic`) share config across nodes.

## System Extensions

| Extension | Purpose |
|-----------|---------|
| `siderolabs/i915` | Intel GPU drivers (media transcoding, compute) |
| `siderolabs/intel-ucode` | CPU microcode security updates |
| `siderolabs/iscsi-tools` | iSCSI client (Longhorn storage) |
| `siderolabs/util-linux-tools` | System utilities |

## Kernel Arguments

Performance is prioritized over security (home lab trade-off — documented decision):

| Argument | Effect |
|----------|--------|
| `i915.enable_guc=3` | Enable Intel GPU GuC firmware |
| `intel_iommu=on` / `iommu=pt` | IOMMU passthrough for direct device access |
| `apparmor=0` | Disable AppArmor LSM |
| `init_on_alloc=0` / `init_on_free=0` | Skip memory zeroing on alloc/free |
| `mitigations=off` | Disable CPU vulnerability mitigations (Spectre/Meltdown) |
| `security=none` | Disable Linux Security Module |
| `talos.auditd.disabled=1` | Disable kernel audit daemon |

## Bootstrap Process (First-Time Setup)

```bash
# Stage 1: Generate configs
task talos:generate-config
# Produces: talos/clusterconfig/ per-node files

# Stage 2: Apply configs to nodes (maintenance mode)
talhelper gencommand apply --extra-flags="--insecure" | bash
# Nodes must be in Talos maintenance mode (port 50000 open)

# Stage 3: Bootstrap etcd quorum (run once on one node)
talhelper gencommand bootstrap | bash

# Stage 4: Get kubeconfig
talhelper gencommand kubeconfig --extra-flags="{{.ROOT_DIR}} --force" | bash

# Stage 5: Install bootstrap apps (cilium, coredns, flux)
task bootstrap:apps
```

## Common Operations

### Apply Config Change

```bash
# Edit talos/talconfig.yaml or patches/
task talos:generate-config

# Apply to a node (modes: auto, staged, immediate)
task talos:apply-node IP=10.10.209.10 MODE=auto
# auto: applies without reboot when possible

# Verify
talosctl -n 10.10.209.10 health
```

### Upgrade Talos

```bash
# 1. Update talosVersion in talos/talenv.yaml
# 2. Regenerate configs
task talos:generate-config

# 3. Upgrade each node (rolling — one at a time)
task talos:upgrade-node IP=10.10.209.10
# Performs: etcd backup → drain → upgrade → reboot → rejoin
```

### Upgrade Kubernetes

```bash
# 1. Update kubernetesVersion in talos/talenv.yaml
# 2. Regenerate configs
task talos:generate-config

# 3. Coordinate upgrade
task talos:upgrade-k8s
# Coordinates: API server → controller manager → scheduler → kubelets
```

### Read Node State

```bash
# Check node health
talosctl -n 10.10.209.10 health

# Get active machine config
talosctl -n 10.10.209.10 get machineconfig

# View etcd member list
talosctl -n 10.10.209.10 etcd member list

# Check system logs
talosctl -n 10.10.209.10 logs controlplane
```

## Secrets Management

`talos/talsecret.sops.yaml` (SOPS+AGE encrypted) contains:
- Cluster CA certificate + key
- API server TLS certificate
- Kubelet certificate
- etcd certificate
- Service account signing key
- Bootstrap token

Never commit unencrypted. Generated once during initial setup; rotate only when required by security policy.

## Troubleshooting

```bash
# Node not responsive
nmap -Pn -n -p 50000 10.10.209.0/24 -vv   # check maintenance mode port
nmap -Pn -n -p 6443 10.10.209.0/24 -vv    # check API port

# Configuration error after apply
talosctl -n 10.10.209.10 logs controlplane
talosctl -n 10.10.209.10 get machineconfig

# etcd unhealthy
talosctl -n 10.10.209.10 etcd member list
talosctl -n 10.10.209.10 health

# Reset a node to maintenance mode (destructive — loses all data)
talosctl -n 10.10.209.10 reset --system-labels-only
```
