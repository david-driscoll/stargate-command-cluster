# GitHub Copilot Guide for Stargate Command Cluster

Talos + Flux GitOps repo. Use `mise` for dev tooling, `task` for common operations, `sops (age)` for secrets, and Flux/Kustomize for deployments.

Key locations
- `kubernetes/` — manifests (apps grouped by namespace under `kubernetes/apps/*`).
- `kubernetes/flux/cluster/ks.yaml` — cluster Kustomization that injects `decryption: sops` and `postBuild` substitutions into child kustomizations.
- `kubernetes/components/common/cluster-secrets.sops.yaml` and `sops-age.sops.yaml` — shared sops secrets (root `age.key` is git-ignored).
- `bootstrap/helmfile.yaml` and `scripts/bootstrap-apps.sh` — bootstrap ordering and helmfile-based initial installs.

Quick developer workflow
- Install dev tools: `mise install` (see `.mise.toml` for pinned versions).
- Generate configuration: `task init` then `task configure`.
- Validate Kustomizations locally: `flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/cluster -v` (CI runs this on PRs: `.github/workflows/flux-local.yaml`).
- Force a reconcile: `task reconcile` (maps to Flux CLI in `Taskfile.yaml`).

Project-specific patterns (follow these exactly)
- Apps: add `kubernetes/apps/<NAMESPACE>/<APP>` with `ks.yaml` (Flux Kustomization), `kustomization.yaml`, and `helmrelease.yaml` when using Helm. Many apps use the `app-template` chart (see `kubernetes/components/repos/app-template/ocirepository.yaml`).
- Secrets: always add `*.sops.yaml` and keep `age.key` out of git. CI and bootstrap scripts expect `cluster-secrets.sops.yaml` and `shared-secrets.sops.yaml`.
- Flux child kustomizations rely on `postBuild` substitutions from `cluster-secrets` and `shared-secrets` — do not remove those without updating `kubernetes/flux/cluster/ks.yaml`.
- Programmatic updates: maintainers use C# scripts (`kubernetes/**/Update.cs`). Use `task update` or `dotnet run .mise/tasks/do-update.cs` to run updates so SOPS handling is consistent.

### Components reference — how apps consume them

- common
  - Purpose: shared namespaces, service-accounts, SOPS secret holders and cluster-wide variables (`CLUSTER_DOMAIN`, `INTERNAL_DOMAIN`, Tailscale IPs).
  - Usage: Ensure `kubernetes/components/common/cluster-secrets.sops.yaml` and `shared-secrets.sops.yaml` are present and encrypted. `kubernetes/flux/cluster/ks.yaml` injects them into child Kustomizations via `postBuild`.
  - Examples: `kubernetes/components/common/cluster-secrets.sops.yaml`, `kubernetes/components/common/Update.cs`.

- repos/app-template
  - Purpose: registers the `app-template` OCI repo used by the standardized HelmRelease pattern.
  - Usage: Helm-based apps use `chartRef.kind: OCIRepository name: app-template` in their `helmrelease.yaml`.
  - Examples: `kubernetes/components/repos/app-template/ocirepository.yaml`, `kubernetes/apps/observability/gatus/app/helmrelease.yaml`.

- ingress (internal / authenticated / external)
  - Purpose: provide reusable Ingress/Gateway annotations and HelmRelease ingress values for internal-only, authenticated (SSO), and public (external-dns) exposure.
  - Usage: Add the component to an app's `ks.yaml` (e.g. `- ../../../../components/ingress/internal`) and set HelmRelease `route` or `ingress` values to select `internal`/`authenticated`/`external`.
  - Examples: `kubernetes/components/ingress/internal/kustomization.yaml`, `kubernetes/components/ingress/authenticated/kustomization.yaml`, `kubernetes/apps/observability/grafana/ks.yaml`.

- gateway (internal / authenticated)
  - Purpose: Gateway API resources that act as parentRefs for HTTPRoute/Route blocks created by apps.
  - Usage: Apps that use Gateway API reference a parent `Gateway` (e.g., `parentRefs.name: internal, namespace: network`) in their HelmRelease `route` values.
  - Examples: `kubernetes/components/gateway/internal/kustomization.yaml`, `kubernetes/apps/observability/gatus/app/helmrelease.yaml` (route.parentRefs).

- tailscale
  - Purpose: Tailscale operator, idp integration, and tailscale-managed ingress bits.
  - Usage: Include `components/tailscale` for apps that should be reachable over Tailscale or use Tailscale nameserver.
  - Examples: `kubernetes/components/tailscale/kustomization.yaml`, `kubernetes/apps/sgc/idp/ks.yaml`.

- volsync (local / remote / backblaze)
  - Purpose: Templates and CRs for PVC replication and offsite backups (volsync + restic).
  - Usage: Apps that require replicated volumes include `components/volsync` (or `volsync/local` / `volsync/remote` / `volsync/backblaze`) and use the provided templates (PVC, ReplicationSource, ReplicationDestination, externalsecret). Many `Update.cs` scripts call these templates programmatically.
  - Examples: `kubernetes/components/volsync/local/replicationsource.yaml`, `kubernetes/components/volsync/pvc.yaml`, `kubernetes/apps/sgc/dns/adguard-home/Update.cs`.

- databases (postgres / mysql / mariadb)
  - Purpose: Components contain DB cluster templates (CRDs), database provisioning manifests (users, databases, backups) and recommended resource defaults used by apps.
  - Usage: Use `components/postgres`, `components/mysql`, or `components/mariadb` for cluster- or namespace-scoped DB provisioning. Store DB credentials in per-app `*.sops.yaml` secrets (e.g., `kubernetes/apps/database/postgres/secret.sops.yaml`) and reference those secrets from HelmRelease values. Prefer the component templates for creating databases/users rather than hand-rolling raw SQL resources.
  - Examples: `kubernetes/components/postgres/database.yaml`, `kubernetes/components/mysql/cluster.yaml`, `kubernetes/components/mariadb/database.yaml`, `kubernetes/apps/database/postgres/ks.yaml`.

- democratic-csi
  - Purpose: Storage runtime configuration and CRs for democratic-csi, exposing storage classes used by applications.
  - Usage: Install once as a cluster-scoped component (found under `components/common/democratic-csi.yaml`). Applications request the storage class by name in their PVCs or volsync templates.
  - Examples: `kubernetes/components/common/democratic-csi.yaml`, `kubernetes/apps/*/*/ks.yaml` references to storage classes.

- code (code-server)
  - Purpose: Developer-focused code-server HelmRelease using the `app-template` pattern to quickly provision per-namespace dev instances.
  - Usage: Use `components/code/code-server.yaml` as a reference HelmRelease (or include the component directly when you want a standardized code-server). Adjust host and ingress values to `${APP}-code.${CLUSTER_DOMAIN}` and set `persistence`/`service` ports to match your app's conventions.
  - Example: `kubernetes/components/code/code-server.yaml` shows how `APP`, `CLUSTER_DOMAIN`, `INGRESS class` and port anchors are used.

Programmatic updates and templates
- Purpose: C# `Update.cs` scripts under `kubernetes/apps/*/*/Update.cs` generate or synchronize derived resources from templates in `kubernetes/components/*/` and write out `.sops.yaml` secrets where needed.
- Behavior: The scripts read templates (e.g., `GetTemplate("kubernetes/components/volsync/local/replicationsource.yaml")`), generate resource YAML, write files, and (for `.sops.yaml`) re-encrypt using `sops -e -i` (see `kubernetes/components/common/Update.cs` for patterns).
- Guidance: Prefer running `task update` (or `dotnet run .mise/tasks/do-update.cs`) to regenerate files. Do not manually edit files that are generated by `Update.cs` except when intentionally modifying the source templates.

How apps consume components — checklist
1. Add the component to the app's `ks.yaml` or `kustomization.yaml`. Example: `- ../../../../components/ingress/internal`.
2. Match the component conventions in your HelmRelease values (e.g., set `route.internal.hostnames` or `ingress.internal.annotations`). See `kubernetes/apps/observability/gatus/app/helmrelease.yaml` for a pattern that declares `route.internal.parentRefs` and `hostnames`.
3. Add any app-specific encrypted secrets as `secret.sops.yaml` under the app directory and reference them in HelmRelease values.
4. If local templates/volsync/db resources are needed, use the component templates (PVC, replication CRs) or run the app's `Update.cs` if present.
5. Locally validate with `dotnet run .mise/tasks/do-update.cs` (if used) and `flux-local test` before opening a PR.

Integration points to watch
- 1Password / op://: Many environment and CI values use `op://` references (`.mise.toml` and some templates). Do not commit credentials — always use `*.sops.yaml` or 1Password integration.
- Renovate: `.github/renovate.json5` targets HelmCharts, helmfile, kustomize, and kubernetes manifests. Avoid large, overlapping changes to charts or component templates in the same PR as Renovate updates.
- CI: `.github/workflows/flux-local.yaml` runs `flux-local` tests and diffs. Keep PRs small — flux-local diffs are limited by size and CI posts the diff to PR comments.

Safety checklist for AI agents (must follow)
- NEVER commit unencrypted secrets or `age.key`.
- Prefer `Update.cs` scripts for any change that touches generated files or `.sops.yaml` files.
- Preserve `postBuild` substitutions in child kustomizations (`cluster-secrets`, `shared-secrets`) unless you intentionally change cluster injection behavior.
- Use `flux-local test` locally and rely on CI diffs before merging.
- For changes to ingress/gateway behavior, double-check Gateway parentRefs (`network` namespace) and Traefik/IngressClass annotations used by `components/ingress/*`.

References to inspect when modifying code
- `Taskfile.yaml`, `.taskfiles/*` — quick tasks and commands.
- `.mise.toml` — pinned toolchain, environment variables, and `task update` script configuration.
- `scripts/bootstrap-apps.sh`, `bootstrap/helmfile.yaml` — bootstrap order and CRD steps.
- `kubernetes/flux/cluster/ks.yaml` — cluster Kustomization and postBuild substitution behavior.
- `kubernetes/components/*/` — templates used by `Update.cs` scripts.
- Any `kubernetes/apps/*/*/Update.cs` — look for `GetTemplate()` usage and follow that pattern.

MCP servers (tools) agents should use

- mcp_context7_resolve-library-id + mcp_context7_get-library-docs
  - Use for authoritative library documentation and code examples (Pulumi providers, SDKs like `@1password/connect`, Unifi SDKs). Always call `resolve-library-id` first unless the caller supplies a Context7-compatible ID (`/org/project`).

- mcp_microsoft-doc_microsoft_docs_search + mcp_microsoft-doc_microsoft_code_sample_search + mcp_microsoft-doc_microsoft_docs_fetch
  - Use for Microsoft/Azure docs and official code samples. Prefer `code_sample_search` with `language` set when you need runnable snippets. For Azure-related generation or deployment plans, call the Azure best-practice tool (get_bestpractices) first as required by repo rules.

- mcp_duckduckgo_search + mcp_duckduckgo_fetch_content
  - General web search and page fetching for vendor docs, blog posts, or quick troubleshooting. Use when library-specific or vendor-specific MCPs do not return sufficient context.

- mcp_github_pull_request_read, mcp_github_list_discussions, mcp_github_search_issues, mcp_github_list_projects
  - Use to inspect PRs, changed files, discussions and project context in the target repo. Prefer `mcp_github_pull_request_read(method: get_files|get_diff|get)` to obtain exact diffs and changed file lists before editing code or proposing PR changes.

- mcp_flux-operator_get_flux_instance
  - Use to get a report of Flux controllers/CRDs and their status when investigating GitOps/Flux issues.

- mcp_kubernetes_namespaces_list + mcp_kubernetes_resources_create_or_update/get/delete
  - Use for cluster-aware workflows (listing namespaces, creating or updating resources). Only call resource-changing tools when the user explicitly asks to modify a cluster; otherwise prefer read-only queries and local validation (`flux-local`).

- mcp_pulumi_neo-task-launcher
  - Use to launch Pulumi Neo tasks (automated infra work) when the user requests an automated Pulumi operation. Always supply clear context and expected artifacts.

- activate_* tools (activate_kubernetes_resource_management, activate_pulumi_deployment_tools, activate_flux_reconciliation_tools, etc.)
  - Call the appropriate `activate_` tool before using specialized domain tools; they ensure the agent has access to the right capabilities and permissions.

Quick rules
- Prefer specialized MCP tools (Context7, Microsoft-doc, GitHub, Flux) over a generic web search when authoritative docs are available.
- Resolve library IDs with `mcp_context7_resolve-library-id` before fetching library docs.
- Never call cluster-modifying tools or Pulumi/Pulumi-Neo tasks without explicit user consent.
