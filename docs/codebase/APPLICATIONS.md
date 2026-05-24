# Applications

**Evidence:** `kubernetes/apps/`, `kubernetes/apps/observability/crds/application-crd.yaml`

All applications are organized by Kubernetes namespace under `kubernetes/apps/`. Each app follows the standard Flux structure: `ks.yaml` + `kustomization.yaml` + `helmrelease.yaml` ± `externalsecret.yaml`.

## Namespace Map

### flux-system — GitOps Engine

| Component | Purpose |
|-----------|---------|
| flux-operator | Manages FluxInstance lifecycle |
| flux-instance | Deploys all Flux controllers |
| source-controller | Syncs GitRepository / OCI sources |
| kustomize-controller | Reconciles Kustomizations |
| helm-controller | Manages HelmReleases |
| notification-controller | Sends alerts to GitHub / Alertmanager |
| sops-age | Secret containing AGE private key for SOPS decryption |
| github-webhook receiver | Triggers reconciliation on git push |

### kube-system — Kubernetes Core

| Component | Purpose | Chart |
|-----------|---------|-------|
| cilium | eBPF CNI + network policies + LB | cilium/cilium |
| coredns | In-cluster DNS | coredns/coredns |
| spegel | P2P container image cache | spegel-org/spegel |
| headlamp | Kubernetes UI dashboard | headlamp/headlamp |
| etcd (HelmRelease) | etcd defrag + maintenance | custom |
| github-token cronjob | Refreshes GitHub auth token | custom |

### cert-manager — TLS

| Component | Purpose |
|-----------|---------|
| cert-manager | Certificate lifecycle management |
| ClusterIssuer: letsencrypt-prod | Let's Encrypt ACME DNS01 via Cloudflare |
| Wildcard cert | `*.sgc.driscoll.tech` |

### network — Ingress & DNS

| Component | Purpose | Chart |
|-----------|---------|-------|
| ingress-nginx | Primary ingress (internal/external/tailscale classes) | ingress-nginx |
| traefik + traefik-crds | Alternative ingress with Middleware CRDs | traefik/traefik |
| external-dns (Cloudflare) | Auto-manage Cloudflare DNS records | bitnami/external-dns |
| external-dns (AdGuard) | Auto-manage AdGuard Home DNS records | custom |
| k8s-gateway | Split-horizon internal DNS | custom |
| cloudflare-tunnel | Zero-trust external access | cloudflared |
| certificates | TLS cert definitions | cert-manager CRDs |
| librespeed | Network speed test | custom |
| whoami | Ingress routing test | custom |

**IngressClasses:** `internal` (home network), `external` (Cloudflare), `tailscale` (VPN)

### longhorn-system — Distributed Storage

3x replicated block storage. Each node has a dedicated 1TB XFS disk at `/var/mnt/longhorn`.

- **StorageClass:** `longhorn` (default for stateful apps)
- **Replication:** 3 replicas across all nodes
- **Expansion:** Online PVC resize supported

### openebs-system — Local Storage

Node-local high-performance storage with no replication.

- **StorageClass:** `openebs-local`
- **Use cases:** Caches, temp data, dev/test workloads

### nfs-system — Shared Network Storage

NFS shares provisioned as PVCs — `ReadWriteMany` for shared access, backups, archives.

### volsync-system — Backup & Replication

Automated PVC backup via ReplicationSource CRDs. Backed up to NFS or cloud storage on a cron schedule (daily 2am). Templates in `kubernetes/components/volsync/`.

### cloudnative-pg — PostgreSQL Operator

Manages PostgreSQL clusters via the `Cluster` CRD. Automated failover, replication, and backup integration with Volsync.

### database — Database Instances

| Component | Purpose |
|-----------|---------|
| postgres-cluster | Primary PostgreSQL instance |
| neo4j | Graph database (local/personal use — not production) |

**PostgreSQL access:** `postgres-cluster.database.svc.cluster.local`

### observability — Monitoring & Logging

| Component | Purpose | Chart |
|-----------|---------|-------|
| kube-prometheus-stack | Prometheus + Alertmanager + Grafana | prometheus-community/kube-prometheus-stack |
| loki | Log aggregation | grafana/loki-stack |
| glance-k8s | K8s visibility widgets for Glance dashboard | lukasdietrich/glance-k8s |
| prometheus-proxy | Alertmanager Tailscale proxy | custom (ExternalName service) |

**Alertmanager:**
- **External URL (Tailscale):** `https://alertmanager.driscoll.tech`
- **Alerts API:** `https://alertmanager.driscoll.tech/api/v2/alerts`
- **Internal URL:** `http://alertmanager-operated.observability.svc.cluster.local:9093/api/v2/alerts/`
- Exposed on the Tailscale tailnet via `tailscale.com/tailnet-fqdn` annotation on the `alertmanager-operated` ExternalName service
- Flux notification provider sends GitOps events to alertmanager

**glance-k8s:**
- Exposed at `glance-k8s.sgc.internal` (Tailscale DNS)
- Endpoints: `/extension/nodes`, `/extension/apps`
- Cluster-wide list permissions via ServiceAccount

**Custom Application CRD** (`application-crd.yaml`):
- Defines `sgc.home-ops.local/v1alpha1` Application resources
- Used to register apps with Authentik for automatic SSO
- Synced to home-operations Pulumi stack

### sgc — Primary Application Namespace

Main application space, organized into sub-directories:

```
kubernetes/apps/sgc/
├── shared/        # Shared ConfigMaps, Secrets, RBAC
├── home/          # Home automation
│   ├── home-assistant/   # Home automation hub
│   ├── mosquitto/        # MQTT broker
│   ├── matter/           # Thread border router / Matter hub
│   ├── chrony/           # NTP server
│   └── oxycloud/         # Custom service
├── idp/           # Identity provider (Authentik)
├── dns/           # Custom DNS services
├── media/         # Media management (tdarr-node, etc.)
└── scrypted/      # (disabled)
```

**Home automation dependencies:** home-assistant depends on mosquitto (MQTT); matter provides Thread border router.

### system-upgrade — OS & Kubernetes Upgrades

| Component | Purpose |
|-----------|---------|
| upgrade-controller | Orchestrates rolling node upgrades |
| Plan CRD | Defines upgrade targets (Talos version, k8s version) |

### tailscale-system — VPN & Identity

| Component | Purpose | Image |
|-----------|---------|-------|
| tailscale-operator | Kubernetes Operator for Tailscale | tailscale/operator |
| tsidp | Tailscale OIDC Identity Provider | ghcr.io/tailscale/tsidp:v0.0.12 |
| authkey | ExternalSecret pulling Tailscale auth key from 1Password | — |

**Tailscale access model:**
- `tailnet-outbound` proxy group: exposes cluster services to tailnet devices
- `tailnet-inbound` proxy group: proxies external tailnet services into cluster
- Device ACL tag: `tag:apps`
- Apps use `kubernetes/components/tailscale/ingress.yaml` component to get Tailscale ingress

**tsidp (OIDC IDP):**
- Tailnet hostname: `idp`
- Provides OIDC tokens backed by Tailscale device identity
- STS enabled for service-to-service auth
- State in PVC at `/data`

## Application Deployment Pattern

### Standard File Structure

```
kubernetes/apps/<namespace>/<app>/
├── ks.yaml              # Flux Kustomization (interval, dependsOn, postBuild vars)
├── kustomization.yaml   # Kustomize (components + resources)
├── helmrelease.yaml     # HelmRelease (chart source, version, values)
├── externalsecret.yaml  # Pull secrets from 1Password (optional)
└── values.yaml          # Extra Helm values (optional)
```

### HelmRelease Conventions

```yaml
apiVersion: helm.toolkit.fluxcd.io/v2
kind: HelmRelease
metadata:
  name: &app home-assistant   # &app anchor for self-reference
  namespace: sgc
spec:
  interval: 1h
  chart:
    spec:
      chart: home-assistant
      sourceRef:
        kind: HelmRepository
        name: home-assistant   # defined in kubernetes/flux/meta/repos/
  install:
    remediation:
      retries: 7
  upgrade:
    cleanupOnFail: true
    remediation:
      retries: 7
      strategy: rollback
  values:
    # app-specific values
```

### Common Management Tasks

```bash
# Deploy a new application
mkdir kubernetes/apps/<namespace>/<app>/
# Create ks.yaml, kustomization.yaml, helmrelease.yaml
# Add to parent kustomization.yaml resources list
git add -A && git commit && git push
# Flux auto-deploys

# Update a Helm chart version
# Edit helmrelease.yaml chart.spec.version
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/ --verbosity 1
git add -A && git commit && git push
# Watch: flux get hr -A -w

# Add an encrypted secret
sops --encrypt -i kubernetes/apps/<ns>/<app>/secret.sops.yaml
# Reference in kustomization.yaml resources or helmrelease.yaml valuesFrom

# Expose app on Tailscale
# Add to kustomization.yaml:
# components:
#   - ../../../../components/tailscale
# Set TAILSCALE_HOST and TAILSCALE_PORT postBuild vars
```
