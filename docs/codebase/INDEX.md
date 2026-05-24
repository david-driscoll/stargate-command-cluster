# Stargate Command Cluster — Documentation Index

A production Talos Linux Kubernetes cluster managed declaratively with Flux CD GitOps.

## Quick Navigation

### Codebase Knowledge (structure, conventions, tooling)
| Document | What it covers |
|----------|----------------|
| [STACK.md](./STACK.md) | All tools, versions, CLI dependencies, key commands |
| [STRUCTURE.md](./STRUCTURE.md) | Directory layout, entry points, naming conventions |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | GitOps flow, layers, patterns, risks |
| [CONVENTIONS.md](./CONVENTIONS.md) | YAML conventions, formatting, CI validation |
| [INTEGRATIONS.md](./INTEGRATIONS.md) | External APIs, secrets, 1Password, Cloudflare |
| [TESTING.md](./TESTING.md) | flux-local CI validation, kubeconform |
| [CONCERNS.md](./CONCERNS.md) | Tech debt, risks, fragile areas |

### Operational Reference (how the cluster works)
| Document | What it covers |
|----------|----------------|
| [FLUX.md](./FLUX.md) | GitOps reconciliation, Kustomize components, SOPS, webhooks |
| [APPLICATIONS.md](./APPLICATIONS.md) | All namespaces and services (50+ apps) |
| [NETWORKING.md](./NETWORKING.md) | Cilium, DNS, ingress, Cloudflare Tunnel, **Tailscale VPN**, Alertmanager |
| [STORAGE.md](./STORAGE.md) | Longhorn, OpenEBS, NFS, Volsync backups |
| [TALOS.md](./TALOS.md) | OS configuration, nodes, extensions, bootstrap, upgrades |

### Operations
| Document | What it covers |
|----------|----------------|
| [../OPERATIONS.md](../OPERATIONS.md) | Daily ops, alertmanager health queries, troubleshooting runbooks |

## Cluster at a Glance

| Aspect | Value |
|--------|-------|
| **Kubernetes Version** | v1.36.1 |
| **Talos Version** | v1.13.2 |
| **Nodes** | 3 (milky-way, pegasus, othalla) — all control-plane + worker |
| **API Endpoint** | `https://10.10.209.201:6443` |
| **Namespaces** | 14 (cert-manager, cloudnative-pg, database, flux-system, kube-system, longhorn-system, network, nfs-system, observability, openebs-system, sgc, system-upgrade, tailscale-system, volsync-system) |
| **GitOps Engine** | Flux v2.8.8 (flux-operator v0.50.0) |
| **CNI** | Cilium 1.19.4 |
| **Secrets** | SOPS + AGE + 1Password Connect |
| **VPN Access** | Tailscale (operator + tsidp IDP) |
| **External Access** | Cloudflare Tunnel (zero-trust) |

## Key Operational Endpoints

| Endpoint | Access | Purpose |
|----------|--------|---------|
| `https://alertmanager.driscoll.tech/api/v2/alerts` | Tailscale required | Query all active cluster alerts |
| `https://alertmanager.driscoll.tech` | Tailscale required | Alertmanager UI |
| `https://apiserver.sgc.driscoll.tech:6443` | Tailscale or local | Kubernetes API |
| `idp` (tailnet hostname) | Tailscale required | Tailscale OIDC IDP |

## Common Task Commands

```bash
# Force Flux to sync
task reconcile

# Check cluster health
flux check && flux get ks -A && flux get hr -A

# Query active alerts (requires Tailscale)
curl https://alertmanager.driscoll.tech/api/v2/alerts | jq '.[].labels'

# Upgrade Talos on a node
task talos:upgrade-node IP=10.10.209.10

# Upgrade Kubernetes
task talos:upgrade-k8s

# Validate Flux locally before push
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/ --verbosity 1
```

## Ecosystem

This cluster is part of a larger home-ops setup:
- **[home-operations](https://github.com/david-driscoll/home-operations)** — Pulumi stack managing Authentik SSO and multi-cluster orchestration
- **[equestria-cluster](https://github.com/david-driscoll/equestria-cluster)** — Sibling cluster; hosts 1Password Connect server used by this cluster
- **Tailscale** — Provides secure VPN access across all clusters; `tsidp` runs on this cluster as the OIDC IDP
