# Equestria Cluster - Talos Kubernetes GitOps Repository

This is a production Talos Kubernetes cluster using GitOps with Flux, managed via mise for development tooling and Task for build automation.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Essential Tool Installation

-   **CRITICAL**: Install mise first, then all required tools:

    ```bash
    # Install mise (may fail due to network restrictions - see troubleshooting)
    curl https://mise.run | sh
    # OR try alternative: curl -L https://install.mise.jdx.dev/install.sh | sh

    # Trust the config and install all tools - takes 10-15 minutes. NEVER CANCEL.
    mise trust
    pip install pipx  # Required for makejinja
    mise install  # TIMEOUT: 20+ minutes
    ```

-   **If mise installation fails due to network restrictions**: Document this limitation and proceed with available tools for validation only.

### Bootstrap and Build Process

-   **NEVER attempt full bootstrap without physical Talos cluster hardware**
-   Configuration generation (without deployment):
    ```bash
    task init        # Generate config files from templates
    task configure   # Template out Kubernetes and Talos configs - takes 2-5 minutes
    ```
-   **Cluster Bootstrap (requires physical hardware)**:
    ```bash
    task bootstrap:talos  # Install Talos - takes 15-30 minutes. NEVER CANCEL.
    task bootstrap:apps   # Bootstrap applications - takes 10-20 minutes. NEVER CANCEL.
    ```

### Validation and Testing

-   **Primary validation method** (works without cluster):
    ```bash
    # Flux manifest validation - takes 79 seconds. NEVER CANCEL. Set timeout to 120+ seconds.
    flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v
    # OR use Docker if flux-local not installed:
    docker run --rm -v "$(pwd):/workspace" -w /workspace ghcr.io/allenporter/flux-local:v7.8.0 test --enable-helm --all-namespaces --path /workspace/kubernetes/flux/cluster -v
    ```
-   **Additional validation tools**:
    ```bash
    # Basic Kubernetes manifest validation (install kubeconform first)
    kubeconform -summary -verbose kubernetes/apps/*/kustomization.yaml
    # Check dependencies in bootstrap script
    bash scripts/bootstrap-apps.sh  # Will fail gracefully, showing missing tools
    ```

### Cluster Operations (requires running cluster)

-   **Post-deployment verification**:
    ```bash
    cilium status                    # Check Cilium CNI status
    flux check                       # Verify Flux system health
    flux get sources git flux-system
    flux get ks -A                   # Check Kustomizations
    flux get hr -A                   # Check HelmReleases
    kubectl get pods --all-namespaces --watch  # Monitor rollout
    ```
-   **Force Flux sync**:
    ```bash
    task reconcile  # Force pull from Git repository
    ```

## Validation Scenarios

-   **ALWAYS run flux-local testing** after making changes to kubernetes/ directory
-   **ALWAYS test** that bootstrap script dependency validation works: `bash scripts/bootstrap-apps.sh`
-   **After cluster changes**: Run complete verification workflow including cilium status and flux checks
-   **Manual testing**: If cluster is available, perform end-to-end GitOps workflow testing

## Common Troubleshooting

### Installation Issues

-   **"Missing required deps"**: Normal when tools not installed - shows helmfile, sops, talhelper, etc.
-   **Network restrictions blocking mise**: Use available tools (kubectl, yq, docker) for validation only
-   **Python compilation errors**: Run `mise settings python.compile=0` then retry
-   **GitHub token issues**: Unset `GITHUB_TOKEN` environment variable and retry

### Expected Timing and Failures

-   **mise install**: 10-20 minutes depending on network. NEVER CANCEL.
-   **flux-local test**: 79 seconds. NEVER CANCEL. Set timeout to 120+ seconds minimum.
-   **task bootstrap:talos**: 15-30 minutes. NEVER CANCEL.
-   **task bootstrap:apps**: 10-20 minutes. NEVER CANCEL.
-   **Cluster rollout**: 10+ minutes normal. Do not interrupt.

## Infrastructure Requirements

### Physical Requirements

-   **This is a running production cluster config**, not a template
-   **Requires specific Talos hardware**: Multiple nodes with exact network configurations
-   **Storage**: Longhorn with specific disk configurations per node
-   **Network**: Static IPs, specific hardware addresses defined in talconfig.yaml

### External Dependencies

-   **OnePassword Connect**: Used for secret management (required for SOPS)
-   **Cloudflare account**: Required for DNS and tunnel management
-   **Talos Image Factory**: For custom system extensions

### Secret Management

-   **SOPS with Age**: All secrets encrypted with age.key file
-   **OnePassword integration**: Credentials managed via op:// references
-   **NEVER commit unencrypted secrets**: All .sops.yaml files must be encrypted

## Repository Structure

### Key Directories

-   `/.taskfiles/`: Build automation (bootstrap, k8s, talos)
-   `/kubernetes/flux/cluster/`: Main Flux configuration entry point (ks.yaml defines cluster-meta and cluster-apps)
-   `/kubernetes/apps/`: Application deployments organized by namespace (kustomization.yaml + ks.yaml per namespace)
-   `/kubernetes/components/`: Shared Kustomize components (common/, ingress/, postgres/, etc.)
-   `/talos/`: Talos Linux configuration and patches
-   `/scripts/`: Shell scripts, primarily bootstrap-apps.sh
-   `/bootstrap/`: Helmfile-based initial cluster setup

### Configuration Files

-   `.mise.toml`: Development tool versions and environment setup
-   `Taskfile.yaml`: Main task runner with includes
-   `talos/talconfig.yaml`: Complete Talos cluster configuration
-   `talos/talenv.yaml`: Version pinning for Talos and Kubernetes

## Code Organization Patterns

### Kubernetes Namespace Structure

Each namespace directory follows this pattern:

-   `kustomization.yaml`: Lists all Flux Kustomizations (ks.yaml files) and shared components
-   `secret-store.yaml`: OnePassword External Secrets integration for the namespace
-   Individual app directories containing Flux `ks.yaml` files

### Flux Kustomization (ks.yaml) Pattern

All applications use standardized ks.yaml files with:

-   `&app` and `&namespace` YAML anchors for DRY configuration
-   Consistent dependency declarations (`dependsOn`)
-   Component references to shared functionality (`../../../../components/ingress/internal`, etc.)
-   PostBuild substitution variables (`APP`, `NAMESPACE`, plus app-specific vars)

### App onboarding: vendor Helm charts (e.g., Homechart)

When adding a new app that ships its own Helm chart, follow this pattern:

1) App folder layout under the target namespace
- `kubernetes/apps/<NAMESPACE>/<CATEGORY>/<APP>/`
    - `kustomization.yaml` that references:
        - `helmrelease.yaml` (can include an embedded HelmRepository followed by the HelmRelease in a single multi-doc file)
        - `externalsecret.yaml` (if the app needs DB or other credentials)
        - `definition.yaml` (ApplicationDefinition + optional Gatus checks)

2) Helm repository and release
- Prefer embedding a small, app-specific HelmRepository in the same file as the HelmRelease (two YAML documents). Example fields to set in the HelmRelease:
    - `chart.spec.chart`: upstream chart name
    - `chart.spec.sourceRef`: references the embedded HelmRepository by name
    - `values.ingress`: set `ingressClassName` and host to `${APP}.${ROOT_DOMAIN}`
    - If the chart includes its own Postgres, disable it via `values.postgresql.enabled: false` and use the shared cluster Postgres instead

3) Database provisioning via component
- If the app uses Postgres, include `../../../../components/postgres` in the app Kustomization’s `components` list and add a `dependsOn` entry for `postgres` in the `database` namespace. The Database (CNPG) object is created by the component; no app-local DB manifests are required.

4) ExternalSecret pattern for DB credentials
- Use a `ClusterSecretStore` named `database` to extract the per-app key `'${APP}-postgres'` and rewrite keys with a `postgres_` prefix.
- Emit the exact env vars the chart expects using `target.template.data`. For example, for Homechart:
    - `HOMECHART_postgresql_hostname`, `HOMECHART_postgresql_database`, `HOMECHART_postgresql_username`, `HOMECHART_postgresql_password`, `HOMECHART_postgresql_port` (5432)
- Avoid unsupported fields (e.g., `decode`) in ExternalSecret; use `rewrite` + template variables instead.

5) Ingress and base URL alignment
- If the app needs a base URL env (e.g., `HOMECHART_APP_BASEURL`), set it to `https://${APP}.${ROOT_DOMAIN}` and ensure the ingress host matches the same value.

6) Namespace ks.yaml wiring
- Add a new Kustomization entry in `kubernetes/apps/<NAMESPACE>/<CATEGORY>/ks.yaml` pointing to the app folder, with:
    - `components`: at minimum `../../../../components/ingress/internal` and, if applicable, `../../../../components/postgres`
    - `dependsOn`: reference `postgres` in `database` namespace when using the Postgres component
    - `postBuild.substitute`: include `APP` and `NAMESPACE` (and any app-specific substitutions)

7) Optional components
- Add `../../../../components/tailscale` to expose the app over Tailscale if desired.
- Add `../../../../components/volsync` when persistent data should be replicated/backed up; set `VOLSYNC_*` substitutions as needed.

8) ApplicationDefinition and health
- Provide a `definition.yaml` for UI metadata (name, icon, URL, access policy) and a simple Gatus check for the app URL.

9) Validate before pushing
- Run flux-local testing (or the Docker-based variant) to validate manifests render and Helm charts resolve:
    - `docker run --rm -v "$(pwd):/workspace" -w /workspace ghcr.io/allenporter/flux-local:v7.8.0 test --enable-helm --all-namespaces --path /workspace/kubernetes/flux/cluster -v`

Notes
- It’s acceptable to register shared Helm repositories under `kubernetes/flux/meta/repos/` when used by multiple apps. For one-off vendor charts, embedding the HelmRepository with the app’s HelmRelease keeps the scope local and avoids clutter under `flux/meta`.

## Automation and Updates

-   **Renovate**: Automated dependency updates via .github/renovate.json5
-   **GitHub Actions**: flux-local testing on all kubernetes/ changes
-   **C# Update Scripts**: Custom update automation in `kubernetes/*/Update.cs` files
    -   Run all updates: `task update` or `dotnet run .mise/tasks/do-update.cs`
    -   Update scripts handle SOPS encryption/decryption automatically
    -   Example: Tailscale nameserver IP sync from live cluster to secrets
-   **Husky pre-commit**: Runs dotnet husky tasks automatically
-   **mise tooling**: All development dependencies managed via `.mise.toml`

## Critical Reminders

-   **NEVER CANCEL long-running builds or deploys** - may take 45+ minutes
-   **ALWAYS validate with flux-local** before pushing kubernetes/ changes
-   **VERIFY .sops.yaml encryption** before committing secrets
-   **SET EXPLICIT TIMEOUTS** (60+ minutes) for bootstrap commands
-   **This is a production config** - exercise extreme caution with changes
