# Flux GitOps

**Evidence:** `kubernetes/flux/`, `kubernetes/apps/`, `kubernetes/components/`, `.github/workflows/flux-local.yaml`

## Overview

Flux CD is the GitOps engine. All cluster state is declared in Git; Flux continuously reconciles the live cluster to match. The entry point is `kubernetes/flux/cluster/ks.yaml`.

## Reconciliation Flow

```
GitHub Repository
  kubernetes/flux/cluster/ks.yaml   ← entry point
        │
        ├─ cluster-meta Kustomization
        │    └─ kubernetes/flux/meta/   (repos, shared secrets)
        │
        └─ cluster-apps Kustomization  (depends on cluster-meta)
             └─ kubernetes/apps/       (all namespaces)
                  └─ per-namespace kustomization.yaml
                       └─ per-app ks.yaml → HelmRelease
```

## Root Kustomizations

**File:** `kubernetes/flux/cluster/ks.yaml`

```yaml
# cluster-meta: system setup (repos, secrets)
kind: Kustomization
metadata:
  name: cluster-meta
spec:
  interval: 1h
  path: ./kubernetes/flux/meta
  decryption:
    provider: sops
    secretRef:
      name: sops-age
  sourceRef:
    kind: GitRepository
    name: flux-system
---
# cluster-apps: all applications (depends on cluster-meta)
kind: Kustomization
metadata:
  name: cluster-apps
spec:
  interval: 1h
  path: ./kubernetes/apps
  dependsOn:
    - name: cluster-meta
  decryption:
    provider: sops
    secretRef:
      name: sops-age
```

## Dependency Order

```
cluster-meta (system setup — repos, shared secrets)
    ↓
cluster-apps
    ├─ cert-manager
    │   └─ certificates, ClusterIssuers
    ├─ network (depends on cert-manager)
    │   ├─ traefik CRDs → traefik
    │   ├─ external-dns, k8s-gateway, cloudflare-tunnel
    │   └─ ingress-nginx
    ├─ tailscale-system
    │   ├─ authkey (secret)
    │   └─ tsidp (OIDC IDP)
    ├─ observability
    │   └─ prometheus-proxy (alertmanager)
    └─ sgc
        ├─ mosquitto (MQTT)
        └─ home-assistant (depends on mosquitto)
```

## Application Structure Pattern

Each application lives at `kubernetes/apps/<namespace>/<app>/`:

```
kubernetes/apps/<namespace>/<app>/
├── ks.yaml              # Flux Kustomization (sync config + postBuild vars)
├── kustomization.yaml   # Kustomize config (components list + resources)
├── helmrelease.yaml     # HelmRelease (chart + values)
├── externalsecret.yaml  # 1Password secret pull (optional)
└── values.yaml          # Additional Helm values (optional)
```

### Example: home-assistant ks.yaml

```yaml
apiVersion: kustomize.toolkit.fluxcd.io/v1
kind: Kustomization
metadata:
  name: home-assistant
  namespace: sgc
spec:
  components:
    - ../../../../components/alerts
    - ../../../../components/common
  resources:
    - helmrelease.yaml
    - configmap.yaml
  postBuild:
    substitute:
      APP: home-assistant
      NAMESPACE: sgc
```

## Kustomize Components Library

Shared logic lives in `kubernetes/components/` and is applied via `components:` lists:

| Component | Path | What it injects |
|-----------|------|-----------------|
| `common` | `components/common/` | Namespace, ServiceAccount, NetworkPolicy, RBAC |
| `alerts` | `components/alerts/` | Prometheus ServiceMonitor, PrometheusRule, Flux Alert Provider |
| `ingress` | `components/ingress/` | Standard nginx Ingress template |
| `tailscale` | `components/tailscale/` | Tailscale `ingressClassName: tailscale` Ingress |
| `volsync` | `components/volsync/` | Volsync ReplicationSource (PVC backup) |
| `postgres` | `components/postgres/` | CloudNative-PG cluster and database |
| `secret-store` | `components/secret-store/` | ClusterSecretStore pointing to 1Password Connect |
| `alerts/alertmanager` | `components/alerts/alertmanager/` | Flux → Alertmanager notification provider |
| `alerts/github-status` | `components/alerts/github-status/` | Flux → GitHub commit status provider |

## SOPS Secret Encryption

All secrets committed to git are SOPS-encrypted with AGE:

- **Config file:** `.sops.yaml` (repo root) — defines which paths to encrypt and with which AGE recipient
- **Key file:** `age.key` (local, gitignored) — private decryption key
- **In-cluster decryption:** Flux uses the `sops-age` Secret in `flux-system` namespace

```bash
# Encrypt a new secret
sops --encrypt -i kubernetes/apps/myapp/secret.sops.yaml

# Decrypt for inspection (local only)
sops -d kubernetes/components/common/cluster-secrets.sops.yaml

# Check Flux can decrypt
kubectl -n flux-system get secret sops-age -o yaml
```

Encrypted files: `kubernetes/components/common/cluster-secrets.sops.yaml`, `shared-secrets.sops.yaml`, `sops-age.sops.yaml`, `talos/talsecret.sops.yaml`, per-app `*.sops.yaml`.

## PostBuild Variable Substitution

Flux substitutes `${VAR}` placeholders in all manifests within a Kustomization:

```yaml
postBuild:
  substitute:
    APP: home-assistant
    NAMESPACE: sgc
    CLUSTER_NAME: sgc
  substituteFrom:
    - kind: Secret
      name: cluster-secrets    # from cluster-secrets.sops.yaml
    - kind: ConfigMap
      name: cluster-config
```

## GitHub Webhook (Push Reconciliation)

By default Flux polls every hour. For immediate sync on push:

```bash
# Get the webhook path
kubectl -n flux-system get receiver github-webhook \
  --output=jsonpath='{.status.webhookPath}'
# → /hook/12ebd1e363c641...

# Full webhook URL
https://flux-webhook.sgc.driscoll.tech/hook/<uuid>
```

Register this URL in GitHub → Settings → Webhooks with Content-Type `application/json` and push events only.

## Update Automation

- **Renovate:** `.renovaterc.json5` — opens PRs for Helm chart, container image, and GitHub Action version bumps. Weekend schedule by default.
- **Custom `Update.cs` scripts:** `do-update.cs` (`.mise/tasks/`) discovers and runs all `Update.cs` files in the repo. Used for tasks Renovate can't handle (e.g., Tailscale IP syncs).

## CI Validation

**File:** `.github/workflows/flux-local.yaml`

On every PR touching `kubernetes/**`:

1. `flux-local test` — renders all Kustomizations and HelmReleases, fails if any chart errors
2. `flux-local diff helmrelease` — posts HelmRelease value diffs as PR comments
3. `flux-local diff kustomization` — posts Kustomization diffs as PR comments

```bash
# Run locally before pushing
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/ --verbosity 1

# Build kustomization manually
kustomize build kubernetes/apps/network

# Force reconcile
task reconcile
# or:
flux --namespace flux-system reconcile kustomization flux-system --with-source
```

## Common Operations

```bash
# Check all Flux resources
flux check
flux get sources git -A
flux get ks -A
flux get hr -A

# Debug a failing Kustomization
flux describe ks <name> -n flux-system
flux logs -n flux-system --all-namespaces --follow

# Reconcile a specific app
flux reconcile kustomization home-assistant -n sgc --with-source

# Force HelmRelease upgrade
flux reconcile hr home-assistant -n sgc

# Verify SOPS decryption
sops -d kubernetes/components/common/cluster-secrets.sops.yaml | head -5

# Check PostBuild substitution
flux get ks <name> -o yaml | grep -A 10 substitute
```

## Troubleshooting

```bash
# Reconciliation stuck
flux describe ks <name> -n flux-system
kubectl -n flux-system get events --sort-by='.lastTimestamp'

# SOPS decryption fails
kubectl -n flux-system get secret sops-age -o yaml
# Verify SOPS_AGE_KEY_FILE points to age.key

# PostBuild variable not substituted
flux get ks <name> -o yaml | grep substitute
grep -r '\${APP}' kubernetes/apps/<namespace>/

# HelmRelease stuck upgrading
flux get hr <name> -n <namespace> -o yaml | grep -A 5 conditions
kubectl -n <namespace> get events --sort-by='.lastTimestamp'
```
