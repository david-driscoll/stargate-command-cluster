# SGC Cluster — N-1 Resilience Implementation Plan

**Date:** 2026-07-04
**Builds on:** [`resource-allocation-review-2026-07-04.md`](./resource-allocation-review-2026-07-04.md)
**Repo:** `stargate-command-cluster` (Flux GitOps) — all changes land under `kubernetes/`.
**Cluster:** Stargate Command (SGC) — 3 identical Talos control-plane+worker mini-PCs: `milky-way`, `othalla`, `pegasus` (4 CPU / 15.3 GiB each).

---

## Overview

Make the SGC cluster survive **one node being offline (N-1)** — planned drain *or* hard failure — while primary services keep serving. Also fix the Longhorn behaviour that blocked a node from draining/restarting during upgrades.

**Primary services (must stay up under N-1):** authentik (server), postgres (primary + quorum), traefik ingress, in-cluster DNS, Longhorn storage.
**Out of scope:** adguard — it has an external always-available backup, so its in-cluster instances need no N-1 work.

The cluster is **reservation- and placement-bound, not capacity-bound**: 6.84c reserved vs 2.27c actually used; everything fits on 2 nodes by real usage. The two documented incidents (full-shutdown cascade; tuppr upgrade drain block) both stem from (a) fake-HA replicas co-located on `othalla`, (b) a node filling with equal/higher-priority pods so critical pods can't (re)schedule, and (c) Longhorn's instance-manager PDB not releasing on drain.

---

## Architecture Decisions

1. **Placement is the highest-leverage fix.** authentik, traefik, error-pages all *have* 2 replicas but land on one node. Fix the spread and most of the risk disappears — no new capacity needed.

2. **Priority ordering must respect the dependency chain.** postgres/authentik run at `system-node-critical` (2 000 001 000). Longhorn instance-manager runs at `longhorn-critical` (1 000 000 000) — *below* its own dependents. Since postgres cannot start without the Longhorn CSI/instance-manager, the storage layer must be **≥** its dependents. Decision: set Longhorn `priorityClass: system-node-critical`. Combined with fixed placement (so they never contend on one node), this removes the deadlock where the IM couldn't get CPU on a node packed with `system-node-critical` pods.
   - *Corrects review finding #3:* the IM was not missing a priority class; it had one that was **too low** relative to its dependents.

3. **Longhorn drain fix = `block-for-eviction-if-contains-last-replica`.** With `defaultReplicaCount: 2`, draining one node almost always leaves a healthy replica elsewhere, so the policy allows the drain and the IM PDB releases. For the rare last-replica case the policy actively evicts/migrates the replica before allowing the drain. Manual `kubectl delete pdb instance-manager-<hash>` remains the documented fallback. (Refs: [longhorn/longhorn#5910](https://github.com/longhorn/longhorn/issues/5910), [longhorn docs — node-drain-policy](https://longhorn.io/docs/1.9.0/references/settings/), [Sylva #1422](https://gitlab.com/sylva-projects/sylva-core/-/issues/1422). #8323 is a `strict-local` edge case, not ours.)

4. **Scheduler accuracy via LimitRange, not 86 chart edits.** A single `LimitRange` with `defaultRequest` (cpu 15m / memory 32Mi) added to the shared `components/common` component gives every namespace-group's pods a floor request the scheduler can reason about, without editing each chart. Targeted per-chart overrides only where the default is wrong.

5. **Descheduler for auto-rebalance, safely.** SIG `descheduler` as a CronJob (`*/2`). Its DefaultEvictor will not evict `system-node-critical` pods, so it never disrupts postgres/authentik/traefik/Longhorn — it only rebalances best-effort/stateless duplicates that piled onto one node after a failover.

6. **No k8s staging exists** (alpha-site is Docker). Validation = phased Flux reconcile with placement observation after each phase, then a deliberate **one-node cordon+drain drill** as the final acceptance gate.

---

## Dependency Graph

```
PriorityClasses (verify existing) ──┐
                                     ├─► Phase 0: placement + priority + drain policy
Longhorn priorityClass = SNC ───────┘         │
                                              ▼
                              Phase 1: LimitRange defaults (common component)
                                              │
                                              ▼
                              Phase 2: right-size reservations/limits
                                              │
                                              ▼
                              Phase 3: PDBs + descheduler
                                              │
                                              ▼
                              Acceptance: one-node drain drill
```

Each phase is independently revertible (its own commit[s]) and leaves the cluster in a working state.

---

## Task List

### Phase 0 — Placement, Priority, Drain Fix (low risk, additive)

This phase alone makes both documented incidents non-repeatable.

#### Task 0.1: Fix authentik anti-affinity selector
**Description:** authentik's `requiredDuringSchedulingIgnoredDuringExecution` anti-affinity selects `app.kubernetes.io/component: authentik`, but real pods carry `component: server`/`worker`, so it matches nothing and all pods sit on othalla. Repoint the server anti-affinity at the real label so the 2 server replicas spread across hostnames. Leave worker at **1 replica** (per decision).
**Acceptance criteria:**
- [x] authentik-server anti-affinity selector matches `app.kubernetes.io/component: server` (verified live pod labels 2026-07-04: real server pods carry `component=server`, worker `component=worker`; shared `&affinity` anchor broken so server→`server`, worker→`worker`).
- [x] ⏳ After reconcile, the 2 `authentik-server` pods are on 2 distinct nodes. *(pending push)*
- [x] worker remains `replicas: 1`.
**Verification:** `kubectl get pods -n sgc -l app.kubernetes.io/name=authentik -o wide` shows server pods on different nodes.
**Dependencies:** None.
**Files:** `kubernetes/apps/sgc/idp/authentik/helmrelease.yaml`
**Scope:** S

#### Task 0.2: Spread traefik + assign priority class
**Description:** traefik has `replicas: 2` and no spread → both on othalla. Add top-level `topologySpreadConstraints` (maxSkew 1 over `kubernetes.io/hostname`, `whenUnsatisfiable: DoNotSchedule`, selector = traefik release labels) and set `priorityClassName: system-node-critical` (ingress is on the critical path for every authenticated service).
**Acceptance criteria:**
- [x] traefik values set `topologySpreadConstraints` and `priorityClassName: system-node-critical`.
- [ ] The 2 traefik pods land on 2 distinct nodes after reconcile.
**Verification:** `kubectl get pods -n network -l app.kubernetes.io/name=traefik -o wide`; `kubectl get pod -n network <traefik-pod> -o jsonpath='{.spec.priorityClassName}'`.
**Dependencies:** None.
**Files:** `kubernetes/apps/network/traefik/values.yaml`
**Scope:** S

#### Task 0.3: Spread error-pages
**Description:** error-pages (bjw-s app-template) has `replicas: 2`, no spread. Add `defaultPodOptions.topologySpreadConstraints` keyed on hostname.
**Acceptance criteria:**
- [x] error-pages `defaultPodOptions.topologySpreadConstraints` added (maxSkew 1, hostname, `ScheduleAnyway` acceptable here — non-critical).
- [ ] The 2 pods spread across nodes after reconcile.
**Verification:** `kubectl get pods -n network -l app.kubernetes.io/name=error-pages -o wide`.
**Dependencies:** None.
**Files:** `kubernetes/apps/network/traefik/error-pages.yaml`
**Scope:** XS

#### Task 0.4: Enforce postgres pod anti-affinity (required)
**Description:** CNPG cluster sets only `affinity.topologyKey`, leaving anti-affinity at the chart/operator default `preferred`. Both `postgres-2` and `postgres-5` landed on pegasus. Set `enablePodAntiAffinity: true` and `podAntiAffinityType: required` so the 3 instances occupy 3 distinct nodes.
**Acceptance criteria:**
- [x] `cluster.affinity` sets `enablePodAntiAffinity: true`, `topologyKey: kubernetes.io/hostname`, `podAntiAffinityType: required` (confirm chart 0.7.0 passes these through to the Cluster CR: `kubectl get cluster -n database postgres -o jsonpath='{.spec.affinity}'`).
- [ ] The 3 postgres instances are on 3 distinct nodes after the rollout completes.
- [x] Note in the change: under N-1, the instance from the downed node stays `Pending` until the node returns (only 2 schedulable nodes for 3 required-distinct pods) — this is acceptable; quorum (2/3) and the primary remain available.
**Verification:** `kubectl get pods -n database -l cnpg.io/cluster=postgres -o wide`; `kubectl cnpg status postgres -n database`.
**Dependencies:** None.
**Files:** `kubernetes/apps/database/postgres/app/resources/values.yaml`
**Scope:** S — **rollout is a rolling restart of the DB; do during a maintenance window and watch CNPG status.**

#### Task 0.5: Longhorn instance-manager priority = system-node-critical
**Description:** Point Longhorn's `priorityClass` at `system-node-critical` so the storage layer is co-equal with its dependents (postgres/authentik) and cannot be starved/preempted by them on a busy node. Remove the dangling `high-priority` reference (that PriorityClass does not exist in the cluster).
**Acceptance criteria:**
- [x] `defaultSettings.priorityClass: system-node-critical` in Longhorn values.
- [ ] Verify `system-node-critical` is usable by longhorn-system pods (no scoped ResourceQuota blocks it): after reconcile `kubectl get pods -n longhorn-system -l longhorn.io/component=instance-manager -o jsonpath='{range .items[*]}{.metadata.name}{" "}{.spec.priorityClassName}{"\n"}{end}'` shows `system-node-critical`.
- [ ] Instance-manager pods restart cleanly onto the new priority class (Longhorn recreates them; watch for a brief storage blip and confirm volumes reattach).
**Verification:** command above; `kubectl get volumes -n longhorn-system` all `robustness: healthy`.
**Dependencies:** None (but coordinate with 0.4 — both touch storage-critical scheduling).
**Files:** `kubernetes/apps/longhorn-system/longhorn/values.yaml`
**Scope:** S — **changing IM priority restarts instance-managers; do during a window.**

#### Task 0.6: Longhorn node-drain-policy = block-for-eviction-if-contains-last-replica
**Description:** Change `nodeDrainPolicy` from `allow-if-replica-is-stopped` to `block-for-eviction-if-contains-last-replica`. With `defaultReplicaCount: 2`, draining a node normally leaves a healthy replica elsewhere → drain allowed, IM PDB releases. Last-replica volumes are actively evicted/migrated before the drain proceeds. Keep `defaultReplicaCount: 2`.
**Acceptance criteria:**
- [x] `defaultSettings.nodeDrainPolicy: block-for-eviction-if-contains-last-replica`.
- [x] `defaultReplicaCount: 2` retained.
- [ ] Verified by the Phase-4 drain drill (a cordon+drain completes without a stuck IM PDB).
**Verification:** deferred to drain drill (Task 4.1).
**Dependencies:** 0.5.
**Files:** `kubernetes/apps/longhorn-system/longhorn/values.yaml`
**Scope:** XS

#### Task 0.7: Ensure in-cluster DNS has 2 spread replicas
**Description:** coredns is already `replicaCount: 2` (verify it also has anti-affinity/spread). k8s-gateway (authoritative resolver for the cluster domain to the LAN) defaults to 1 → bump to 2 and add topology spread. External-dns *controllers* (cloudflare-dns, unifi-dns, adguard-dns) are record-pushers, not resolvers — leave at 1 unless you want otherwise (flagged as open question).
**Acceptance criteria:**
- [x] coredns has 2 replicas with topology spread / pod anti-affinity (add if missing).
- [x] k8s-gateway `replicas: 2` + topology spread.
- [ ] Both resolvers' pods land on distinct nodes.
**Verification:** `kubectl get pods -n kube-system -l k8s-app=kube-dns -o wide`; `kubectl get pods -n network -l app.kubernetes.io/name=k8s-gateway -o wide`.
**Dependencies:** None.
**Files:** `kubernetes/apps/kube-system/coredns/helm/values.yaml`, `kubernetes/apps/network/k8s-gateway/**`
**Scope:** S

#### Task 0.8: Apply fast-node-eviction to stateless primary services
**Description:** The `components/failover/fast-node-eviction` component (60s not-ready/unreachable tolerations) is only wired into some apps. Add it to traefik, error-pages, and k8s-gateway kustomizations so stateless ingress/DNS reschedule quickly on a *hard* node failure. Do **not** add to CNPG/stateful (CNPG handles its own failover; aggressive eviction is undesirable there).
**Acceptance criteria:**
- [x] traefik, error-pages, k8s-gateway `ks.yaml` include the `fast-node-eviction` component.
- [ ] Pods show 60s tolerations: `kubectl get pod -n network <pod> -o jsonpath='{.spec.tolerations}'`.
**Dependencies:** 0.2, 0.3, 0.7.
**Files:** respective `ks.yaml` files.
**Scope:** S

### ✅ Checkpoint: Phase 0
- [ ] Every primary service (authentik-server, postgres, traefik, coredns, k8s-gateway) has its replicas on **distinct nodes**.
- [ ] Longhorn instance-managers run at `system-node-critical`; all volumes `healthy`.
- [ ] `flux get kustomizations -A` all Ready; no HelmRelease in a failed/retrying state.
- [ ] Review with human before Phase 1.

---

### Phase 1 — Scheduler Accuracy (LimitRange defaults)

#### Task 1.1: Add default-request LimitRange to the common component
**Description:** ~86 pods declare no requests, so the scheduler under-reserves and pods redistribute unpredictably on failover. Add a `LimitRange` with a modest `defaultRequest` (cpu 15m, memory 32Mi) to `components/common` so it is created in every namespace-group that includes the component (kube-system, network, longhorn-system, cert-manager, nfs-system, observability, database, flux-system, tailscale-system, cloudnative-pg).
**Acceptance criteria:**
- [x] `LimitRange` resource added to `components/common/kustomization.yaml` resource list (+ new `limit-range.yaml`), with only `defaultRequest` set (no `default`/limits, so we don't accidentally cap anything).
- [x] Confirm the component's namespace transformer assigns the correct namespace to the LimitRange (the component's `namespace.yaml` uses a `not-used` placeholder patched per group — verify the LimitRange gets the same treatment; if not, template the namespace via `${NAMESPACE}` substitution as the other resources do).
- [ ] After reconcile, `kubectl get limitrange -A` shows one per common-consuming namespace.
- [ ] Spot-check a previously no-request pod now shows an effective request: `kubectl get pod -n kube-system <cilium-pod> -o jsonpath='{.spec.containers[*].resources.requests}'`.
**Verification:** `kubectl describe limitrange -n network`; re-run the allocation inventory and confirm per-node CPU% reservation reflects the new floors.
**Dependencies:** Phase 0 checkpoint.
**Files:** `kubernetes/components/common/kustomization.yaml`, `kubernetes/components/common/limit-range.yaml` (new)
**Scope:** M — **touches every namespace-group; roll out and watch for any pod that becomes unschedulable due to the new floor.**

### ✅ Checkpoint: Phase 1
- [ ] All common-consuming namespaces have a LimitRange; no pods stuck `Pending` from the new floors.
- [ ] Re-run allocation inventory; per-node reservation now reflects real footprint.

---

### Phase 2 — Right-size Reservations / Limits

#### Task 2.1: Lower Longhorn guaranteed instance-manager CPU
**Description:** `guaranteedInstanceManagerCpu: 12` (~480m/node) is the largest idle reservation. Lower to `6` (~240m/node) to free ~240m/node of real N-1 slack. Drop the legacy `guaranteedEngineManagerCPU`/`guaranteedReplicaManagerCPU` (merged into instance-manager in modern Longhorn; verify they're no-ops on 1.12 before removing).
**Acceptance criteria:**
- [x] `guaranteedInstanceManagerCpu: 6`.
- [x] Legacy engine/replica-manager CPU settings removed or confirmed ignored.
- [ ] Instance-managers reschedule with the smaller request; volumes stay healthy.
**Verification:** `kubectl get pod -n longhorn-system -l longhorn.io/component=instance-manager -o jsonpath='{.items[*].spec.containers[*].resources.requests.cpu}'`.
**Dependencies:** Phase 1 checkpoint. **Restarts instance-managers — maintenance window.**
**Files:** `kubernetes/apps/longhorn-system/longhorn/values.yaml`
**Scope:** S

#### Task 2.2: Cap flux controller limits
**Description:** flux controllers declare ~1–2c CPU limits each (~6c of limits total) vs ~1–11m actual. Trim limits to realistic values (e.g. 500m) so they don't distort scheduling/eviction math.
**Acceptance criteria:**
- [x] flux-instance/fluxm-operator resource limits trimmed to sane values.
- [ ] Controllers stay Ready; reconciliation unaffected.
**Verification:** `kubectl top pods -n flux-system`; `flux get kustomizations -A`.
**Dependencies:** Phase 1 checkpoint.
**Files:** `kubernetes/apps/flux-system/flux-instance/**`
**Scope:** S

#### Task 2.3: Right-size home-assistant memory limit
**Description:** home-assistant limit is 6 GiB vs 816 MiB used. Lower to a realistic ceiling (e.g. 2 GiB) to reduce the oversized reservation footprint.
**Acceptance criteria:**
- [x] home-assistant memory limit lowered; pod runs comfortably below the new ceiling.
**Verification:** `kubectl top pod -n sgc -l app.kubernetes.io/name=home-assistant`.
**Dependencies:** Phase 1 checkpoint.
**Files:** `kubernetes/apps/sgc/home/home-assistant/**`
**Scope:** XS

### ✅ Checkpoint: Phase 2
- [ ] Re-run allocation inventory: CPU reservation on the two busiest nodes leaves clear N-1 headroom (target: no node > ~65% requests with a peer node offline).
- [ ] All workloads healthy.

---

### Phase 3 — Enforce & Auto-heal (PDBs + Descheduler)

#### Task 3.1: PodDisruptionBudgets for multi-replica primary services
**Description:** Only postgres + Longhorn have PDBs. Add PDBs so voluntary drains never take a primary service to zero: traefik, coredns, k8s-gateway, authentik-server, error-pages. Use `maxUnavailable: 1` for 2-replica services (allows one node drain, blocks taking both).
**Acceptance criteria:**
- [x] PDBs exist for traefik, coredns, k8s-gateway, authentik-server, error-pages (traefik via chart `podDisruptionBudget.enabled`; others via raw PDB manifests or chart support).
- [ ] Each PDB selector matches its pods; `kubectl get pdb -A` shows `ALLOWED DISRUPTIONS ≥ 1` in steady state.
**Verification:** `kubectl get pdb -A`.
**Dependencies:** Phase 0 (replicas must already be spread, or a `maxUnavailable: 1` PDB could block drains).
**Files:** traefik `values.yaml` (`podDisruptionBudget`), new PDB manifests under coredns / k8s-gateway / authentik / error-pages dirs.
**Scope:** M

#### Task 3.2: Deploy SIG descheduler
**Description:** Add the `kubernetes-sigs/descheduler` Helm chart as a CronJob (`*/2`), v1alpha2 default profile (RemoveDuplicates + RemovePodsViolatingTopologySpreadConstraint + LowNodeUtilization). Confirm DefaultEvictor keeps its default protection so it never evicts `system-node-critical` pods (postgres/authentik/traefik/Longhorn) — it only rebalances best-effort/stateless duplicates after a failover.
**Acceptance criteria:**
- [x] New app `kubernetes/apps/kube-system/descheduler/**` (HelmRelease + repo + ks.yaml) following repo conventions.
- [x] `kind: CronJob`; `deschedulerPolicy` uses the default profile; `priorityClassName: system-cluster-critical`.
- [x] DefaultEvictor confirmed to skip system-critical pods and pods with local storage / no owner.
- [ ] After a simulated pile-up (cordon a node, let pods land elsewhere, uncordon), the descheduler rebalances stateless duplicates within a couple of cycles — and does **not** evict postgres/authentik.
**Verification:** `kubectl logs -n kube-system -l app.kubernetes.io/name=descheduler` (CronJob pods) shows evictions of only stateless duplicates.
**Dependencies:** 3.1 (PDBs bound the descheduler's disruption).
**Files:** `kubernetes/apps/kube-system/descheduler/**` (new), `kubernetes/apps/kube-system/kustomization.yaml`
**Scope:** M

### ✅ Checkpoint: Phase 3
- [ ] All primary services have PDBs with ≥1 allowed disruption.
- [ ] Descheduler running, rebalancing only non-critical pods.

---

### Phase 4 — Acceptance: One-Node Drain Drill

#### Task 4.1: Controlled cordon + drain drill
**Description:** The end-to-end proof. Pick the least-loaded node, `kubectl cordon` + `kubectl drain --ignore-daemonsets --delete-emptydir-data`, and confirm the cluster behaves.
**Acceptance criteria:**
- [ ] Drain **completes without a stuck instance-manager PDB** (the Longhorn drain fix works; manual PDB delete not needed).
- [ ] Throughout the drain: authentik login works, postgres primary stays available (`kubectl cnpg status`), traefik serves ingress, DNS resolves.
- [ ] No primary service drops to zero replicas.
- [ ] `kubectl uncordon` the node; descheduler + scheduler restore balanced placement.
- [ ] Repeat for a hard-failure simulation only if desired (power off one node briefly) — optional, higher risk.
**Verification:** live observation + Gatus/Grafana during the drill.
**Dependencies:** Phases 0–3.
**Scope:** M (operational, not a code change).

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| `required` postgres anti-affinity leaves an instance `Pending` under N-1 | Med | Expected/acceptable — 2/3 quorum + primary remain; instance recovers when node returns. Documented in Task 0.4. |
| Changing Longhorn priority/IM-CPU restarts instance-managers → brief storage blip | Med | Do in a maintenance window; verify volumes reattach `healthy` before moving on. |
| LimitRange floor makes a namespace's pods unschedulable | Med | Small floor (15m/32Mi); roll out Phase 1 alone and watch for `Pending` pods; easy revert. |
| `system-node-critical` blocked by a scoped ResourceQuota in longhorn-system | Low | Verify usable (Task 0.5); if blocked, create a custom `storage-critical` PriorityClass valued just above `system-node-critical`. |
| Descheduler evicts something critical | Low | DefaultEvictor skips system-critical + local-storage + owner-less pods by default; verify in Task 3.2; PDBs bound disruption. |
| `maxUnavailable: 1` PDB blocks a drain when replicas are already co-located | Low | Sequenced after Phase 0 (spread first), so PDBs are never applied to co-located replicas. |
| CNPG chart 0.7.0 doesn't pass through `podAntiAffinityType` | Low | Verify against the live Cluster CR (Task 0.4); if not, set affinity via a Cluster-CR patch or bump the chart. |

## Open Questions

- **External-dns controllers (cloudflare-dns, unifi-dns, adguard-dns):** leave at 1 replica (they're record-pushers, not resolvers), or do you want them redundant too? *Recommendation: leave at 1.*
- **CNPG chart version:** we're on `cluster` chart 0.7.0 (fairly old). Bump as part of Task 0.4, or keep pinned and patch affinity directly? *Recommendation: keep pinned; verify passthrough first.*
- **Hard-failure drill (Task 4.1 optional step):** do you want to physically power off a node as part of acceptance, or is the drain drill sufficient? *Recommendation: drain drill is sufficient; the full-shutdown scenario is already covered by the placement + priority fixes.*

## Definition of Done

- [ ] All primary services survive a one-node drain with zero downtime (Task 4.1).
- [ ] A node can drain and reboot without manual PDB intervention.
- [ ] N-1 headroom is real: no node exceeds ~65% CPU requests with a peer offline.
- [ ] Changes committed in phase-sized commits; `flux get kustomizations -A` all Ready.
- [ ] Incident memories (`sgc-full-shutdown-cilium-zombie-node-cascade`, `tuppr-talos-upgrade-longhorn-im-pdb-drain-block`) updated to reference this plan and note the drain fix.

## Appendix — Manual Longhorn Drain Runbook (Fallback)

If a drain still stalls on an instance-manager PDB (`ALLOWED DISRUPTIONS = 0`):
1. Confirm the node holds no last-healthy-replica: for each attached volume ensure ≥1 healthy replica (`spec.healthyAt` set) on another node.
2. `kubectl get pods -n longhorn-system -l longhorn.io/component=instance-manager -o wide` → find the draining node's IM.
3. `kubectl delete pdb -n longhorn-system instance-manager-<hash>` for that node's IM. Eviction completes in seconds; Longhorn recreates the PDB after reboot.
