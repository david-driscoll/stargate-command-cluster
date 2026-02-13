# Talos Kubernetes GitOps Repository

This is a production Talos Kubernetes cluster using GitOps with Flux, managed via mise for development tooling and Task for build automation.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

# Tools

- Mise is used to ensure versions for the repository are managed centrally.
  https://mise.jdx.dev/walkthrough.html
- Task is used for some of the management tasks in the cluster
  https://github.com/go-task/task

# Validation and Testing

- `mise run update`: Used to run local validation
- **ALWAYS run flux-local testing** after making changes to kubernetes/ directory

# Infrastructure Requirements

# Key Directories

- `/.taskfiles/`: Build automation (bootstrap, k8s, talos)
- `/kubernetes/flux/cluster/`: Main Flux configuration entry point (ks.yaml defines cluster-meta and cluster-apps)
- `/kubernetes/apps/`: Application deployments organized by namespace (kustomization.yaml + ks.yaml per namespace)
- `/kubernetes/components/`: Shared Kustomize components (common/, ingress/, postgres/, etc.)
- `/talos/`: Talos Linux configuration and patches
- `/scripts/`: Shell scripts, primarily bootstrap-apps.sh
- `/bootstrap/`: Helmfile-based initial cluster setup

# Configuration Files

- `.mise.toml`: Development tool versions and environment setup
- `Taskfile.yaml`: Main task runner with includes
- `talos/talconfig.yaml`: Complete Talos cluster configuration
- `talos/talenv.yaml`: Version pinning for Talos and Kubernetes

# Code Organization Patterns

# Kubernetes Namespace Structure

Each namespace directory follows this pattern:

- `kustomization.yaml`: Lists all Flux Kustomizations (ks.yaml files) and shared components
- Individual app directories containing Flux `ks.yaml` files

# Flux Kustomization (ks.yaml) Pattern

All applications use standardized ks.yaml files with:

- `&app` and `&namespace` YAML anchors for DRY configuration
- Consistent dependency declarations (`dependsOn`)
- Component references to shared functionality (`../../../../components/ingress/internal`, etc.)
- PostBuild substitution variables (`APP`, `NAMESPACE`, plus app-specific vars)

# Automation and Updates

- **Renovate**: Automated dependency updates via .github/renovate.json5
- **GitHub Actions**: flux-local testing on all kubernetes/ changes
- **C# Update Scripts**: Custom update automation in `kubernetes/*/Update.cs` files
    - Run all updates: `task update` or `dotnet run .mise/tasks/do-update.cs`
    - Update scripts handle SOPS encryption/decryption automatically
    - Example: Tailscale nameserver IP sync from live cluster to secrets
- **Husky pre-commit**: Runs dotnet husky tasks automatically
- **mise tooling**: All development dependencies managed via `.mise.toml`

# Critical Reminders

- **NEVER CANCEL long-running builds or deploys** - may take 45+ minutes
- **ALWAYS validate with flux-local** before pushing kubernetes/ changes
- **VERIFY .sops.yaml encryption** before committing secrets
- **SET EXPLICIT TIMEOUTS** (60+ minutes) for bootstrap commands
- **This is a production config** - exercise extreme caution with changes

# Application definitions

This system supports a custom kubernetes resource as defined by kubernetes/apps/observability/crds/application-crd.yaml

This application definition is discoverd by pulumi in this stack config https://github.com/david-driscoll/home-operations/blob/main/stacks/home.

This stack config will create authentik applications for each one.
In addition to the application, if defined it will support the oauth and proxy configurations.
Examples are https://github.com/david-driscoll/stargate-command-cluster/blob/main/kubernetes/apps/sgc/home/home-assistant/definition.yaml or https://github.com/david-driscoll/stargate-command-cluster/blob/main/kubernetes/apps/network/traefik/definition.yaml

In addition oauth credentials will generate secrets as seen with this definition https://github.com/david-driscoll/home-operations/blob/main/stacks/applications/kubernetes.ts

# MCP servers (tools) agents should use

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

- activate\_\* tools (activate_kubernetes_resource_management, activate_pulumi_deployment_tools, activate_flux_reconciliation_tools, etc.)
    - Call the appropriate `activate_` tool before using specialized domain tools; they ensure the agent has access to the right capabilities and permissions.

# Quick rules

- Prefer specialized MCP tools (Context7, Microsoft-doc, GitHub, Flux) over a generic web search when authoritative docs are available.
- Resolve library IDs with `mcp_context7_resolve-library-id` before fetching library docs.
- Never call cluster-modifying tools or Pulumi/Pulumi-Neo tasks without explicit user consent.
