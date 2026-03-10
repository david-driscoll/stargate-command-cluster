# Talos and Kubernetes Upgrade Runbook (v1.11.6 -> v1.12.5, v1.33.4 -> v1.33.9)

## Scope

This runbook covers direct-jump upgrade preparation and execution for:

- Talos: v1.11.6 -> v1.12.5
- Kubernetes/kubelet: v1.33.4 -> v1.33.9
- Tooling alignment: kubectl 1.33.4 -> 1.33.9, talosctl 1.11.6 -> 1.12.5

Primary Renovate PRs analyzed:

- https://github.com/david-driscoll/stargate-command-cluster/pull/859
- https://github.com/david-driscoll/stargate-command-cluster/pull/871
- https://github.com/david-driscoll/stargate-command-cluster/pull/872

## What Changes In This Repo

The version bump spans all control points:

- talos/talenv.yaml
    - talosVersion
    - kubernetesVersion
- versions.env
    - TALOS_VERSION
    - KUBERNETES_VERSION
- .mise.toml
    - kubectl
    - talosctl
- kubernetes/apps/system-upgrade/upgrades/talos.yaml
    - spec.talos.version
- kubernetes/apps/system-upgrade/upgrades/kubernetes.yaml
    - spec.kubernetes.version
- kubernetes/apps/kube-system/etcd-backup/helmrelease.yaml
    - talosctl image tag

## External Changes To Account For

### Talos 1.12 considerations

Key behavior changes called out in Talos docs and release notes:

- Some machine config fields are now ignored/locked in v1.12:
    - machine.features.rbac (locked true)
    - machine.features.apidCheckExtKeyUsage (locked true)
    - cluster.apiServer.disablePodSecurityPolicy (locked false)
- Kernel and hardening defaults changed across v1.12 patches.
- Talos recommends tested upgrades between adjacent minor releases.

References:

- https://docs.siderolabs.com/talos/v1.12/getting-started/what%27s-new-in-talos
- https://docs.siderolabs.com/talos/v1.12/configure-your-talos-cluster/lifecycle-management/upgrading-talos

### Kubernetes 1.35 considerations

- Kubernetes minor upgrades should not skip minor versions in control-plane progression logic.
- kubelet must not be newer than kube-apiserver.
- kubectl is supported only within +/-1 minor of kube-apiserver.
- For 1.35, FailCgroupV1 is true by default for kubelet configuration.

References:

- https://kubernetes.io/docs/setup/release/version-skew-policy/
- https://kubernetes.io/docs/tasks/administer-cluster/kubeadm/kubeadm-upgrade/

## Risk Hotspots For This Cluster

- HA control-plane upgrade sequencing can create temporary skew windows.
- Current TUPPR health checks are minimal (Node Ready + VolSync only).
- Maintenance windows in upgrade CRs are currently commented out.
- Previous failed upgrade suggests progression gates were not strict enough for rollout conditions.

## Implementation Added

A new task is available:

- task talos:preflight-upgrade

It validates before rollout:

- target versions from talenv.yaml
- client tooling visibility (kubectl, talosctl)
- all nodes Ready
- Talos API health
- VolSync ReplicationSource is not actively synchronizing

## Recommended Execution Order

1. Align branch with PR version bumps.
2. Run preflight checks:
    - task talos:preflight-upgrade
3. Take etcd backup:
    - task talos:backup-etcd
4. Upgrade Talos first:
    - task talos:upgrade
5. Validate node and control-plane stability.
6. Upgrade Kubernetes:
    - task talos:upgrade-k8s
7. Validate again and observe for a soak window.

## Validation Gates

Pass all of these before moving to the next stage:

- kubectl get nodes reports all Ready
- talosctl health completes successfully
- no active TupprUpgradeFailed or TupprUpgradeStuck alerts
- no VolSync resources stuck synchronizing

## Rollback and Stop Criteria

Stop rollout immediately if any of the following occurs:

- control-plane API instability
- repeated node NotReady after Talos reboot cycle
- upgrade enters failed/stuck alert states

Recovery options:

- pause/hold further version merges
- investigate failed node with talosctl logs and service status
- restore from etcd snapshot if control-plane state is compromised

## Notes

- Keep kubectl version within one minor of apiserver during transition.
- Avoid parallel risky changes (CNI, storage, ingress major changes) in the same window.
- Apply this runbook cluster-by-cluster with soak time between clusters.
