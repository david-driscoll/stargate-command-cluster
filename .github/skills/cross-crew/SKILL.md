---
name: "cross-crew"
description: "Coordinating work across multiple Crew instances — discovery, delegation, and disambiguation when the user says 'crew' (the product) vs casual English 'group of agents'."
domain: "orchestration"
confidence: "medium"
source: "manual"
triggers:
  - "spawn N crews"
  - "spawn a crew"
  - "another crew"
  - "two crews of"
  - "second crew"
  - "fan out to crews"
  - "delegate to a crew"
  - "set up crews for"
  - "create a crew to review"
  - "ask the other crew"
tools:
  - name: "crew-discover"
    description: "List known crews and their capabilities"
    when: "When you need to find which crew can handle a task"
  - name: "crew-delegate"
    description: "Create work in another crew's repository"
    when: "When a task belongs to another crew's domain"
---

## Context

> **Read this FIRST any time the user says "crew" as a thing to spawn, delegate to, address, or fan out to** — e.g., *"spawn two crews of designers and devs"*, *"ask the other crew"*, *"delegate to a crew"*. In Crew-PRODUCT vocabulary, "crew" is a **peer** (an independent installation with its own `.crew/`, `team.md`, MCP server, and agents) — NOT a generic English synonym for "team" or "group". Do not fan out raw `task` agents inside your own coordinator context when the user means "another crew". Use the discovery and communication patterns below (and the companion `cross-crew-communication` skill for the actual protocols).

When an organization runs multiple Crew instances (e.g., platform-crew, frontend-crew, data-crew), those crews need to discover each other, share context, and hand off work across repository boundaries. This skill teaches agents how to coordinate across crews without creating tight coupling.

> **Companion skill — for protocol details:** `cross-crew-communication/SKILL.md` covers the four communication patterns (synchronous CLI, read-only knowledge query, git-based async, and GitHub-issue-based delegation) once a peer crew is discovered via the registry below. This skill answers "who?" — the companion answers "how?".

Cross-crew orchestration applies when:
- A task requires capabilities owned by another crew
- An architectural decision affects multiple crews
- A feature spans multiple repositories with different crews
- A crew needs to request infrastructure, tooling, or support from another crew

## Disambiguation: "crew" vs ad-hoc agents

When the user uses the word **"crew" / "crews"** or asks to **"spawn a team"**, the coordinator MUST treat it as a literal reference to a Crew install (a `.crew/` directory with its own roster, casting, and coordinator) — NOT as a casual synonym for "a group of sub-agents".

### Default behaviour (apply unless explicitly told otherwise)

| User says | Coordinator does |
|---|---|
| *"spawn two crews of X and Y"* / *"set up crews for X, Y, Z"* | Bootstrap N **real** Crew installs — separate folder + `git init` + `crew init` per crew — then use the cross-crew patterns below (`.crew/manifest.json`, `crew registry add`, `crew delegate`) and the protocols in the `cross-crew-communication` skill |
| *"ask the other crew about X"* / *"delegate to the data crew"* | Discover the peer via `crew registry list` (or by reading a known `.crew/manifest.json`), then use `cross-crew-communication` Pattern 0 / 1 / 2 / 3 — never re-implement the protocol with `task` |
| *"spawn a few agents to do X"* / *"have some agents review X"* / *"in parallel, get sub-agents to..."* | Ad-hoc `task` fan-out is fine — no `.crew/` bootstrap needed. This is the only path where raw `task` is appropriate when the user mentioned a multi-agent activity |

### Ambiguous? `ask_user`, never silently downgrade

If the request **could** be either interpretation AND bootstrapping real crews is non-trivial (more than one or two `crew init` runs), you MUST use the `ask_user` tool with a 2-choice prompt before proceeding:

```
question: "Should I create separate Crew installs or just dispatch ad-hoc agents?"
choices:
  - "Real crews — separate .crew/ per crew (heavier, persistent, can be re-engaged later)"
  - "Ad-hoc agents — one-shot `task` dispatch (lighter, ephemeral, no .crew/ created)"
```

The cost of asking is one `ask_user`. The cost of getting it wrong is the user has to redo the work. **Never silently pick the cheaper option just because it feels disproportionate for the task size — surface the trade-off and let the user pick.**

### Anti-patterns (every one of these is a real failure mode observed in production)

- **Calling `task` sub-agents "crew-alpha" / "crew-beta"** and treating them as crews. Naming something a crew doesn't make it one — a crew has its own `.crew/`, `team.md`, MCP server, and coordinator. If those aren't there, it's not a crew.
- **Matching a prior session's ad-hoc pattern without re-checking current intent.** If you see existing `reviews/crew-alpha/` folders from a previous run, that's a hint, NOT a contract — the user may have meant something different this time. Re-evaluate from scratch.
- **Silently choosing the cheaper interpretation because "bootstrapping two real crews for a 30-line app feels disproportionate".** That's a judgment call for the USER to make, not the coordinator. Use `ask_user`.
- **Loading the `cross-crew` skill, reading it, then doing `task` fan-out anyway** because the eager-execution / parallel-fan-out doctrine pulled you back. The disambiguation rule on this page OVERRIDES the generic fan-out doctrine when "crew" was the trigger.

## Patterns

### Discovery via Manifest
Each crew publishes a `.crew/manifest.json` declaring its name, capabilities, and contact information. Crews discover each other through two mechanisms:

1. **`.crew/crew-registry.json`** — **discovery-only.** Peer crews are findable via `crew discover` and addressable via `crew delegate`, but their skills/decisions/wisdom are NOT loaded into your coordinator. Manage with `crew registry add/list/remove`.
2. **`.crew/upstream.json`** — **discovery + inheritance.** Crews listed here are also discoverable, AND your coordinator inherits their skills/decisions/wisdom/routing at session start. Manage with `crew upstream add/list/remove/sync`.

Both forms read the peer's manifest via the same code path. The `path` field is the **repository root** (e.g. `../friend-repo`), and Crew appends `.crew/manifest.json` internally. Pointing at the `.crew/` directory works too — Crew accepts both forms (`readManifest` strips a trailing `.crew` if present).

```json
{
  "name": "platform-crew",
  "version": "1.0.0",
  "description": "Platform infrastructure team",
  "capabilities": ["kubernetes", "helm", "monitoring", "ci-cd"],
  "contact": {
    "repo": "org/platform",
    "labels": ["crew:platform"]
  },
  "accepts": ["issues", "prs"],
  "skills": ["helm-developer", "operator-developer", "pipeline-engineer"]
}
```

### Context Sharing
When delegating work, share only what the target crew needs:
- **Capability list**: What this crew can do (from manifest)
- **Relevant decisions**: Only decisions that affect the target crew
- **Handoff context**: A concise description of why this work is being delegated

Do NOT share:
- Internal team state (casting history, session logs)
- Full decision archives (send only relevant excerpts)
- Authentication credentials or secrets

### Work Handoff Protocol
1. **Check manifest**: Verify the target crew accepts the work type (issues, PRs)
2. **Create issue**: Use `gh issue create` in the target repo with:
   - Title: `[cross-crew] <description>`
   - Label: `crew:cross-crew` (or the crew's configured label)
   - Body: Context, acceptance criteria, and link back to originating issue
3. **Track**: Record the cross-crew issue URL in the originating crew's orchestration log
4. **Poll**: Periodically check if the delegated issue is closed/completed

### Feedback Loop
Track delegated work completion:
- Poll target issue status via `gh issue view`
- Update originating issue with status changes
- Close the feedback loop when delegated work merges

## Examples

### Registering a peer crew (no inheritance)
```bash
# Friend's repo is checked out at ../friend-platform/
crew registry add platform-crew ../friend-platform

# Verify
crew registry list
crew discover
```

### Discovering crews
```bash
# List all crews discoverable from registry + upstreams
crew discover

# Output:
#   platform-crew  →  org/platform  (kubernetes, helm, monitoring)
#   frontend-crew  →  org/frontend  (react, nextjs, storybook)
#   data-crew      →  org/data      (spark, airflow, dbt)
```

### Delegating work
```bash
# Delegate a task to the platform crew
crew delegate platform-crew "Add Prometheus metrics endpoint for the auth service"

# Creates issue in org/platform with cross-crew label and context
```

### Manifest in crew.config.ts
```typescript
export default defineCrew({
  manifest: {
    name: 'platform-crew',
    capabilities: ['kubernetes', 'helm'],
    contact: { repo: 'org/platform', labels: ['crew:platform'] },
    accepts: ['issues', 'prs'],
    skills: ['helm-developer', 'operator-developer'],
  },
});
```

## Anti-Patterns
- **Direct file writes across repos** — Never modify another crew's `.crew/` directory. Use issues and PRs as the communication protocol.
- **Tight coupling** — Don't depend on another crew's internal structure. Use the manifest as the public API contract.
- **Unbounded delegation** — Always include acceptance criteria and a timeout. Don't create open-ended requests.
- **Skipping discovery** — Don't hardcode crew locations. Use manifests and the discovery protocol.
- **Sharing secrets** — Never include credentials, tokens, or internal URLs in cross-crew issues.
- **Circular delegation** — Track delegation chains. If crew A delegates to B which delegates back to A, something is wrong.
