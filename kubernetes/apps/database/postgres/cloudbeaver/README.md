# CloudBeaver - Static connections via ConfigMap + ExternalSecret

This app deploys CloudBeaver with two pre-configured connections (Postgres + Redis). Configuration is driven by:

-   ConfigMap: `data-sources.json` and `cloudbeaver.conf` are kept in `resources/` and turned into a `ConfigMap` via the kustomize `configMapGenerator`.
-   ExternalSecret: `externalsecret.yaml` turns the ConfigMap into a Secret `${APP}-config` and injects secrets from `database` ClusterSecretStore into template variables used by `data-sources.json` (for example, the `postgres-superuser` keys).

Usage:

1. Ensure `postgres-superuser` secret exists in the `database` ClusterSecretStore (created via the `postgres-push-secrets` kustomization).
2. Ensure `onepassword-connect` and `database` ClusterSecretStore definitions are present in the cluster (they are part of the general setup).
3. Deploy via Flux: this kustomization renders the ConfigMap and ExternalSecret and mounts them into CloudBeaver workspace at `/opt/cloudbeaver/workspace/GlobalConfiguration/.dbeaver`.

To change the connections, update `resources/data-sources.json` and commit to Git.
