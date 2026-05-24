# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| YAML files | kebab-case | `helmrelease.yaml`, `external-dns.yaml` | Any app directory |
| SOPS secrets | suffix `.sops.yaml` | `cluster-secrets.sops.yaml` | `kubernetes/components/common/` |
| Kustomization files | always `kustomization.yaml` | `kustomization.yaml` | Every app dir |
| Flux Kustomization CRDs | always `ks.yaml` | `ks.yaml` | Every app dir |
| Helm values overrides | `values.yaml` or `helmrelease.yaml` inline | `kubernetes/apps/network/traefik/values.yaml` | `kubernetes/apps/network/traefik/` |
| App name anchor | `&app` YAML anchor = app name | `name: &app home-assistant` | `kubernetes/apps/sgc/home/home-assistant/helmrelease.yaml` |
| Namespace directories | match Kubernetes namespace name | `kubernetes/apps/tailscale-system/` | `kubernetes/apps/` |
| Renovate comment | `# renovate: datasource=<source> depName=<name>` | `talos/talenv.yaml` | `talos/talenv.yaml` |
| Schema annotation | `# yaml-language-server: $schema=<url>` | First line of every YAML | All YAML files |

### 2) Formatting and Linting

- **Formatter:** EditorConfig (`.editorconfig`) — 2-space indent, LF line endings, UTF-8, insert final newline
- **YAML exception:** `.cue` files use 4-space tab; `.md` files 4-space indent with no trailing whitespace trim
- **Linter:** `kubeconform` for Kubernetes manifest schema validation; `flux-local test` for Flux kustomization validity
- **Most relevant rules:**
  - 2-space indent on all YAML
  - Trailing whitespace trimmed on all files except `.md`
  - Final newline required
  - All committed `.sops.yaml` must be encrypted (enforced by SOPS git hooks or manual discipline)
- **Run commands:**
  ```bash
  # Validate manifests
  kubeconform -strict -ignore-missing-schemas kubernetes/
  # Flux local test
  flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/ --verbosity 1
  ```

### 3) Import and Module Conventions

- **Kustomize imports:** Apps import shared components via `components:` list in `kustomization.yaml`; paths are relative to repo root using `../../components/<name>`
- **HelmRelease chart sources:** Always reference a `HelmRepository` or `OCIRepository` defined in `kubernetes/flux/meta/repos/`
- **SOPS decryption:** Handled cluster-side by Flux using `decryption.provider: sops` + `secretRef: sops-age` on each Kustomization that needs it
- **ExternalSecret store:** Apps reference the `ClusterSecretStore` defined in `kubernetes/components/secret-store/`

### 4) Error and Logging Conventions

- **Flux reconciliation errors:** Surface in `kubectl get ks -A` and `flux get hr -A`; events visible via `kubectl get events -n <namespace>`
- **Application logs:** Collected by Loki in `observability` namespace; queryable via Grafana
- **Alertmanager:** Active alerts queryable at `https://alertmanager.driscoll.tech/api/v2/alerts` (Tailscale required) or internally at `http://alertmanager-operated.observability.svc.cluster.local:9093/api/v2/alerts/`
- **Secret redaction:** SOPS encrypts entire secret values; no plaintext secrets should appear in git history

### 5) Testing Conventions

- **No unit tests** for YAML manifests; validation is done via `flux-local test` + `kubeconform` in CI
- **CI gate:** All PRs touching `kubernetes/**` must pass `flux-local test` (see `.github/workflows/flux-local.yaml`)
- **Diff preview:** `flux-local diff` posts HelmRelease and Kustomization diffs as PR comments
- **Mocking:** Not applicable — this is a GitOps infrastructure repo

### 6) Evidence

- `.editorconfig` — formatting rules
- `.github/workflows/flux-local.yaml` — CI validation gate
- `talos/talenv.yaml` — Renovate comment convention
- `kubernetes/components/common/cluster-secrets.sops.yaml` — SOPS naming convention
- `kubernetes/apps/sgc/home/home-assistant/helmrelease.yaml` — `&app` anchor convention
