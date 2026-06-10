---
name: create-k8s-deployment
description: Create standard Kubernetes deployments with components matching application requirements (postgres, OIDC, storage, secrets, tailscale)
---

# Create Standard Kubernetes Deployment

Your goal is to create a production-ready Kubernetes deployment following established patterns in equestria-cluster and stargate-command-cluster.

## Prerequisites

- Application name determined (e.g., `freshrss`, `glance`)
- Target namespace identified (e.g., `equestria`, `database`, `observability`)
- **Container image identified and version pinned** (see Container Image Selection below)
- Decision about component needs made (see Component Decision Tree below)
- Access to 1Password for secret management
- Update scripts available in repository for postgres passwords/secrets

## Container Image Selection

Before creating any deployment files, identify the container image and pin a specific version.

### Finding the Container Image

1. **Check application documentation** — Most projects specify their official container image
2. **Registry Preference Order** (most to least preferred):
   - **GHCR** (github.com container registry): `ghcr.io/OWNER/PROJECT-NAME:TAG`
     - Example: `ghcr.io/linuxserver/freshrss:v4.2.1`
   - **Quay** (quay.io): `quay.io/OWNER/IMAGE:TAG`
     - Example: `quay.io/prometheus/prometheus:v2.48.0`
   - **Docker Hub** (fallback): `docker.io/OWNER/IMAGE:TAG` or just `OWNER/IMAGE:TAG`
     - Example: `immich-official/immich-server:v1.102.0`
   - **Other registries**: `registry.example.com/...` (use only if official source)

### Finding and Pinning Versions

**Never use `latest`, `main`, or `dev` tags unless they are the only available option.**

#### Option 1: Browse Image Tags (GHCR)
```bash
# List available tags for a GHCR image
curl -s https://ghcr.io/v2/OWNER/PROJECT/tags/list | jq '.tags | sort_by(.) | reverse'

# Example: FreshRSS
curl -s https://ghcr.io/v2/linuxserver/freshrss/tags/list | jq '.tags | sort_by(.) | reverse' | head -20
```

#### Option 2: Browse Image Tags (Docker Hub)
```bash
# Use Docker Hub web interface: https://hub.docker.com/r/OWNER/IMAGE/tags
# Or use skopeo to list tags:
skopeo list-tags docker://docker.io/OWNER/IMAGE
```

#### Option 3: Check GitHub Releases
- Go to upstream project GitHub → Releases
- Look for version following semantic versioning (v1.2.3, not `latest`)
- Cross-reference with container tag naming

### Version Selection Strategy

1. **Identify latest stable release** (not pre-release, not `latest` tag)
   - Example: v1.102.0 is better than v1.103.0-rc1 or latest
2. **Pin exact version in helmrelease.yaml**
   ```yaml
   chart:
     spec:
       version: "1.102.0"  # Pin exact version, not range
   ```
3. **Document in code comment** why this version was chosen (if notable)
4. **If only `latest` available**: Use it, but note in PR/commit that this should be revisited when stable releases become available

### Recording the Image

Use the image reference in the Helm chart values:

```yaml
apiVersion: helm.toolkit.fluxcd.io/v2beta2
kind: HelmRelease
spec:
  chart:
    spec:
      sourceRef:
        kind: HelmRepository
        name: CHART_REPO  # e.g., linuxserver, immich-community
  values:
    image:
      registry: ghcr.io          # or docker.io, quay.io, etc.
      repository: OWNER/PROJECT
      tag: v1.102.0              # SPECIFIC VERSION, NO latest/dev
```

**Common Helm Chart Formats**:
- Some charts use `image.registry`, `image.repository`, `image.tag` (recommended)
- Others use `image: ghcr.io/owner/project:tag` or `image.repository: ghcr.io/owner/project`
- Check the Helm chart's `values.yaml` for the correct structure

## Component Decision Tree

Use this logic to determine which components your app needs:

### 1. Database Support
**Question**: Does the application require a persistent SQL database?
- **YES** → Add component: `../../../../components/postgres`
  - Include in `ks.yaml` dependsOn: postgres
  - Create/use ExternalSecret for DB credentials (managed by repo update script)
  - Set DB env vars: `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
  - Example: FreshRSS, Vikunja, Tandoor
- **NO** → Skip postgres component

### 2. Authentication/Authorization
**Question**: Does the application support OAuth2/OIDC natively?

- **YES (native OIDC)** → Add authentik oauth2 config to `definition.yaml` (recommended when the app supports OIDC directly):
  ```yaml
  authentik:
    oauth2:
      clientType: confidential
      allowedRedirectUris:
        - url: "https://${APP}.${ROOT_DOMAIN}/auth/openid/authentik"
        - url: "https://${APP}.${TAILSCALE_DOMAIN}/auth/openid/authentik"
      propertyMappings:
        - email
        - openid
        - profile
      includeClaimsInIdToken: true
      subMode: user_email
  ```

  - Credentials should be provided via ExternalSecret (see below).
  - For Outline specifically: prefer automatic discovery by supplying OIDC_ISSUER_URL plus OIDC_CLIENT_ID and OIDC_CLIENT_SECRET. When OIDC_ISSUER_URL is present Outline will auto-discover OIDC endpoints (AUTH/TOKEN/USERINFO). If automatic discovery is not possible, provide the three URIs manually (OIDC_AUTH_URI, OIDC_TOKEN_URI, OIDC_USERINFO_URI).
  - Optional env vars that can improve UX:
    - OIDC_DISPLAY_NAME — text displayed on the login button
    - OIDC_SCOPES — default: "openid profile email"
    - OIDC_USERNAME_CLAIM — default: "preferred_username"
    - OIDC_DISABLE_REDIRECT / OIDC_LOGOUT_URI — logout behavior

  - Example apps: FreshRSS, Immich, Outline

- **NO (no native OIDC)** → Add authentik forward proxy config to `definition.yaml` (proxy handles auth at ingress):
  ```yaml
  authentik:
    proxy:
      mode: "forward_single"
      externalHost: *url
  ```
  - Proxy handles auth entirely and injects user headers to the app
  - Example: Glance, Rustdesk

### 3. Persistent Storage
**Question**: Does the application need persistent data (not just DB)?
- **YES** → Add component: `../../../../components/volsync`
  - Include in `ks.yaml` dependsOn: volsync
  - Include in `ks.yaml` components list
  - Set PostBuild substitutions:
    ```yaml
    VOLSYNC_CAPACITY: "5Gi"  # adjust to app needs
    VOLSYNC_ACCESSMODES: ReadWriteOnce  # or ReadWriteMany
    ```
  - Example: Glance (for config), Windmill (for data), N8N (for workflows)
- **NO** → Skip volsync component

### 4. Network Connectivity
**Question**: Should the app be accessible over Tailscale?
- **YES** (most internal services)** → Add component: `../../../../components/tailscale`
  - Include in `ks.yaml` components list
  - Example: Most apps, except public-only dashboards
- **NO** → Skip tailscale component (rare for internal homelab)

### 5. Secret Management
**Question**: Are there secrets beyond oauth/db (encryption keys, API tokens, etc.)?
- **YES** → Create `externalsecret.yaml`
  - Use `onepassword-connect` ClusterSecretStore
  - Structure: environment variables in ExternalSecret template
  - User must manually add corresponding 1Password items
  - Example: FreshRSS crypto key, Immich JWT secret

- **NO** → Skip custom externalsecret (oauth/db secrets handled automatically)

### 6. Cache/Session Storage
**Question**: Does the app require Redis/caching?
- **YES** → Add dependency on valkey or redis
  - Include in `ks.yaml` dependsOn: valkey (or redis)
  - Configure connection environment variables
  - Example: Immich (ML caching), Home Assistant (cache)

### 7. Persistent Storage Method
**Question**: If storage needed, what type?
- **Volsync** (recommended): For replicable, backup-able volumes
  - Works with `components/volsync`
  - Best for application config/databases

- **NFS/TrueNAS**: For large shared storage
  - Create custom `nfs.yaml` in app directory
  - Define PersistentVolume with CSI driver
  - Include dependency: truenas-volumes
  - Example: Immich (media library), Downloads folder

- **Choose ONE** — Don't mix both for same mount point
├── kustomization.yaml       # Kustomize resources list
├── helmrelease.yaml         # Helm chart deployment
├── externalsecret.yaml      # (optional) 1Password secret injection
└── resources/               # (optional) Custom K8s manifests
```

### definition.yaml Template

```yaml
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/david-driscoll/stargate-command-cluster/refs/heads/main/schemas/definition.schema.json
apiVersion: driscoll.dev/v1
kind: ApplicationDefinition
metadata:
  name: ${APP}
spec:
  name: "Pretty Name"
  category: Applications          # or Services, PVR, Media, etc.
  description: "Brief description"
  url: &url https://${APP}.${ROOT_DOMAIN}
  icon: https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/svg/ICON_NAME.svg
  access_policy:
    groups:
      - family                     # or friends, admin, etc.
  authentik:
    # CHOICE: oauth2 OR proxy (not both)
    oauth2:
      clientType: confidential
      allowedRedirectUris:
        - url: https://${APP}.${ROOT_DOMAIN}:443/i/oidc/
        - url: https://${APP}.${TAILSCALE_DOMAIN}:443/i/oidc/
      propertyMappings:
        - email
        - openid
        - profile
    # OR:
    # proxy:
    #   mode: "forward_single"
    #   externalHost: *url
  gatus:
    - group: "Applications"
      url: *url
      method: GET
      conditions:
        - "[STATUS] == 200"        # adjust expected status code
```

### ks.yaml Template

```yaml
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/datreeio/CRDs-catalog/refs/heads/main/kustomize.toolkit.fluxcd.io/kustomization_v1.json
apiVersion: kustomize.toolkit.fluxcd.io/v1
kind: Kustomization
metadata:
  name: &app APP_NAME
  namespace: &namespace NAMESPACE_NAME
  labels:
    app.kubernetes.io/name: *app
    driscoll.dev/name: *app
spec:
  targetNamespace: *namespace
  commonMetadata:
    labels:
      app.kubernetes.io/name: *app
      driscoll.dev/name: *app
  dependsOn:
    # Add based on component selection:
    - name: volsync
      namespace: volsync-system
    - name: postgres
      namespace: database
    - name: external-secrets
      namespace: kube-system
  path: ./kubernetes/apps/CLUSTER/NAMESPACE/APP_NAME
  prune: true
  force: false
  wait: true
  interval: 1h
  timeout: 10m
  sourceRef:
    kind: GitRepository
    name: flux-system
    namespace: flux-system
  components:
    # Always include for resilience (unless single-node cluster)
    - ../../../../components/failover/fast-node-eviction
    # Add based on component selection:
    - ../../../../components/postgres
    - ../../../../components/tailscale
    - ../../../../components/volsync
  postBuild:
    substitute:
      APP: *app
      NAMESPACE: *namespace
      # For volsync:
      VOLSYNC_CAPACITY: 5Gi
      VOLSYNC_ACCESSMODES: ReadWriteOnce
```

### kustomization.yaml Template

```yaml
---
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ./helmrelease.yaml
  - ./definition.yaml
  # Add if externalsecret exists:
  # - ./externalsecret.yaml
  # Add if NFS storage is needed:
  # - ./nfs.yaml
```

### nfs.yaml Template (if using TrueNAS/NFS storage)

```yaml
---
# PersistentVolume for NFS-backed storage
apiVersion: v1
kind: PersistentVolume
metadata:
  name: APP_NAME-data
  labels:
    kustomize.toolkit.fluxcd.io/force: enabled
spec:
  accessModes:
    - ReadWriteMany                    # or ReadWriteOnce
  persistentVolumeReclaimPolicy: Retain
  mountOptions:
    - nfsvers=4.2
    - hard
    - intr
    - nconnect=8
    - noatime
  capacity:
    storage: 2Ti                       # Adjust to app needs
  storageClassName: nfs-csi
  csi:
    driver: nfs.csi.k8s.io
    volumeHandle: APP_NAME-volume-id
    volumeAttributes:
      server: "${STORAGE_SERVER_IP}"
      share: /mnt/path/to/app

---
# PersistentVolumeClaim to bind to PV above
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: ${APP}-data
spec:
  accessModes:
    - ReadWriteMany                    # Must match PV
  storageClassName: nfs-csi
  resources:
    requests:
      storage: 2Ti
```

**When to use nfs.yaml**:
- Large media libraries (photos, videos, music)
- Shared download folders
- Common storage accessed by multiple pods
- **Do NOT use** if Volsync is sufficient (e.g., small app config)

### externalsecret.yaml Template (if needed)

```yaml
---
apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
  name: ${APP}-env
  annotations:
    reloader.stakater.com/auto: "true"
spec:
  secretStoreRef:
    kind: ClusterSecretStore
    name: onepassword-connect
  refreshPolicy: Periodic
  refreshInterval: 4m
  target:
    name: ${APP}-env
    creationPolicy: Owner
    deletionPolicy: Retain
    template:
      metadata:
        labels:
          cnpg.io/reload: "true"
        annotations:
          reloader.stakater.com/auto: "true"
      data:
        TZ: "${TIMEZONE}"
        # Database / Redis / OIDC variables (examples)
        DATABASE_URL: "{{ .postgres_uri }}"     # from ${APP}-postgres in 'database' ClusterSecretStore
        REDIS_URL: "redis://valkey.database.svc.cluster.local:6379"
        ENCRYPTION_KEY: "{{ .outline_password }}"  # from Outline Secret Key (1Password)
        # OIDC (Outline supports auto-discovery via OIDC_ISSUER_URL)
        OIDC_ISSUER_URL: "{{ .oidc_issuer }}"
        OIDC_CLIENT_ID: "{{ .oidc_client_id }}"
        OIDC_CLIENT_SECRET: "{{ .oidc_client_secret }}"
  dataFrom:
    - extract:
        key: 'APP_NAME Secret Key'  # 1Password item name (user-managed)
      rewrite:
        - regexp:
            source: "[\\W]"
            target: _           # replace non-word chars with underscore
        - regexp:
            source: "(.*)"
            target: "outline_$1"  # prefix fields to avoid name collisions
    - extract:
        key: '${APP}-postgres'
      sourceRef:
        storeRef:
          kind: ClusterSecretStore
          name: database
      rewrite:
        - regexp:
            source: "[\\W]"
            target: _
        - regexp:
            source: "(.*)"
            target: "postgres_$1"  # postgres_username -> .postgres_username
    - extract:
        key: '${APP}-oidc-credentials'
      sourceRef:
        storeRef:
          kind: ClusterSecretStore
          name: cluster
      rewrite:
        - regexp:
            source: "[\\W]"
            target: _
        - regexp:
            source: "(.*)"
            target: "oidc_$1"      # oidc_client_id -> .oidc_client_id
```

How dataFrom / rewrite works
- dataFrom.extract.key selects an item by name from the configured secret store (1Password via onepassword-connect, or a ClusterSecretStore).
- Each field in the extracted item becomes a key available to the template as the dot-path (e.g., a field 'password' becomes `.password`).
- The rewrite block applies sequential regex transforms to each extracted field name; use it to normalize and/or prefix keys to avoid collisions (`outline_password`, `postgres_password`, `oidc_client_id`).
- Use sourceRef.storeRef to extract from other stores (cluster-level secrets created by repo update scripts). This is how `${APP}-postgres` (created by `mise run update`) is pulled from the `database` ClusterSecretStore.
- Reference extracted keys in `template.data` using `{{ .<rewritten_key> }}`. Example: `DB_PASSWORD: "{{ .postgres_password }}"` or `OIDC_CLIENT_ID: "{{ .oidc_client_id }}"`.
- Order/precedence: template.data reads the merged map. Explicit keys in `template.data` can reference any key that was produced by dataFrom. Avoid duplicate keys by using prefixes in rewrite rules.

Examples (Outline)
- Postgres from repo-managed cluster secret:
  - dataFrom extracts `${APP}-postgres` from `database` store and rewrites fields to `postgres_*`.
  - In `template.data`, use `DATABASE_URL: "{{ .postgres_uri }}"` or `DB_HOST: "{{ .postgres_hostname }}"`, etc.
- OIDC from cluster or 1Password:
  - Create a secret item `${APP}-oidc-credentials` with fields `client_id`, `client_secret`, `issuer` (or an openid configuration url).
  - dataFrom.rewrite -> `oidc_client_id`, `oidc_client_secret`, `oidc_issuer`.
  - In template.data set `OIDC_CLIENT_ID: "{{ .oidc_client_id }}"` and `OIDC_ISSUER_URL: "{{ .oidc_issuer }}"`.
- 1Password "Outline Secret Key" item:
  - If it contains a `password` field and rewrite prefixes `outline_`, reference `{{ .outline_password }}`.

Recommendations
- Always prefix rewrites to prevent name collisions (postgres_, oidc_, outline_).
- Prefer automatic discovery for Outline OIDC: provide OIDC_ISSUER_URL and let Outline auto-discover endpoints; otherwise provide explicit OIDC_AUTH_URI, OIDC_TOKEN_URI, OIDC_USERINFO_URI.
- Keep DB credentials in the repo-managed database secret when possible (update scripts handle rotation); use 1Password for application-generated secrets and tokens.


### helmrelease.yaml Template

Refer to the Helm chart's values.yaml. Structure:
```yaml
apiVersion: helm.toolkit.fluxcd.io/v2
kind: HelmRelease
metadata:
  name: &app APP_NAME
spec:
  chartRef:
    kind: OCIRepository              # Modern Flux approach (vs HelmRepository)
    name: app-template              # or other chart repo
  maxHistory: 3                      # Keep 3 revisions for rollback
  interval: 15m                      # Update check frequency
  driftDetection:
    mode: enabled                    # Detect config drift
  timeout: 10m
  install:
    createNamespace: true
    replace: true
    remediation:
      retries: 7                     # Retry count (or -1 for infinite)
  upgrade:
    crds: CreateReplace
    cleanupOnFail: true
    remediation:
      retries: 7
      strategy: rollback             # Rollback on failed upgrade
  rollback:
    force: true
    cleanupOnFail: true
    recreate: true
  uninstall:
    keepHistory: false
  values:
    fullnameOverride: *app
    controllers:
      app:
        annotations:
          reloader.stakater.com/auto: "true"  # Auto-reload on config changes
        strategy: Recreate                     # Or RollingUpdate for graceful updates
        pod:
          securityContext:
            runAsNonRoot: true
            runAsUser: 568                     # Non-root user ID
            runAsGroup: 568
            supplementalGroups: [0, 1, 44, 109, 303, 568, 10000]  # Unix groups for file access
          restartPolicy: Always
          terminationGracePeriodSeconds: 30   # Time to gracefully shutdown
        containers:
          app:
            image:
              registry: ghcr.io                # Prefer: GHCR > Quay > Docker Hub
              repository: OWNER/PROJECT
              tag: v1.102.0@sha256:abc123...  # Pin with image digest for security
              pullPolicy: IfNotPresent
            envFrom:
              - secretRef:
                    name: ${APP}-env           # ExternalSecret reference
            env:
              PUID: ${PUID}                    # Unix user ID (media apps)
              PGID: ${PGID}                    # Unix group ID
            securityContext:
              runAsNonRoot: true
              runAsUser: 568
              runAsGroup: 568
              allowPrivilegeEscalation: false
              readOnlyRootFilesystem: false    # Set true if app doesn't need write
              capabilities:
                drop: ["ALL"]                  # Drop all capabilities
            resources:
              requests:
                cpu: 100m
                memory: 256Mi
              limits:
                memory: 2Gi
            probes:
              liveness:
                httpGet:
                  path: /health
                  port: http
                initialDelaySeconds: 30
                periodSeconds: 10
              readiness:
                httpGet:
                  path: /ready
                  port: http
                initialDelaySeconds: 5
                periodSeconds: 5
        initContainers:
          fix-permissions:           # Optional: Run setup tasks
            image:
              repository: alpine
              tag: 3.23.3@sha256:abc123...
            command:
              - sh
              - -c
              - chmod -R 0777 /config || true
            securityContext:
              runAsUser: 0             # Run as root for system tasks
              runAsNonRoot: false
          machine-learning:           # Optional: Additional containers
            image:
              repository: ghcr.io/immich-app/immich-machine-learning
              tag: v2.6.1@sha256:def456...
            securityContext:
              runAsNonRoot: true
              runAsUser: 568
              allowPrivilegeEscalation: false
              capabilities:
                drop: ["ALL"]

# For apps with NFS or custom storage
volumes:
  - name: data
    persistentVolumeClaim:
      claimName: ${APP}-data
```

**Important Patterns**:
- **Image digest pinning**: Use `tag: v1.102.0@sha256:...` for security (prevents rollback to compromised images)
- **OCIRepository**: Modern Flux v2 approach (vs older HelmRepository)
- **driftDetection**: Detect when cluster state differs from git
- **Remediation strategy**: Auto-rollback on failed upgrades
- **Multiple containers**: Add sidecar containers for multi-pod services (e.g., immich-server + machine-learning)
- **Init containers**: For setup tasks (permissions, migrations, etc.)
- **Security contexts**: Always define at pod AND container level
- **reloader annotation**: Auto-restart pods when externalsecret changes
- **Strategy**: Use `Recreate` for single-replica stateful apps, `RollingUpdate` for multi-replica
- **PUID/PGID**: Common in media/download apps for file permissions
- **probes**: Define readiness and liveness probes for reliability
- **OIDC credentials** (oauth2 only): Generated by pulumi/stacks/applications from ApplicationDefinition
- **Postgres credentials**: Generated by repository update script (`mise run update` or `task update`)

### User-Managed Secrets (Manual 1Password Entry)
For custom secrets (encryption keys, API tokens):

1. Identify needed secrets from app documentation
2. Add entry to `externalsecret.yaml` with placeholder:
   ```yaml
   ENCRYPTION_KEY: "{{ .encryption_key }}"
   ```
3. Create corresponding 1Password item (format: `APP_NAME Secret Key`)
4. Add to `dataFrom.extract.key`
5. Verify item exported to ExternalSecret after reconciliation

Example 1Password item structure:
```
Title: FreshRSS Crypto Key
Fields:
  - crypto_password: [random generated value]
```

## Validation & Deployment Checklist

- [ ] **Container image identified and version pinned with digest** (e.g., `v1.0.0@sha256:abc123...`)
- [ ] Prefer GHCR > Quay > Docker Hub in image registry
- [ ] **Chart repository is OCIRepository** (modern Flux v2 approach)
- [ ] All YAML files created and in correct directory
- [ ] `${APP}` and `${NAMESPACE}` variables used consistently (anchors in ks.yaml)
- [ ] Components selected match application needs
- [ ] ks.yaml dependsOn includes all component dependencies (postgres, valkey, truenas-volumes, etc.)
- [ ] definition.yaml has correct authentik section (oauth2 XOR proxy)
- [ ] ExternalSecret keys match app environment variables
- [ ] 1Password items created for custom secrets
- [ ] kustomization.yaml references all resource files (including nfs.yaml if needed)
- [ ] helmrelease.yaml includes:
  - [ ] Security context (pod and container level)
  - [ ] Resource requests and limits
  - [ ] Readiness and liveness probes
  - [ ] reloader annotation if using ExternalSecrets
  - [ ] Correct deployment strategy (Recreate vs RollingUpdate)
  - [ ] PUID/PGID for media apps
  - [ ] Init containers if setup needed
  - [ ] driftDetection enabled
  - [ ] Remediation strategy (retry count, rollback)
- [ ] nfs.yaml created if using TrueNAS/NFS storage
- [ ] All variables (${PUID}, ${ROOT_DOMAIN}, etc.) are defined in ks.yaml postBuild or referenced correctly

### Local Validation

```bash
# Validate flux manifests
flux-local

# Check YAML syntax
yamllint kubernetes/apps/<CLUSTER>/<NAMESPACE>/<APP>/*.yaml

# Verify substitutions will work
kustomize build kubernetes/apps/<CLUSTER>/<NAMESPACE>/<APP> --load-restrictor=LoadRestrictionsNone
```

### Deployment

```bash
# Stage changes (git add)
git add kubernetes/apps/<CLUSTER>/<NAMESPACE>/<APP>/

# If secrets added, verify they're encrypted in git
# Watch reconciliation
kubectl logs -n flux-system deploy/kustomize-controller -f

# Verify pod brought up
kubectl get pods -n <NAMESPACE> -l app.kubernetes.io/name=<APP>

# Check logs
kubectl logs -n <NAMESPACE> -l app.kubernetes.io/name=<APP>
```

## Common Patterns by Application Type

### Web Application (with auth and storage)
Components: postgres, tailscale, volsync
Example: Vikunja, Tandoor, Immich
HelmRelease: Multi-container (app + machine-learning), securityContext, driftDetection

### Dashboard (no auth, light storage)
Components: tailscale, volsync
Example: Glance, Status page
HelmRelease: Simple container, Recreate strategy

### PVR/Media Service (heavy storage, unix permissions)
Components: tailscale, volsync + nfs.yaml (TrueNAS)
Example: Prowlarr, Sonarr, Radarr
HelmRelease: PUID/PGID env vars, securityContext, reloader annotation, Recreate strategy
Dependencies: truenas-volumes

### Backend Service (auth, DB, no storage)
Components: postgres, tailscale
Example: Authentik, Minio
HelmRelease: Standard setup

### Home Automation (storage, init containers, dependencies)
Components: volsync
Example: Home Assistant, Mosquitto
HelmRelease: Init containers for permission setup, pod restartPolicy, terminationGracePeriodSeconds
Dependencies: Other services (MQTT broker, etc.)

### Machine Learning / Cache-Heavy (multi-container, cache)
Components: postgres, valkey, tailscale, volsync
Example: Immich (ML + API), code-server with GPU
HelmRelease: Multiple containers, supplementalGroups, probes, high resource limits

## Advanced HelmRelease Patterns

### Multi-Container Services
When an app has multiple components (e.g., Immich: server + ML):
```yaml
controllers:
  app:
    containers:
      app:
        image:
          repository: ghcr.io/immich-app/immich-server
          tag: v2.6.1@sha256:...
      machine-learning:
        image:
          repository: ghcr.io/immich-app/immich-machine-learning
          tag: v2.6.1@sha256:...
```

### Init Containers for Setup Tasks
For tasks that must run before main container (permissions, migrations):
```yaml
initContainers:
  fix-permissions:
    image:
      repository: alpine
      tag: 3.23.3@sha256:...
    command:
      - sh
      - -c
      - chmod -R 0777 /config || true
    securityContext:
      runAsUser: 0
      runAsNonRoot: false
```

### Auto-Reload on Secret Changes
Use `reloader.stakater.com/auto: "true"` annotation to restart pods when ExternalSecrets change:
```yaml
annotations:
  reloader.stakater.com/auto: "true"
```

### Probes for Reliability
Always include readiness and liveness probes:
```yaml
probes:
  liveness:
    httpGet:
      path: /health
      port: http
    initialDelaySeconds: 30
    periodSeconds: 10
  readiness:
    httpGet:
      path: /ready
      port: http
    initialDelaySeconds: 5
    periodSeconds: 5
```

### For Media Apps: Unix Permissions
Configure PUID/PGID and security context:
```yaml
env:
  PUID: ${PUID}      # User ID (usually 568)
  PGID: ${PGID}      # Group ID (usually 568)
securityContext:
  runAsUser: 568
  runAsGroup: 568
  supplementalGroups: [0, 1, 44, 109, 303, 568, 10000]  # For file access
```

### Strategy Selection
- **Recreate**: Single-replica stateful apps (Immich, Home Assistant)
  - Pod is terminated and restarted (brief downtime)
  - Good for apps with file locks

- **RollingUpdate**: Multi-replica stateless apps (API servers)
  - Gradually replace pods (zero downtime)
  - Requires app to support multiple versions simultaneously

## How to Find Reference Deployments

Before creating a new deployment, **always find a similar existing app** to use as a reference.

### Finding by Category

**Web Apps with Postgres + OIDC**:
- Equestria: `immich`, `vikunja`, `tandoor`
- Stargate: `oxycloud`

**Dashboards / Simple Apps**:
- Equestria: `glance`
- Stargate: none yet

**PVR / Media Management**:
- Equestria: `radarr`, `sonarr`, `prowlarr`, `lidarr`

**Home Automation**:
- Stargate: `home-assistant`

**Downloads / Cache Apps**:
- Equestria: `sabnzbd`, `qbittorrent`, `autobrr`
- Stargate: `tdarr-node`

### Reference Checklist

When copying from a reference app:
1. Check if it has custom security context (PUID/PGID)
2. Look for init containers (permission fixes, migrations)
3. Verify if it has probes defined
4. Check for multi-container setup (sidecars)
5. Look at storage strategy (Volsync vs NFS)
6. Review HelmRelease retry/rollback strategy
7. Check what dependencies are needed

**Exact Copy Steps**:
1. Copy `helmrelease.yaml` from similar app
2. Update image registry/repository/tag
3. Update container port if different
4. Adjust resource requests/limits for app type
5. Update environment variables per app docs


**Pod stuck pending**: Check if postgres/volsync/valkey dependencies are reconciled first
**ExternalSecret errors**: Verify 1Password item exists and ClusterSecretStore can access it
**OIDC login fails**: Check that pulumi stack ran; verify client ID/secret in app config
**Auth proxy not working**: Ensure definition.yaml uses `proxy` mode and not `oauth2`
**Storage not appearing**:
  - For Volsync: Verify component included, capacity sufficient, access modes match
  - For NFS: Check server IP/share path, nfsvers, and CSI driver availability
**ImagePullBackOff**:
  - Verify image registry is correct (typo in ghcr.io vs docker.io vs quay.io)
  - Check if tag/digest exists: `skopeo list-tags docker://REGISTRY/IMAGE` or GHCR API
  - Verify registry credentials if using private images
  - Check pod events: `kubectl describe pod -n NAMESPACE POD_NAME`
**Image digest mismatches**: If using v1.0.0 but chart resolves to v1.0.1, check Helm chart source for image pinning strategy
**Pod stuck in Recreate strategy**: Check for PVC/storage binding issues, wait for volsync to ready state
**reloader not triggering restarts**: Ensure `reloader.stakater.com/auto: "true"` annotation is on controller, and ExternalSecret has matching annotations
**Permission denied on mounted storage**: For NFS, verify supplementalGroups includes storage owner GID; for media apps, verify PUID/PGID match storage ownership
**Probes failing (not ready/alive)**:
  - Verify endpoint paths match app configuration (/health, /ready, /alive, /ping)
  - Check initialDelaySeconds is high enough for app startup
  - Look at app logs: `kubectl logs -n NAMESPACE -l app.kubernetes.io/name=APP`
**Init container not running**: Verify securityContext runAsUser: 0 and runAsNonRoot: false for root-requiring setup tasks
**Multi-container pod stuck**: Check that all containers have correct securityContext and resource requests; one blocking container prevents others from starting
