# Codebase Structure

## Core Sections (Required)

### 1) Top-Level Map

| Path | Purpose | Evidence |
|------|---------|----------|
| `kubernetes/` | All Kubernetes manifests managed by Flux | `kubernetes/flux/`, `kubernetes/apps/` |
| `kubernetes/flux/` | Flux GitOps entry points (cluster, meta, repos) | `kubernetes/flux/cluster/ks.yaml` |
| `kubernetes/apps/` | Per-namespace application HelmReleases and Kustomizations | `kubernetes/apps/<namespace>/` |
| `kubernetes/components/` | Reusable Kustomize components (ingress, alerts, volsync, etc.) | `kubernetes/components/` |
| `talos/` | Talos node configuration (talconfig.yaml, patches, clusterconfig) | `talos/talconfig.yaml` |
| `bootstrap/` | One-time cluster bootstrap (helmfile for cilium, flux, cert-manager) | `bootstrap/helmfile.yaml` |
| `.github/workflows/` | CI pipelines (flux-local test, label sync, equestria sync) | `.github/workflows/flux-local.yaml` |
| `docs/` | Documentation hub, CODEMAPS, operations guides | `docs/README.md`, `docs/CODEMAPS/` |
| `docs/codebase/` | Codebase knowledge documents (this set) | `docs/codebase/` |
| `scripts/` | Helper scripts | `scripts/` |
| `.mise.toml` | Tool version management and environment variables | `.mise.toml` |
| `Taskfile.yaml` | Task runner (includes `.taskfiles/`) | `Taskfile.yaml` |
| `kubeconfig` | Cluster kubeconfig (gitignored in production, present locally) | `Taskfile.yaml` (`KUBECONFIG` env) |
| `age.key` / `sgc.age.key` | AGE private keys for SOPS decryption (never commit) | `.mise.toml` |
| `.agents/` / `.claude/` / `.github/skills/` | Claude Code skills (gitops-cluster-debug, k8s, etc.) | `.agents/skills/` |
| `Skillfile` / `Skillfile.lock` | Skillfile manifest for project-specific Claude skills | `Skillfile` |
| `apm.yml` / `apm.lock.yaml` | APM tool manifest | `.mise.toml` |

### 2) Entry Points

- **GitOps entry:** `kubernetes/flux/cluster/ks.yaml` — two root Kustomizations: `cluster-meta` (repos/meta) and `cluster-apps` (all app namespaces)
- **Bootstrap entry:** `bootstrap/helmfile.yaml` — helmfile used once during cluster bootstrap (cilium → coredns → spegel → cert-manager → flux-operator → flux-instance)
- **Task runner entry:** `Taskfile.yaml` — includes `.taskfiles/bootstrap`, `.taskfiles/talos`, `.taskfiles/k8s`, `.taskfiles/flux`
- **Talos config entry:** `talos/talconfig.yaml` — processed by `talhelper` to produce `talos/clusterconfig/`

### 3) Module Boundaries

| Boundary | What belongs here | What must not be here |
|----------|-------------------|------------------------|
| `kubernetes/flux/` | Flux source/repo definitions and root kustomizations | Application configs |
| `kubernetes/apps/<namespace>/` | HelmRelease, Kustomization, ExternalSecret, config per app | Cross-namespace shared components |
| `kubernetes/components/` | Reusable Kustomize components (ingress, tailscale, volsync, alerts, postgres) | App-specific values |
| `talos/` | Node OS config, patches, cluster bootstrap secrets | Kubernetes application manifests |
| `bootstrap/` | One-time helmfile install only | Regular app management |
| `docs/` | Documentation only | Config or manifests |

### 4) Naming and Organization Rules

- **Namespace directories:** `kubernetes/apps/<namespace>/` — each namespace has its own directory
- **App structure:** `kubernetes/apps/<namespace>/<app>/` containing `ks.yaml` (Kustomization), `helmrelease.yaml`, `kustomization.yaml`, and optionally `externalsecret.yaml`, `values.yaml`
- **SOPS secrets:** named `*.sops.yaml` — must always be encrypted before commit
- **Kustomization components:** referenced via `components:` list in `kustomization.yaml`, sourced from `kubernetes/components/<component>/`
- **File naming:** kebab-case for all YAML files (e.g., `helmrelease.yaml`, `external-dns.yaml`)
- **HelmRelease naming:** app name used as both `metadata.name` and `&app` anchor for self-reference
- **Schema annotations:** `# yaml-language-server: $schema=...` at top of all YAML files
- **Renovation tracking:** Version pins use `# renovate: datasource=...` comments for Renovate bot discovery

### 5) Evidence

- `kubernetes/flux/cluster/ks.yaml` — root Flux entry points
- `bootstrap/helmfile.yaml` — bootstrap chart ordering
- `talos/talconfig.yaml` — node definitions
- `kubernetes/apps/sgc/home/home-assistant/` — representative app structure
- `kubernetes/components/tailscale/ingress.yaml` — reusable component example
