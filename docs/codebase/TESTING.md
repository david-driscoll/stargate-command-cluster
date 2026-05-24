# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands

This is a GitOps infrastructure repository. Testing validates YAML manifests and Flux kustomization rendering — there are no application unit or integration tests.

- **Primary validation framework:** `flux-local` v8.2.0 — renders and validates all Flux Kustomizations and HelmReleases
- **Schema validation:** `kubeconform` v0.7.0 — validates Kubernetes resource schemas
- **CI runner:** GitHub Actions (ubuntu-latest)

```bash
# Validate all Flux kustomizations and HelmReleases (renders Helm charts locally)
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/ --verbosity 1

# Diff HelmReleases between branches (used in CI for PR comments)
flux-local diff helmrelease \
  --unified 6 \
  --path ./kubernetes/flux/cluster \
  --path-orig ./default/kubernetes/flux/cluster \
  --all-namespaces

# Validate Kubernetes resource schemas
kubeconform -strict -ignore-missing-schemas kubernetes/

# Run local update + validation (C# + flux-local)
dotnet run .mise/tasks/do-update.cs
flux-local test --enable-helm --all-namespaces --path ./kubernetes/flux/ --verbosity 1
```

### 2) Test Layout

- No test files in traditional sense (no `*.test.yaml` or `spec/` directory)
- Validation config implicit in CI pipeline definition
- CI validates on every PR touching `kubernetes/**`
- Local validation via `task` commands or `flux-local` CLI directly

### 3) Test Scope Matrix

| Scope | Covered? | Typical target | Notes |
|-------|----------|----------------|-------|
| Unit | No | N/A | Infrastructure repo — no application code to unit test |
| Manifest validation | Yes | All YAML under `kubernetes/` | kubeconform schema check |
| Flux rendering | Yes | All Kustomizations + HelmReleases | flux-local renders all Helm charts |
| Diff preview | Yes | HelmRelease + Kustomization | flux-local diff posts to PR comments |
| Integration / E2E | No | Live cluster behavior | No automated live cluster tests |
| Talos config validation | No | `talos/talconfig.yaml` | [ASK USER] — talhelper validate not seen in CI |

### 4) Mocking and Isolation Strategy

- Not applicable — no unit tests. `flux-local` renders Helm charts in a sandboxed environment without a live cluster.
- SOPS-encrypted secrets are **not decrypted** during CI — `flux-local` skips ExternalSecret rendering.
- Helm chart rendering may fail if chart version is unavailable (OCI/Helm registry must be reachable from CI).

### 5) Coverage and Quality Signals

- **Coverage:** Not measured (infrastructure repo)
- **Quality signal:** Successful `flux-local test` run = all kustomizations and HelmReleases render without error
- **Known gap:** No live cluster smoke tests; a passing CI does not guarantee runtime correctness (e.g., image pull failures, wrong env vars, storage misconfiguration)
- **Flaky area:** Helm chart pulls in CI can time out if upstream chart registries are slow

### 6) Evidence

- `.github/workflows/flux-local.yaml` — full CI test and diff pipeline
- `.mise.toml` — `flux-local` version pin (`8.2.0`) and `kubeconform` version (`0.7.0`)
- `.mise.toml` `[tasks.update]` — local update + test command
