# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|------------------|
| High | AGE private keys on disk in project root | `age.key`, `sgc.age.key` present locally | If repo is accidentally pushed with keys, all SOPS secrets are compromised | Covered — `.gitignore` includes `/age.key`; keys also stored in 1Password |
| High | 1Password Connect on sibling cluster (equestria) | `.mise.toml` `CONNECT_HOST` | If equestria is down, new ExternalSecrets cannot be resolved | No failover planned — accepted risk for home lab |
| Medium | Neo4j in `database` namespace | Git log churn; local-use-only workload | Instability visible in commit history; not a production concern | Intentional — Neo4j is for local/personal use, not production |
| Medium | No live cluster E2E tests | `.github/workflows/flux-local.yaml` only tests rendering | Passing CI does not prove runtime correctness | Manual verification accepted; flux-local covers manifest correctness |
| Medium | Tailscale as sole access layer for internal services | `kubernetes/apps/observability/prometheus-proxy/alertmanager-operated.yaml` | Tailscale outage → loss of alertmanager, IDP access | Monitor Tailscale operator health |
| Resolved | etcd backup DBs | `talos/etcd-pre-upgrade-*.db` | N/A — `*.db` is in `.gitignore`; files are not tracked in git | No action needed |
| Resolved | Cloudflare tunnel credentials | `cloudflare-tunnel.json` | N/A — `/cloudflare-tunnel.json` is gitignored; credential stored in 1Password | No action needed |

### 2) Technical Debt

| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|-----------|---------------|-------|-----------------|---------------|
| High churn on `prometheus-proxy` HelmRelease | Ongoing prometheus configuration tuning | `kubernetes/apps/observability/prometheus-proxy/helmrelease.yaml` (28 changes in 90 days) | Fragile area; easy to break monitoring | Stabilize config; consider splitting concerns |
| Multiple skill directories (`.agents/`, `.claude/`, `.github/skills/`) | Skill syncing across Claude Code contexts | Root level | Confusion about which is canonical | Synced by `skillfile`/`apm` tooling — `.agents/` and `.github/skills/` are distribution copies |
| C# `Update.cs` scripts scattered in repo | Intentional — `do-update.cs` discovers and runs all `Update.cs` files in the repo for custom version update logic | `.mise/tasks/do-update.cs`, per-app `Update.cs` | .NET 10 required to run updates locally | Accepted — part of the update workflow alongside Renovate |

### 3) Security Concerns

| Risk | OWASP category | Evidence | Current mitigation | Gap |
|------|---------------|----------|--------------------|-----|
| Secrets in git | A02 (Cryptographic Failures) | `*.sops.yaml` files | SOPS + AGE encryption | AGE key management/rotation procedure unclear |
| Kernel security mitigations disabled on all nodes | N/A (OS hardening) | `talos/talconfig.yaml`: `mitigations=off`, `security=none`, `apparmor=0` | Performance trade-off accepted; nodes are home lab | Acceptable for home lab; document the decision |
| `cloudflare-tunnel.json` at repo root | A02 | File present at root | [TODO] — unclear if gitignored | Verify `.gitignore`; if not ignored, rotate tunnel credentials |
| Tailscale authkey in cluster | A07 (Auth Failures) | `kubernetes/apps/tailscale-system/authkey/authkey.yaml` (9 changes in 90 days) | ExternalSecret pulls from 1Password | High churn suggests frequent key rotation or troubleshooting |
| 1Password Connect token in mise env | A02 | `.mise.toml` `CONNECT_TOKEN = "op://Eris/..."` | op:// reference resolved at runtime by 1Password CLI | Not a static secret — safe |

### 4) Performance and Scaling Concerns

| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---------|----------|-----------------|-------------|-----------------------|
| Longhorn replication overhead | 3x replication on all volumes | Normal for 3-node cluster | Adding nodes without storage won't help replication | Acceptable for HA; document storage tiering |
| etcd compaction | `kubernetes/apps/kube-system/etcd/helmrelease.yaml` (13 changes in 90 days) | Ongoing etcd tuning | Large etcd with many resources can slow API | Regular defrag job in place (etcd-defrag fix in recent commits) |
| Single-region, single-site | Physical cluster at one location | N/A | Power/network outage = full cluster loss | Home lab limitation; accept the risk |

### 5) Fragile/High-Churn Areas

| Area | Why fragile | Churn signal | Safe change strategy |
|------|-------------|-------------|----------------------|
| `.mise.toml` | Central config for all tools + env; changes affect everyone | 71 commits in 90 days (highest churn file) | Test tool version bumps in isolation; check downstream commands |
| `kubernetes/apps/observability/prometheus-proxy/helmrelease.yaml` | Complex prometheus config with many moving parts | 28 commits in 90 days | Use `flux-local diff` before merging; check alertmanager (`https://alertmanager.driscoll.tech/api/v2/alerts`) after |
| `bootstrap/helmfile.yaml` | Bootstrap sequence is linear; wrong version = failed bootstrap | 22 commits in 90 days | Never change during live cluster ops; test in lab first |
| `kubernetes/apps/sgc/home/home-assistant/helmrelease.yaml` | Frequently updated (HA releases often) | 19 commits in 90 days | Let Renovate handle updates; avoid manual version changes |
| `kubernetes/apps/sgc/idp/authentik/helmrelease.yaml` | SSO breakage affects all authenticated apps | 19 commits in 90 days | Test login flow after every change |
| `kubernetes/apps/tailscale-system/authkey/authkey.yaml` | Was unstable during setup; now working correctly | 9 commits in 90 days — resolved | Stable; treat as low-risk going forward |
| `kubernetes/apps/network/traefik/` | Dual files (`values.yaml` + `helmrelease.yaml`) both churning | 9 + 9 commits | Validate ingress routing after changes |

### 6) `[ASK USER]` Questions

1. **[ASK USER]** What is `apm` (microsoft/apm v0.14.1) used for in this repo?
2. **[ASK USER]** Is there a 1Password Connect failover plan if the equestria cluster hosting Connect is unavailable?
3. **[ASK USER]** Are Talos config validations (`talhelper validate`) run in CI? They are not visible in the current GitHub Actions workflows.

*Resolved questions:*
- ~~etcd DB files in repo~~ — Covered by `*.db` in `.gitignore`; not tracked in git.
- ~~cloudflare-tunnel.json safety~~ — Gitignored; credential stored in 1Password.
- ~~Neo4j production status~~ — Local/personal use only, not production.
- ~~do-update.cs purpose~~ — Discovers and runs all `Update.cs` scripts across the repo.
- ~~Tailscale authkey churn~~ — Was unstable during setup; resolved and now stable.

### 7) Evidence

- Scan output: `docs/codebase/.codebase-scan.txt` (HIGH-CHURN FILES, CODE METRICS sections)
- `talos/etcd-pre-upgrade-20260308-*.db` — large binary files in repo
- `talos/talconfig.yaml` — kernel security flags
- `.github/workflows/flux-local.yaml` — CI scope
- `.mise.toml` — `CONNECT_HOST` pointing to sibling cluster
