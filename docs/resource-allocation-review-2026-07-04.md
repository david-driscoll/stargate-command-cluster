# SGC Cluster — Resource Allocation Review

**Date:** 2026-07-04
**Trigger:** Post-incident review after a full-shutdown recovery failure (milky-way booted into a Cilium-not-ready zombie → othalla CPU-saturated → Longhorn instance-manager `OutOfcpu` → CNPG postgres could not start).
**Goal:** Inventory all resource allocations and identify changes for reliable operation when **1 of 3 nodes is offline (N-1)**.

> Data captured live from the cluster on 2026-07-04. `req` = request, `lim` = limit, `act` = live usage (`kubectl top`). `*` = pod declares **no request**, `!` = pod declares **no limit**.

---

## 1. Cluster capacity

All three nodes are identical mini-PCs (Talos, control-plane + worker):

| Node | Capacity | Allocatable |
|------|----------|-------------|
| milky-way | 4 CPU / 15.3 GiB | 3.95 CPU / 14.8 GiB |
| othalla | 4 CPU / 15.3 GiB | 3.95 CPU / 14.8 GiB |
| pegasus | 4 CPU / 15.3 GiB | 3.95 CPU / 14.8 GiB |
| **Cluster total** | **12 CPU / 45.9 GiB** | **11.85 CPU / 44.4 GiB** |
| **Any 2 nodes (N-1 budget)** | — | **7.90 CPU / 29.6 GiB** |

---

## 2. Per-node allocation vs actual

| Node | Pods | CPU req | CPU% | Mem req | Mem% | Actual CPU | Actual Mem |
|------|------|---------|------|---------|------|-----------|-----------|
| milky-way | 19 | 0.92c | 23% | 2.8 GiB | 19% | 686m (17%) | 3.5 GiB (23%) |
| othalla | 83 | 3.17c | **80%** | 7.7 GiB | 52% | 3370m (85%) | 8.5 GiB (55%) |
| pegasus | 51 | 2.75c | **70%** | 9.7 GiB | 65% | 2595m (65%) | 9.6 GiB (62%) |
| **TOTAL** | **153** | **6.84c** | 58% | 20.2 GiB | 46% | ~2.3c pod / ~6.6c node | ~18–22 GiB |

**Cluster-wide totals:** requests **6.84 CPU / 20.2 GiB**, actual pod usage **2.27 CPU / 18.2 GiB**.
CPU is reserved at ~3× actual use. Memory reservation is close to real use.

---

## 3. N-1 survivability

**By requests, everything fits on 2 nodes — but barely, and placement defeats it:**

- CPU: 6.84c of requests vs 7.90c on 2 nodes → **1.06c headroom (~13%)**.
- Mem: 20.2 GiB vs 29.6 GiB → 9.4 GiB headroom.

**Why it still failed:** the incident was not a total-capacity problem. When one node's workload piled onto othalla, othalla hit 88% requests, leaving < 0.47c — not enough for othalla's Longhorn instance-manager (0.47c request), which then could not schedule and wedged all storage. Two structural causes:

1. **Longhorn instance-manager has no PriorityClass** → could not preempt a lower-priority pod → `OutOfcpu`.
2. **86 pods have no requests** → the scheduler under-reserves, so the real "headroom" is thinner than it looks, and pods redistribute unpredictably on failover.

---

## 4. Detailed allocation — every workload by namespace

```
Legend: cpu cores (m=milli), mem M=MiB/G=GiB. req=request lim=limit act=live. * = no request, ! = no limit

### cert-manager  (4 pods)  req 0.00c/0.0Gi  lim 0.00c/0.0Gi  act 0.01c/0.1Gi
    POD                                        NODE       cpuREQ cpuLIM cpuACT memREQ memLIM memACT
    cert-manager-57884c6847-ghfpf              othalla        0m     0m     1m     0M     0M    34M *!
    cert-manager-cainjector-67db78f868-m4j6f   pegasus        0m     0m     1m     0M     0M    56M *!
    cert-manager-webhook-567d569d7f-tgk2c      othalla        0m     0m     2m     0M     0M    12M *!
    trust-manager-64cb6cc5f7-9vqcx             pegasus        0m     0m     2m     0M     0M    28M *!

### cloudnative-pg  (2 pods)  req 0.00c/0.0Gi  lim 0.00c/0.0Gi  act 0.00c/0.1Gi
    barman-cloud-786fcc74f6-567q9              othalla        0m     0m     1m     0M     0M    16M *!
    postgres-operator-cloudnative-pg-58595dd7c othalla        0m     0m     3m     0M     0M    99M *!

### database  (4 pods)  req 1.20c/3.0Gi  lim 0.00c/12.0Gi  act 0.15c/0.8Gi
    postgres-2                                 pegasus      400m     0m    80m   1.0G   4.0G   480M
    postgres-5                                 pegasus      400m     0m    51m   1.0G   4.0G    84M
    postgres-7                                 othalla      400m     0m    19m   1.0G   4.0G   269M
    valkey-5f66486d4f-4g489                    pegasus        0m     0m     3m     0M     0M     5M *!

### flux-system  (5 pods)  req 0.45c/0.3Gi  lim 6.00c/5.0Gi  act 0.02c/0.3Gi
    flux-operator-8695c5cf87-8fh59             othalla      100m   2.00    11m    64M   1.0G   158M
    helm-controller-7b9cb4f854-6ld9h           pegasus      100m   1.00     3m    64M   1.0G    35M
    kustomize-controller-846d54df7c-7k5gs      pegasus      100m   1.00     1m    64M   1.0G    56M
    notification-controller-5f9b8844d6-d98hp   pegasus      100m   1.00     3m    64M   1.0G    30M
    source-controller-79545c595d-rm2wk         pegasus       50m   1.00     5m    64M   1.0G    43M

### kube-system  (44 pods)  req 1.73c/9.4Gi  lim 0.60c/22.1Gi  act 0.90c/7.9Gi
    kube-apiserver-milky-way                   milky-way    200m     0m    41m   2.0G   4.0G   856M
    kube-apiserver-othalla                     othalla      200m     0m   261m   2.0G   4.0G   1.9G
    kube-apiserver-pegasus                     pegasus      200m     0m   214m   2.0G   4.0G   2.0G
    onepassword-connect-b467d5dbf-z56kp        othalla      200m     0m     2m   128M   128M    42M
    coredns-84c7d5b76-8p7q4                    pegasus      100m   100m     4m   128M   128M    18M
    coredns-84c7d5b76-96np6                    othalla      100m   100m     6m   128M   128M    24M
    inteldeviceplugins-controller-manager-c5b6 othalla      100m   100m     3m   100M   120M    29M
    metrics-server-5dc75559f5-fqwrx            pegasus      100m     0m     5m   200M     0M    34M !
    node-feature-discovery-master-b5d5dcc8f-7g othalla      100m     0m     1m   128M   4.0G    17M
    kube-controller-manager-milky-way          milky-way     50m     0m     2m   256M     0M    72M !
    kube-controller-manager-othalla            othalla       50m     0m     2m   256M     0M    19M !
    kube-controller-manager-pegasus            pegasus       50m     0m    35m   256M     0M   223M !
    registry-55fc5df6b8-zbwkg                  pegasus       50m     0m     3m   512M   1.0G   230M
    intel-gpu-plugin-intel-gpu-plugin-25j7r    pegasus       40m   100m     1m    45M    90M     7M
    intel-gpu-plugin-intel-gpu-plugin-2tld8    othalla       40m   100m     1m    45M    90M    10M
    intel-gpu-plugin-intel-gpu-plugin-k25l4    milky-way     40m   100m     1m    45M    90M    20M
    headlamp-5664df6b9-btnwb                   othalla       20m     0m     1m   128M   512M    84M
    headlamp-5664df6b9-jwsjq                   pegasus       20m     0m     1m   128M   512M   132M
    external-secrets-reloader-8589c865c7-xpvfr othalla       10m     0m     2m    64M   512M    60M
    kube-scheduler-milky-way                   milky-way     10m     0m     4m    64M     0M    72M !
    kube-scheduler-othalla                     othalla       10m     0m     5m    64M     0M    33M !
    kube-scheduler-pegasus                     pegasus       10m     0m     4m    64M     0M    38M !
    node-feature-discovery-gc-5894446899-hkbsj othalla       10m     0m     1m   128M   1.0G    10M
    node-feature-discovery-worker-2zfbm        pegasus        5m     0m     1m    64M   512M    10M
    node-feature-discovery-worker-cs896        othalla        5m     0m     6m    64M   512M    16M
    node-feature-discovery-worker-d8dcr        milky-way      5m     0m     1m    64M   512M    45M
    cilium-2pf8t                               pegasus        0m     0m   110m     0M     0M   325M *!
    cilium-8gv2p                               othalla        0m     0m   112m     0M     0M   379M *!
    cilium-8mvxq                               milky-way      0m     0m    49m     0M     0M   493M *!
    cilium-operator-7d96f485cf-jh4v9           othalla        0m     0m     4m     0M     0M    84M *!
    external-secrets-575488649f-ckqsm          othalla        0m     0m     1m     0M     0M    59M *!
    external-secrets-cert-controller-74ddc86fc pegasus        0m     0m     1m     0M     0M    54M *!
    external-secrets-webhook-989bb6b44-dx5vs   othalla        0m     0m     1m     0M     0M    37M *!
    multus-85spl                               pegasus        0m     0m     1m     0M     0M     3M *!
    multus-sfdxb                               othalla        0m     0m     1m     0M     0M     4M *!
    multus-w9fhp                               milky-way      0m     0m     1m     0M     0M     6M *!
    onepassword-connect-operator-5bfb897f7-jls othalla        0m     0m     1m     0M     0M    61M *!
    reflector-6d67d55864-pqmmg                 pegasus        0m     0m     1m     0M     0M   307M *!
    reloader-5f8596bb8-g85pg                   pegasus        0m     0m     1m     0M     0M    69M *!
    snapshot-controller-86b7dc9cb8-kk5dc       othalla        0m     0m     1m     0M     0M    22M *!
    snapshot-controller-86b7dc9cb8-vlwsb       othalla        0m     0m     1m     0M     0M    12M *!
    spegel-2r897                               othalla        0m     0m     3m   128M   128M    33M
    spegel-fwdmk                               milky-way      0m     0m     4m   128M   128M    21M
    spegel-th98f                               pegasus        0m     0m     3m   128M   128M    15M

### longhorn-system  (35 pods)  req 1.42c/0.0Gi  lim 0.00c/0.0Gi  act 0.68c/2.4Gi
    instance-manager-59d4d80fa82008280bda47a17 othalla      474m     0m   122m     0M     0M   447M !
    instance-manager-5d4adc54259701759f65ce2e2 milky-way    474m     0m    18m     0M     0M   158M !
    instance-manager-f746d0972246e47ff8770b231 pegasus      474m     0m   183m     0M     0M   633M !
    (+ csi-attacher x3, csi-provisioner x3, csi-resizer x3, csi-snapshotter x3,
       engine-image x9, longhorn-csi-plugin x3, longhorn-manager x3, longhorn-ui x2,
       longhorn-driver-deployer, share-manager x2 — all 0 request, 0 limit)

### network  (13 pods)  req 0.32c/0.3Gi  lim 0.00c/1.3Gi  act 0.02c/0.5Gi
    traefik-6dc8bcd4cb-ft2vb                   othalla      100m     0m     4m   100M   512M   114M
    traefik-6dc8bcd4cb-zgh78                   othalla      100m     0m     5m   100M   512M   113M
    librespeed-55c6b84b4c-x8dbw                othalla       50m     0m     1m    32M   128M    17M
    openspeedtest-7b6dc77d5-r5pgl              othalla       50m     0m     1m    32M   128M     8M
    traefik-whoami-5fdcbf474-v4n62             othalla       20m     0m     0m    84M    84M     1M
    adguard-dns-9cf88df57-jsbzt                othalla        0m     0m     1m     0M     0M    44M *!
    cloudflare-dns-5d66fccf8d-2wps4            othalla        0m     0m     1m     0M     0M    55M *!
    cloudflare-tunnel-...-remote (x2)          pegasus/othalla  0m   0m     3m     0M     0M    16M *!
    error-pages-6f54d999d4-2k8cs               othalla        0m     0m     0m     0M     0M     4M *!
    error-pages-6f54d999d4-s7grn               othalla        0m     0m     0m     0M     0M     5M *!
    k8s-gateway-f48898d78-drtxv                othalla        0m     0m     3m     0M     0M    21M *!
    unifi-dns-755b7774fd-5lhcl                 pegasus        0m     0m     1m     0M     0M    75M *!

### nfs-system  (5 pods)  req 0.14c/0.3Gi  lim 0.00c/2.8Gi  act 0.01c/0.2Gi
    csi-nfs-controller-9cc7d99f7-5wctn         othalla       40m     0m     4m    80M   1.1G    40M
    csi-nfs-node-95lnv/m62cp/n4cvm (x3)        all nodes     30m     0m     2m    60M   500M    ~50M
    snapshot-controller-7bc5c67495-hwwfb       othalla       10m     0m     1m    20M   300M    17M

### observability  (13 pods)  req 0.43c/1.7Gi  lim 1.60c/5.8Gi  act 0.15c/2.5Gi
    alloy-9825s / -lllsh / -rgkq7 (x3)         all nodes    110m   400m ~25m    250M   600M ~275M
    prometheus-prometheus-0                    pegasus      100m   400m    59m   1.0G   4.0G   1.5G
    blackbox-exporter-77f89dccf4-8pvkc         othalla        0m     0m     2m     0M     0M    23M *!
    kube-state-metrics-7fdc7c6795-rxjg8        othalla        0m     0m     4m     0M     0M    35M *!
    node-exporter (x3)                         all nodes      0m     0m     1m     0M     0M   ~18M *!
    prometheus-operator-689c494855-wlg65       othalla        0m     0m     3m     0M     0M    37M *!
    smartctl-exporter-0 (x3)                   all nodes      0m     0m     1m     0M     0M   ~13M *!

### sgc  (13 pods)  req 1.04c/5.2Gi  lim 0.00c/11.9Gi  act 0.22c/2.6Gi
    home-assistant-7bdd4656ff-p6rnr            pegasus      250m     0m    11m   2.1G   6.0G   816M
    authentik-server-7684c458b5-95sx9          othalla      200m     0m    85m   512M   1.0G   531M
    authentik-server-7684c458b5-h46nw          othalla      200m     0m    34m   512M   1.0G   552M
    authentik-worker-7cfc946cd8-b78jw          othalla      200m     0m    67m   512M   2.0G   313M
    adguard-home-0                             othalla       50m     0m    13m   500M   500M   213M
    adguard-home-1                             othalla       50m     0m     4m   500M   500M   158M
    adguard-home-2                             pegasus       50m     0m     -    500M   500M     -
    chrony-0                                   othalla       23m     0m     0m    52M     0M     7M !
    mosquitto-0                                othalla       10m     0m     1m     8M   200M     3M
    mosquitto-1                                pegasus       10m     0m     1m     8M   200M     2M
    adguard-home-sync-77ccbf69dc-sfw4k         othalla        0m     0m     0m     0M     0M    23M *!
    authentik-outpost-7748d788f-z9l7k          othalla        0m     0m     3m     0M     0M    51M *!

### tailscale-system  (12 pods)  req 0.01c/0.0Gi  lim 0.00c/0.0Gi  act 0.10c/0.5Gi
    tailnet-inbound-0 / tailnet-outbound-0 / ts-* (x6)   mostly othalla   1m  0m  ~15m  1M 0M ~55M !
    nameserver / operator / sgc-kubeproxy x2 / tsidp      mostly othalla   0m  0m   ~1m  0M 0M ~40M *!

### Other namespaces (minimal)
    openebs-system/openebs-localpv-provisioner  othalla      0m 0m 3m  0M 0M 12M *!
    system-upgrade/tuppr-5845b49b5b-db5bz       pegasus      0m 0m 4m  0M 0M 137M *!
    volsync-system/volsync-6f7cb9b9f8-v4tbb     othalla    100m 1.00 3m 64M 1.0G 76M

=== GRAND TOTAL: req 6.84c / 20.2 GiB | actual 2.27c / 18.2 GiB ===
```

---

## 5. Key findings

1. **Reservation-bound, not resource-bound.** 6.84c reserved vs 2.27c used. Plenty of real headroom; the problem is placement + reservation shape, not hardware.

2. **othalla is a concentration point / fake-HA.** Both `traefik` replicas, both `authentik-server` replicas, and both `error-pages` replicas run on **othalla**. Plus `adguard-dns`, `cloudflare-dns`, `k8s-gateway`, `authentik-worker`, `authentik-outpost`, 2/3 `adguard-home`, and most of `tailscale-system`. **Losing othalla drops ingress + DNS + auth simultaneously despite the replica counts** — there is no anti-affinity / topology spread.

3. **Longhorn instance-manager is the outage trigger.** 474m × 3 reserved (largest idle reservation), no PriorityClass, so it can't preempt when a node fills → `OutOfcpu` → storage wedged.

4. **86 pods have no requests; most have no limits.** Scheduler is blind to them (cert-manager, external-secrets, cilium, all CSI, all DNS, authentik-outpost, kube-state-metrics…). A few limits are wildly oversized (flux controllers 1–2c each = 6c of limits; home-assistant 6 GiB limit vs 816 MiB used).

5. **postgres not spread.** `postgres-2` (primary) and `postgres-5` both on pegasus; CNPG anti-affinity is `preferred`, not enforced. Losing pegasus drops 2/3 instances.

6. **Sparse PDBs.** Only postgres + Longhorn have PDBs; traefik/coredns/DNS/authentik unprotected during drains. Longhorn instance-manager PDBs are `allowed=0` (block Talos drains — see incident memory).

---

## 6. Proposed plan (phased GitOps changes)

> **Detailed implementation plan:** see [`sgc-n1-resilience-plan-2026-07-04.md`](./sgc-n1-resilience-plan-2026-07-04.md) — full task breakdown, acceptance criteria, and rollout. (Note: that plan corrects finding #3 below — the Longhorn IM had a priority class, just one ranked *below* its dependents.)

| Phase | Change | Why | Risk |
|-------|--------|-----|------|
| **0 — Stop the repeat** | Longhorn instance-manager `system-node-critical` PriorityClass; add `topologySpreadConstraints`/anti-affinity so traefik, authentik-server, error-pages replicas span nodes | Prevents the exact incident + makes existing replicas actually survive a node loss | Low |
| **1 — Scheduler accuracy** | Add modest requests (10–50m / 32–128Mi) to the ~86 no-request pods, starting with DNS, operators, CSI | Scheduler can spread & reserve → no silent overcommit on failover | Low |
| **2 — Right-size reservations** | Lower Longhorn `guaranteed-instance-manager-cpu` (~12%→~6%); postgres 400m→~200m; cap flux limits; trim home-assistant memory | Frees ~1c+ of reservation → real N-1 slack | Med |
| **3 — Enforce & protect** | `topologySpreadConstraints` on all multi-replica Deployments; PDBs for traefik/coredns/DNS/authentik; 2nd replica for single-replica DNS; optional descheduler for auto-rebalance | N-1 survivable by design, not luck | Med |

**Recommended start:** Phase 0 — low-risk, additive, fixes the two things that actually bite (storage preemption + fake-HA replica placement). Review Phase 1/2 numbers before applying.

---

## Appendix — incident summary

Full-shutdown recovery failed because **milky-way** rebooted into a zombie state: `Ready` to the API but kubelet unresponsive and `cilium-agent` never reaching Ready (`node.cilium.io/agent-not-ready` taint), so nothing scheduled there. Its workloads piled onto othalla → othalla CPU-saturated → othalla's Longhorn instance-manager `OutOfcpu` → Longhorn engines couldn't start → CNPG postgres stuck. Recovered by waking milky-way (Wake-on-LAN), then freeing CPU on othalla (relocating stateless pods to pegasus) so its instance-manager scheduled and CNPG brought postgres back to 3/3. No data was ever at risk (healthy Longhorn replicas existed on live nodes throughout).
