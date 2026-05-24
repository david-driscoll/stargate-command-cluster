# Stargate Command Cluster Documentation

Production Talos Kubernetes cluster managed with Flux CD GitOps.

## Start Here

**[codebase/INDEX.md](./codebase/INDEX.md)** — complete navigation, cluster facts, key commands

## Codebase Knowledge (structure, conventions, tooling)

| Document | What it covers |
|----------|----------------|
| [codebase/STACK.md](./codebase/STACK.md) | All tools, versions, CLI dependencies, key commands |
| [codebase/STRUCTURE.md](./codebase/STRUCTURE.md) | Directory layout, entry points, naming conventions |
| [codebase/ARCHITECTURE.md](./codebase/ARCHITECTURE.md) | GitOps flow, layers, patterns, node specs |
| [codebase/CONVENTIONS.md](./codebase/CONVENTIONS.md) | YAML conventions, formatting, CI validation |
| [codebase/INTEGRATIONS.md](./codebase/INTEGRATIONS.md) | External APIs, secrets, 1Password, Cloudflare, Tailscale |
| [codebase/TESTING.md](./codebase/TESTING.md) | flux-local CI validation, kubeconform |
| [codebase/CONCERNS.md](./codebase/CONCERNS.md) | Tech debt, risks, fragile areas |

## Operational Reference (how the cluster works)

| Document | What it covers |
|----------|----------------|
| [codebase/FLUX.md](./codebase/FLUX.md) | GitOps reconciliation, Kustomize components, SOPS, webhooks |
| [codebase/APPLICATIONS.md](./codebase/APPLICATIONS.md) | All namespaces and services (50+ apps) |
| [codebase/NETWORKING.md](./codebase/NETWORKING.md) | Cilium, DNS, ingress, Cloudflare Tunnel, Tailscale VPN, Alertmanager |
| [codebase/STORAGE.md](./codebase/STORAGE.md) | Longhorn, OpenEBS, NFS, Volsync backups |
| [codebase/TALOS.md](./codebase/TALOS.md) | OS config, nodes, extensions, bootstrap, upgrades |

## Operations & Runbooks

| Document | What it covers |
|----------|----------------|
| [OPERATIONS.md](./OPERATIONS.md) | Daily ops, active alert queries, troubleshooting runbooks, upgrade procedures |

## Key Operational Endpoints

| Endpoint | Access | Purpose |
|----------|--------|---------|
| `https://alertmanager.driscoll.tech/api/v2/alerts` | Tailscale required | Query all active cluster alerts |
| `https://alertmanager.driscoll.tech` | Tailscale required | Alertmanager UI |
| `https://apiserver.sgc.driscoll.tech:6443` | Tailscale or local network | Kubernetes API |
| `idp` (tailnet hostname) | Tailscale required | Tailscale OIDC IDP |
