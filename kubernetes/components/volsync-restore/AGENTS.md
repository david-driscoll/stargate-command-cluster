# Component

- This is the volsync-restore component: the one-time `restore-once` ReplicationDestination
  split out of `components/volsync`.
- Add it to an app's `ks.yaml` `components:` list only while bootstrapping that app from an
  existing restic backup (fresh deploy, migration, disaster recovery).
- Remove it again once the restore has completed (`kubectl get replicationdestination
  ${APP}-dst -n <namespace>` shows a `lastSyncTime`). Leaving it in permanently leaves a fully
  replicated `${APP}-dst-dest`/`${APP}-dst-cache` PVC pair on disk forever for a trigger that
  will never fire again — this is what caused the 2026-07 Longhorn storage incident.
