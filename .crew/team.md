# Crew Team

> stargate-command-cluster — spoke crew, governed by `home-operations-crew`

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Crew | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Morpheus | Lead / Architect | `../home-operations/.crew/agents/morpheus/charter.md` | active |
| Tank | Kubernetes Workloads & Flux Delivery | `../home-operations/.crew/agents/tank/charter.md` | active |
| Seraph | Storage & Data Protection | `../home-operations/.crew/agents/seraph/charter.md` | active |
| Roland | Node Lifecycle & Cluster Upgrades | `../home-operations/.crew/agents/roland/charter.md` | active |
| Oracle | Observability & SRE | `../home-operations/.crew/agents/oracle/charter.md` | active |
| Sparks | CI/CD & GitHub Automation | `../home-operations/.crew/agents/sparks/charter.md` | active |
| Niobe | Networking & DNS | `../home-operations/.crew/agents/niobe/charter.md` | active |
| Dozer | Secrets & Identity | `../home-operations/.crew/agents/dozer/charter.md` | active |
| Mouse | Verification & Review | `../home-operations/.crew/agents/mouse/charter.md` | active |
| Trinity | Pulumi & TypeScript IaC | `../home-operations/.crew/agents/trinity/charter.md` | active |
| Scribe | Session Logger | `.crew/agents/scribe/charter.md` | active |
| Ralph | Work Monitor | `.crew/agents/ralph/charter.md` | active |
| Rai | RAI Reviewer | `.crew/agents/Rai/charter.md` | active |
| Fact Checker | Verifier | `.crew/agents/fact-checker/charter.md` | active |

> **Inherited roster.** This is a spoke crew. The cast above is owned by
> `home-operations-crew` (the hub) and reached via the `upstream` entry in
> `.crew/upstream.json`. Do not re-cast these agents here, and do not edit their
> charters from this repo — edit them in `home-operations`.
>
> Primary owner for this repo is **Tank**. Niobe covers ingress/DNS, Dozer covers
> sops/age, Mouse gates anything that reconciles against the live cluster.

## Project Context

- **Project:** stargate-command-cluster
- **Owner:** David Driscoll
- **Created:** 2026-07-26
- **Role:** Spoke crew
- **Stack:** Talos Linux + Kubernetes, Flux CD GitOps, sops/age, mise, Taskfile; manifests under `kubernetes/`
- **Hub:** `home-operations-crew` at `../home-operations`
- **Issue tracker:** `david-driscoll/vault` — do **not** open crew issues in this repo

### Notes

No crew GitHub workflows are installed here (`crew init --no-workflows`). Issue
automation lives in `david-driscoll/vault`, because that is where the issues are.
