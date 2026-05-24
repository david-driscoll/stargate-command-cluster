# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Primary language | YAML (Kubernetes/Talos manifests) | `kubernetes/`, `talos/` |
| Secondary languages | C# (.NET 10), Python 3.14 | `.mise.toml`, `scripts/` |
| Runtime environment | Kubernetes v1.36.1 on Talos Linux v1.13.2 | `talos/talenv.yaml` |
| Package manager (tools) | mise (polyglot tool manager) | `.mise.toml` |
| Build/task system | Taskfile v3 | `Taskfile.yaml` |
| GitOps engine | Flux v2.8.8 (flux-operator v0.50.0) | `bootstrap/helmfile.yaml`, `.mise.toml` |

### 2) Production Frameworks and Dependencies

| Dependency | Version | Role in system | Evidence |
|------------|---------|----------------|----------|
| Talos Linux | v1.13.2 | Immutable OS for all cluster nodes | `talos/talenv.yaml` |
| Kubernetes | v1.36.1 | Container orchestration platform | `talos/talenv.yaml` |
| Flux Operator | 0.50.0 | GitOps controller (FluxInstance CRD) | `bootstrap/helmfile.yaml` |
| Cilium | 1.19.4 | CNI + network policy + load balancing | `bootstrap/helmfile.yaml` |
| CoreDNS | 1.45.2 | In-cluster DNS | `bootstrap/helmfile.yaml` |
| Spegel | 0.7.0 | Peer-to-peer container image distribution | `bootstrap/helmfile.yaml` |
| cert-manager | v1.20.2 | TLS certificate lifecycle management | `bootstrap/helmfile.yaml` |
| ingress-nginx | (managed by Flux) | HTTP/HTTPS ingress with multiple classes | `kubernetes/apps/network/` |
| Longhorn | (managed by Flux) | Replicated block storage (3x) | `kubernetes/apps/longhorn-system/` |
| external-dns | (managed by Flux) | Automatic DNS record management | `kubernetes/apps/network/external-dns/` |
| Cloudflared | 2026.5.0 | Cloudflare Tunnel for external access | `.mise.toml` |
| Tailscale Operator | (managed by Flux) | VPN access + IDP | `kubernetes/apps/tailscale-system/` |
| kube-prometheus-stack | 85.2.0 | Prometheus + Alertmanager + Grafana | `kubernetes/apps/observability/` |
| Loki | (managed by Flux) | Log aggregation | `kubernetes/apps/observability/` |
| SOPS + AGE | sops 3.13.1, age 1.3.1 | Secret encryption at rest | `.mise.toml` |
| Volsync | (managed by Flux) | PVC backup and replication | `kubernetes/components/volsync/` |
| CloudNative-PG | (managed by Flux) | PostgreSQL operator | `kubernetes/apps/cloudnative-pg/` |
| Renovate | (GitHub App) | Automated dependency PRs | `.renovaterc.json5` |
| Reloader | (managed by Flux) | Automatic pod restart on ConfigMap/Secret change | `kubernetes/apps/` (annotations) |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| mise | Polyglot tool version manager | `.mise.toml` |
| talhelper | Talos config generation from talconfig.yaml | `.mise.toml`, `talos/talconfig.yaml` |
| talosctl | Talos node management CLI | `.mise.toml` |
| kubectl | Kubernetes CLI | `.mise.toml` |
| flux2 (CLI) | Flux GitOps CLI | `.mise.toml` |
| helm | Helm chart templating | `.mise.toml` |
| helmfile | Declarative helm release manager (bootstrap) | `.mise.toml`, `bootstrap/helmfile.yaml` |
| kustomize | Kustomization overlays | `.mise.toml` |
| sops | Secret encryption/decryption | `.mise.toml` |
| age | AGE key-based encryption backend for SOPS | `.mise.toml` |
| flux-local | CI validation of Flux kustomizations | `.mise.toml`, `.github/workflows/flux-local.yaml` |
| kubeconform | Kubernetes manifest schema validation | `.mise.toml` |
| yq / jq | YAML/JSON manipulation | `.mise.toml` |
| cloudflared | Cloudflare Tunnel CLI | `.mise.toml` |
| 1password-cli (op) | 1Password secret access | `.mise.toml` |
| dotnet (.NET 10) | C# scripts and update tooling | `.mise.toml` |
| apm (microsoft/apm) | [ASK USER] purpose in this repo | `.mise.toml` |
| skillfile | Claude Code skill management | `.mise.toml` |
| gh | GitHub CLI | `.mise.toml` |
| task | Taskfile runner | `.mise.toml` |

### 4) Key Commands

```bash
# Install all tools
mise install

# Bootstrap the cluster (first-time)
task bootstrap:talos
task bootstrap:apps

# Force Flux to sync
task reconcile
# or: flux --namespace flux-system reconcile kustomization flux-system --with-source

# Generate/apply Talos config
task talos:generate-config
task talos:apply-node IP=10.10.209.10 MODE=auto

# Upgrade Talos on a node
task talos:upgrade-node IP=10.10.209.10

# Upgrade Kubernetes
task talos:upgrade-k8s

# Validate Flux kustomizations locally
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/ --verbosity 1

# Run update scripts (Renovate-supplementing C# tooling)
dotnet run .mise/tasks/do-update.cs
```

### 5) Environment and Config

- Config sources: `.mise.toml` (tool versions + env), `Taskfile.yaml` (task definitions), `talos/talconfig.yaml` (node config), `talos/talenv.yaml` (version pins)
- Required env vars:
  - `KUBECONFIG` → `./kubeconfig` (set by `.mise.toml` and `Taskfile.yaml`)
  - `SOPS_AGE_KEY_FILE` → `./age.key` (private AGE key for SOPS decryption)
  - `TALOSCONFIG` → `./talos/clusterconfig/talosconfig`
  - `CONNECT_HOST` → 1Password Connect server URL (`https://op-connect.equestria.driscoll.tech/`)
  - `CONNECT_VAULT` → `Eris`
  - `CONNECT_TOKEN` → 1Password op:// reference
  - `TAILSCALE_CLIENT_ID` / `TAILSCALE_CLIENT_SECRET` → Tailscale OAuth via 1Password
- Deployment constraint: All secrets committed to git must be encrypted with SOPS+AGE before push.

### 6) Evidence

- `.mise.toml` — all tool versions and env vars
- `Taskfile.yaml` — task definitions and env wiring
- `bootstrap/helmfile.yaml` — bootstrap-phase chart versions
- `talos/talenv.yaml` — Talos and Kubernetes version pins
