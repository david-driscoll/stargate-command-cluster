# External Integrations

## Core Sections (Required)

### 1) Integration Inventory

| System | Type | Purpose | Auth model | Criticality | Evidence |
|--------|------|---------|------------|-------------|----------|
| GitHub | Git remote + Webhook | Source of truth for GitOps; Flux pulls from it | Deploy key (github-deploy.key) + push token | High | `kubernetes/flux/cluster/ks.yaml`, `github-push-token.txt` |
| 1Password Connect | Secret store API | Dynamic secret injection via ExternalSecret | Bearer token (`CONNECT_TOKEN` from op://) | High | `kubernetes/components/secret-store/`, `.mise.toml` |
| Cloudflare | DNS + Tunnel | External DNS records + tunnel for public ingress | API token (`Zone - DNS - Edit`) | High | `kubernetes/apps/network/external-dns/`, `cloudflare-tunnel.json` |
| Tailscale | VPN + IDP | Internal cluster access + OIDC IDP | OAuth (`TAILSCALE_CLIENT_ID/SECRET`), authkey Secret | High | `kubernetes/apps/tailscale-system/`, `.mise.toml` |
| Alertmanager | Internal HTTP API | Alert routing; Flux sends GitOps events to it | No auth (cluster-internal) | Medium | `kubernetes/components/alerts/alertmanager/provider.yaml` |
| Equestria cluster | Sibling cluster (1Password Connect host) | Hosts the 1Password Connect server used by this cluster | op:// credential | High | `.mise.toml` (`CONNECT_HOST`) |
| Home Assistant | Application | Home automation hub | [ASK USER] | Medium | `kubernetes/apps/sgc/home/home-assistant/` |
| Authentik | SSO/OIDC | Single sign-on for cluster applications | [ASK USER] | Medium | `kubernetes/apps/sgc/idp/authentik/` |
| Renovate | GitHub App | Automated dependency update PRs | GitHub App (configured per repo) | Low | `.renovaterc.json5` |
| AdGuard Home | DNS filter | Home network DNS filtering + ad blocking | [ASK USER] | Medium | `kubernetes/apps/network/external-dns/adguard/` |
| CloudNative-PG | PostgreSQL operator | Managed PostgreSQL for stateful apps | In-cluster TLS | Medium | `kubernetes/apps/cloudnative-pg/` |
| Neo4j | Graph database | Local/personal use — not production workload | [ASK USER] | Low | `kubernetes/apps/database/neo4j/` |

### 2) Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|-------|------|--------------|----------|----------|
| etcd | Kubernetes state store | Kubernetes API server (internal) | etcd quorum loss if 2+ nodes down | `kubernetes/apps/kube-system/etcd/`, `talos/etcd-pre-upgrade-*.db` |
| Longhorn | Primary persistent storage (3x replicated block) | CSI driver (`longhorn-system` namespace) | Volume rebuild time on node failure | `kubernetes/apps/longhorn-system/` |
| OpenEBS | Local persistent storage | CSI driver (`openebs-system` namespace) | No replication — node-local only | `kubernetes/apps/openebs-system/` |
| NFS | Shared network storage | CSI NFS driver (`nfs-system` namespace) | Depends on NFS server availability | `kubernetes/apps/nfs-system/` |
| CloudNative-PG | PostgreSQL | CNPG operator, app-specific clusters | [ASK USER] backup policy | `kubernetes/apps/cloudnative-pg/` |
| Valkey | Redis-compatible cache | Direct app connection | [TODO] — what apps use it | `kubernetes/apps/` (Valkey image in git log) |

### 3) Secrets and Credentials Handling

- **SOPS + AGE:** All secrets committed to git are encrypted with AGE. Key files: `age.key` (primary), `sgc.age.key`. Private keys never committed.
- **ExternalSecret + 1Password Connect:** Runtime secrets (API tokens, auth keys) are pulled from 1Password vault `Eris` via the Connect server at `https://op-connect.equestria.driscoll.tech/`. ClusterSecretStore defined in `kubernetes/components/secret-store/`.
- **Flux Decryption:** Each Kustomization that needs SOPS decryption declares `decryption.provider: sops` + `secretRef: sops-age`.
- **Hardcoding check:** No plaintext secrets found in scan. All `*.sops.yaml` files are encrypted.
- **Rotation:** 1Password secrets rotate at source; ExternalSecret refreshes on a schedule. AGE key rotation would require re-encrypting all `.sops.yaml` files.

### 4) Reliability and Failure Behavior

- **Flux HelmRelease:** `remediation.retries: 7`, `strategy: rollback` on upgrade failure, `cleanupOnFail: true` — standardized via `bjw-s/app-template` defaults.
- **Flux Kustomization:** `retryInterval: 2m` on reconciliation failures.
- **1Password Connect:** If Connect server is unavailable, ExternalSecret sync fails but existing secrets remain in cluster (not deleted on sync failure by default).
- **Tailscale:** If tailscale operator fails, VPN-only services become inaccessible but cluster continues operating.
- **Alertmanager timeout:** Flux provider fires-and-forgets; alert delivery failures don't block reconciliation.

### 5) Observability for Integrations

- **Prometheus/Grafana:** Metrics for all cluster components scraped in `observability` namespace. Accessible via Tailscale or internal ingress.
- **Alertmanager:** Active alerts queryable at `https://alertmanager.driscoll.tech/api/v2/alerts` — primary ops troubleshooting endpoint.
- **Loki:** Log aggregation for all pods; queryable via Grafana.
- **Flux notifications:** Flux `Alert` resources send reconciliation status to GitHub commit status API and Alertmanager.
- **Missing visibility:** No distributed tracing (no Jaeger/Tempo detected). External dependency health (1Password Connect, Cloudflare tunnel) not explicitly monitored at the app level.

### 6) Evidence

- `kubernetes/components/secret-store/secret-store.yaml` — 1Password Connect ClusterSecretStore
- `kubernetes/components/alerts/alertmanager/provider.yaml` — Alertmanager Flux provider
- `cloudflare-tunnel.json` — Cloudflare tunnel credentials
- `.mise.toml` — `CONNECT_HOST`, `CONNECT_VAULT`, `TAILSCALE_CLIENT_ID/SECRET`
- `kubernetes/apps/tailscale-system/idp/tsidp.yaml` — Tailscale IDP deployment
