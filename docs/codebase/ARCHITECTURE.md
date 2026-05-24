# Architecture

## Core Sections (Required)

### 1) Architectural Style

- **Primary style:** GitOps / Declarative Infrastructure-as-Code
- **Why:** Every cluster state change is committed to Git and reconciled by Flux. No imperative `kubectl apply` in production — all changes flow through PRs and Flux reconciliation.
- **Primary constraints:**
  1. All secrets must be SOPS-encrypted (AGE) before committing — plaintext secrets are forbidden.
  2. Bootstrap sequence is linear (cilium → coredns → spegel → cert-manager → flux-operator → flux-instance); breaking the order leaves the cluster in a partial state.
  3. Talos is immutable — node OS changes require `talhelper generate` + `talosctl apply` or an upgrade, not SSH and shell edits.

### 2) System Flow

```
Git Push (PR merged to main)
  → GitHub Webhook → Flux source-controller detects change
  → kustomize-controller reconciles Kustomization trees (cluster-meta → cluster-apps)
  → helm-controller applies HelmReleases with resolved chart versions
  → SOPS decryption via AGE key (sops-age Secret in flux-system)
  → Kubernetes resources created/updated on cluster
  → Alertmanager routes alerts → Flux notification-controller sends status back to GitHub/Alertmanager
```

Detailed:

1. **Source:** `kubernetes/flux/cluster/ks.yaml` — two root Kustomizations (`cluster-meta`, `cluster-apps`) are the reconciliation entry points.
2. **Meta layer:** `kubernetes/flux/meta/` — defines Helm repos, OCI repos, and shared secrets referenced by all apps.
3. **Apps layer:** `kubernetes/apps/` — each namespace folder contains per-app `Kustomization` + `HelmRelease` resources; dependencies declared with `dependsOn`.
4. **Components:** `kubernetes/components/` — reusable Kustomize components (ingress template, tailscale ingress, volsync backup, postgres, alerts) mixed into app kustomizations.
5. **Secret resolution:** ExternalSecret resources pull secrets from 1Password Connect; SOPS-encrypted `.sops.yaml` files are decrypted in-cluster by Flux using the AGE key stored in `sops-age` Secret.
6. **Feedback loop:** Flux `Alert` + `Provider` resources send reconciliation status to GitHub commit status and Alertmanager (`http://alertmanager-operated.observability.svc.cluster.local:9093`).

### 3) High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                   GitHub Repository                         │
│         (Git as Single Source of Truth)                     │
└──────────────────────┬──────────────────────────────────────┘
                       │ webhook / polling
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              Flux CD (flux-system)                          │
│  ├─ GitRepository source                                    │
│  ├─ Kustomization: cluster-meta  (repos, secrets)          │
│  ├─ Kustomization: cluster-apps  (all namespaces)          │
│  └─ SOPS decryption provider (AGE key)                     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│          Talos Kubernetes Cluster (sgc)                     │
│  Nodes: milky-way (10.10.209.10)                           │
│          pegasus   (10.10.209.11)                           │
│          othalla   (10.10.209.12)                           │
│  API VIP: 10.10.209.201:6443                               │
│                                                             │
│  ┌──────────┐ ┌────────────┐ ┌─────────────┐              │
│  │  Cilium  │ │cert-manager│ │ingress-nginx│              │
│  │ (CNI+LB) │ │ (TLS/ACME) │ │(int/ext/ts) │              │
│  └──────────┘ └────────────┘ └─────────────┘              │
│       │                             │                       │
│  ┌────┴──────────────────────────── ┤                       │
│  │  internal: k8s_gateway → home network                   │
│  │  external: Cloudflare Tunnel → internet                 │
│  │  tailscale: Tailscale VPN → tailnet devices             │
│  └──────────────────────────────────┘                       │
└─────────────────────────────────────────────────────────────┘
```

### 4) Infrastructure Nodes

| Hostname | IP | Role | Storage | Extensions |
|----------|----|------|---------|------------|
| milky-way | 10.10.209.10 | control-plane + worker | 1TB XFS Longhorn | i915, intel-ucode, iscsi-tools |
| pegasus | 10.10.209.11 | control-plane + worker | 1TB XFS Longhorn | i915, intel-ucode, iscsi-tools |
| othalla | 10.10.209.12 | control-plane + worker | 1TB XFS Longhorn | i915, intel-ucode, iscsi-tools |

- **Pod CIDR:** 10.209.0.0/16 (Cilium)
- **Service CIDR:** 10.199.0.0/16
- **Home network:** 10.10.0.0/16, gateway 10.10.0.1

### 5) Layer/Module Responsibilities

| Layer or module | Owns | Must not own | Evidence |
|-----------------|------|--------------|----------|
| `kubernetes/flux/` | GitRepository, OCIRepository, root Kustomizations | Application configs | `kubernetes/flux/cluster/ks.yaml` |
| `kubernetes/flux/meta/repos/` | HelmRepository, OCIRepository source definitions | Workloads | `kubernetes/flux/meta/repos/` |
| `kubernetes/apps/<ns>/` | Per-app HelmRelease, Kustomization, namespace-scoped secrets | Shared infra components | `kubernetes/apps/sgc/` |
| `kubernetes/components/` | Reusable Kustomize Components (ingress, tailscale, volsync, alerts, postgres) | App-specific values | `kubernetes/components/tailscale/ingress.yaml` |
| `talos/` | Node OS config, extensions, disk layout, bootstrap secrets | Kubernetes app manifests | `talos/talconfig.yaml` |
| `bootstrap/` | One-time helmfile bootstrap | Ongoing app management | `bootstrap/helmfile.yaml` |
| `observability` ns | Prometheus, Alertmanager, Grafana, Loki | Business logic | `kubernetes/apps/observability/` |
| `network` ns | nginx-ingress, external-dns, Cloudflare Tunnel, k8s_gateway | Application workloads | `kubernetes/apps/network/` |
| `tailscale-system` ns | Tailscale operator, tsidp IDP, authkey | Application workloads | `kubernetes/apps/tailscale-system/` |

### 6) Reused Patterns

| Pattern | Where found | Why it exists |
|---------|-------------|---------------|
| Kustomize Component | `kubernetes/components/ingress/`, `tailscale/`, `volsync/`, `postgres/`, `alerts/` | DRY ingress/backup/alert config across many apps |
| SOPS-encrypted secrets | `kubernetes/components/common/*.sops.yaml`, `*.sops.yaml` per app | Git-safe secret storage |
| ExternalSecret + 1Password Connect | `kubernetes/components/secret-store/`, per-app `externalsecret.yaml` | Dynamic secret sync from 1Password vault |
| HelmRelease `&app` YAML anchor | Every HelmRelease file | Self-referencing name for DRY values |
| `# renovate: datasource=...` comment | `talos/talenv.yaml`, HelmRelease chart versions | Enables Renovate to detect and update versions |
| `dependsOn` in Kustomization | `kubernetes/flux/cluster/ks.yaml`, per-app `ks.yaml` | Ordered reconciliation |
| Flux `Alert` + `Provider` | `kubernetes/components/alerts/` | Notify GitHub + Alertmanager of GitOps events |
| Reloader annotation | `reloader.stakater.com/auto: "true"` on Deployments | Auto-restart pods when ConfigMap/Secret changes |
| `bjw-s/app-template` HelmRelease | Most custom app deployments | Standardized deployment chart |
| Tailscale `tailnet-inbound` ExternalName | `kubernetes/apps/observability/prometheus-proxy/alertmanager-operated.yaml` | Expose external tailnet services into cluster |

### 7) Security Layers

1. **OS Level:** Talos immutable, read-only root filesystem, no SSH
2. **Network:** Cilium eBPF network policies, zero-trust Cloudflare Tunnel
3. **VPN:** Tailscale for internal access; `tsidp` provides OIDC identity
4. **Secrets:** SOPS+AGE encryption at rest; 1Password Connect for runtime secrets
5. **TLS:** cert-manager with Let's Encrypt automatic renewal
6. **RBAC:** Per-namespace service accounts with least-privilege access
7. **Ingress:** Three classes: `internal` (home only), `external` (Cloudflare), `tailscale` (VPN only)

### 8) Known Architectural Risks

- **Single-cluster control plane:** All 3 nodes are control plane + worker. Losing 2 nodes = etcd quorum loss.
- **1Password Connect dependency:** ExternalSecret resources depend on Connect at `https://op-connect.equestria.driscoll.tech/` (equestria cluster). Equestria down = secret refresh fails.
- **AGE key storage:** `age.key` on disk (gitignored). Loss of this file without backup = SOPS secrets unrecoverable.
- **Tailscale as sole VPN layer:** Alertmanager, glance-k8s, tsidp are only reachable via Tailscale. Tailscale outage = loss of internal access.
- **Immutable OS update friction:** Talos upgrades need coordinated `talosctl upgrade` + Kubernetes API availability.

### 9) Evidence

- `kubernetes/flux/cluster/ks.yaml` — root reconciliation entry
- `bootstrap/helmfile.yaml` — bootstrap stack ordering
- `kubernetes/components/` — component library
- `kubernetes/components/alerts/alertmanager/provider.yaml` — Flux → Alertmanager integration
- `kubernetes/components/secret-store/secret-store.yaml` — 1Password Connect ClusterSecretStore
- `talos/talconfig.yaml` — node OS layer
