---
name: "cross-crew-communication"
description: "Protocol for sending queries, delegating tasks, and sharing context between independent Crew instances across different repositories"
domain: "multi-repo coordination"
confidence: "medium"
source: "Ported from tamirdresher/crew-skills (plugins/cross-crew-communication). Companion to the registry-aware cross-crew skill — this one teaches the actual communication protocols once a peer is discovered. Pattern 0 (synchronous CLI) is the only end-to-end-validated pattern; Patterns 1, 2, 3 are documented from design but require live validation against your own setup before relying on them in production. See the Validation Status section at the bottom of this skill."
---

## Context

When multiple repositories each have their own Crew (AI team), they need to exchange information: knowledge queries, PR reviews, task delegation, and dependency analysis. Each crew has its own agents, MCP tools, and issue tracker — there is no shared runtime.

> **Companion skill — read first:** `cross-crew/SKILL.md` covers **discovery** of peer crews via `crew registry add/list/remove`. This skill picks up after a peer is known and covers the **communication protocols** themselves — the four numbered patterns below: Pattern 0 (synchronous CLI), Pattern 1 (read-only knowledge query), Pattern 2 (git-based async), and Pattern 3 (GitHub-issue-based delegation). A separate non-numbered appendix (Cross-Repo Dependency Scan) is provided as a related analysis tool, not a communication pattern. The two skills are designed to be used together.

**When this skill applies:**
- A crew agent needs information from another crew-enabled repo
- A task needs to be delegated to another crew
- Cross-repo dependency analysis is needed
- PR review requests span repo boundaries

**Key constraint:** Each crew has its own runtime, MCP tools, and issue tracker. Cross-crew communication can be **synchronous** (via CLI session targeting the other repo) or **asynchronous** (file-based or issue-based). The coordinator decides which approach fits.

---

## Patterns

### Decision Tree: Choosing the Right Pattern

```
Is the target repo cloned locally?
├─ NO → Use Pattern 3 (Issue-Based) or Pattern 2 (Git-Based Async)
└─ YES
    ├─ Is this a quick query / knowledge lookup?
    │   └─ YES → Use Pattern 0 (Synchronous CLI) — fastest
    ├─ Does the work need to persist as artifacts?
    │   └─ YES → Use Pattern 2 (Git-Based Async)
    ├─ Is it a long-running analysis or multi-cycle task?
    │   └─ YES → Use Pattern 2 (Git-Based Async)
    └─ Is the target crew's Ralph running?
        ├─ YES → Pattern 2 or 3 (async processing available)
        └─ NO → Pattern 0 (Synchronous CLI) or Pattern 1 (Read-Only)
```

---

## Universal rule: every `copilot` spawn into a peer crew MUST pass `--agent crew`

The `copilot` CLI accepts `--agent <name>` to select a custom agent (see `copilot --help`). Crew installs ship `.github/agents/crew.agent.md`, which is loaded **only when `--agent crew` is specified**. Without it the spawned session runs as a generic Copilot CLI session that does NOT load the peer's `team.md`, routing, MCP tools, casting, or coordinator behaviour — so you get an off-the-shelf model answering, not the peer's Crew. **Every command example in this skill that spawns `copilot` into a peer repo includes `--agent crew`; do not strip it.**

This rule also applies anywhere else you spawn `copilot` into a Crew-initialised repo (not just cross-crew protocols) — e.g., `crew init`'s post-init tip and any automation that invokes the CLI on a crewified folder. The only case where you may omit `--agent` is when resuming an existing session (`copilot --resume <sessionId>`) — the resumed session preserves its original agent context.

---

### Pattern 0: Synchronous CLI Session (Fastest for Interactive Queries)

For quick knowledge queries, decision lookups, or short analyses — spawn a Copilot CLI session with the working directory set to the target crew's repo. This lets you send a prompt and get a response within the same session, using the target repo's full context.

This is the same technique used by `ralph-watch.ps1`: write the prompt to a temp file, then invoke the CLI with that file as input. The key insight is that setting the working directory to the target repo gives the CLI session access to that crew's `.crew/` metadata, codebase, and conventions.

**Protocol:**
1. Write prompt to a temp file (avoids argument-splitting issues, as learned in `ralph-watch.ps1`)
2. Read the file into a string and invoke `copilot -p <text>` with `-C <directory>` set to the target repo (`-p` takes prompt text, NOT a file path) AND `--agent crew` so the spawned session uses the peer crew's coordinator (without `--agent` you get a generic Copilot CLI session that doesn't load the peer's `team.md`, MCP tools, or skills)
3. Receive response in the same session

**Invocation:**
```powershell
# Spawn a Copilot CLI session targeting another crew's repo
$targetRepo = "C:\repos\platform-crew-repo"
$promptFile = New-TemporaryFile
@"
You are working in a Crew-enabled repository.
Read .crew/team.md and .crew/decisions.md first.

[CROSS-CREW REQUEST]
From: research-crew
Request Type: knowledge_query
Query: What is the current architecture of the platform? What services does it expose?
Response Format: Brief structured summary
"@ | Out-File $promptFile -Encoding utf8

# Option A: copilot with prompt file (read file into string; -p takes text, not a path)
# --agent crew is REQUIRED: the target is another Crew install, so the spawned
# session must use that crew's coordinator (not a generic Copilot CLI session).
copilot -C $targetRepo --agent crew -p (Get-Content $promptFile -Raw) --allow-all-tools

# Option B: Start-Process for non-blocking (ralph-watch.ps1 style)
Start-Process pwsh -ArgumentList "-NoProfile -Command `"copilot -C '$targetRepo' --agent crew -p (Get-Content '$promptFile' -Raw) --allow-all-tools`"" -Wait

# Option C: Pipe directly (stdin is the prompt text)
"What is the platform architecture?" | copilot -C $targetRepo --agent crew --allow-all-tools
```

**When to use synchronous vs async:**

| Scenario | Pattern | Why |
|----------|---------|-----|
| Quick knowledge query | Synchronous CLI (Pattern 0) | Fast answer, no overhead |
| "What did you decide about X?" | Synchronous CLI (Pattern 0) | Read decisions.md via the target crew's context |
| PR review request | Either (Pattern 0 or 2/3) | Sync for quick feedback, async for thorough review |
| Task delegation (do work in their repo) | Async (Pattern 2 or 3) | Work needs to persist beyond the session |
| Long-running analysis | Async (Pattern 2) | May take multiple cycles |
| Target repo not locally cloned | Async (Pattern 3) | Can't set working directory to a remote repo |

**The coordinator decides which pattern to use based on:**
1. Is the target repo cloned locally? → If yes, sync CLI is available
2. Is this a quick query or a long task? → Quick = sync, long = async
3. Does the work need to persist? → If yes, use async (creates artifacts)
4. Is the target crew's Ralph running? → Needed for async processing

**Requirements:**
- Target repo must be cloned locally (for `copilot -C <directory>`)
- Target repo must be Crew-initialised (`.crew/config.json` + `.github/agents/crew.agent.md` present), so `--agent crew` resolves to the peer's coordinator
- Prompt file avoids argument-splitting bugs (see `ralph-watch.ps1` lines 2166-2184)

**Response quality:** ⭐⭐⭐⭐⭐ — the CLI session has full context of the target repo, including code, crew metadata, and MCP tools.

### Liveness Protocol for Pattern 0

The synchronous CLI session requires monitoring to avoid false timeouts. With 7+ MCP servers initializing and `.crew/` metadata being read, startup can take 30-60 seconds. A hard timeout kills valid sessions before they complete. Instead, monitor the agency session's activity log directory.

**Health Check Approach:**

Instead of a fixed wall-clock timeout, monitor the agency session log directory for activity:

```powershell
# The Copilot CLI creates a session log directory at ~/.copilot/logs/.
# Older `agency` runtimes wrote to ~/.agency/logs/; fall back to that
# location if the new path doesn't exist yet on the user's machine.
# e.g., ~/.copilot/logs/session_20260325_071211_57824
$copilotLogs = "$env:USERPROFILE\.copilot\logs"
$agencyLogs = "$env:USERPROFILE\.agency\logs"
$logRoot = if (Test-Path $copilotLogs) { $copilotLogs } elseif (Test-Path $agencyLogs) { $agencyLogs } else { $null }
if ($logRoot) {
    $logDir = Get-ChildItem $logRoot -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
$lastSize = 0
$stallCount = 0

while ($proc -and -not $proc.HasExited) {
    Start-Sleep -Seconds 15
    $currentSize = (Get-ChildItem $logDir -Recurse -File | Measure-Object -Property Length -Sum).Sum

    if ($currentSize -eq $lastSize) {
        $stallCount++
        if ($stallCount -ge 4) { # 60s with no progress
            Write-Warning "Session stalled — no log activity for 60s"
            break
        }
    } else {
        $stallCount = 0
        $lastSize = $currentSize
    }
}
```

**Progress Indicators (What Counts as "Alive"):**

- New files appearing in the session log directory (e.g., `transcript.log`, `mcp-server-logs/`)
- Log file size increasing (indicates active processing)
- New or modified `.crew/` files in the target repo (e.g., `decisions/inbox.md`, `identity/history.md`)
- Process still running and consuming non-idle CPU time

**Stall Detection (When to Intervene):**

- **No log activity for 60s** → Issue a warning; session may be slow but not hung
- **No log activity for 120s** → Likely stuck; consider terminating and checking logs
- **Process exited with non-zero exit code** → Failed; examine `transcript.log` and `stderr` for errors
- **MCP server connection timeout** → Session blocked waiting for an MCP server response

**Recovery Actions When Stalled:**

1. **Check for user input waiting:** Inspect logs for prompts or dialogs (shouldn't happen with `--autopilot`)
2. **Check MCP server health:** Review `mcp-server-logs/` for connection errors or timeouts
3. **Retry with `--disable-builtin-mcps` flag:** For lightweight queries that don't require MCP tools
   ```powershell
   # Retry without MCP servers — faster startup, limited capability
   copilot -C $targetRepo --agent crew -p (Get-Content $promptFile -Raw) --disable-builtin-mcps --allow-all-tools
   ```
4. **Increase timeout threshold:** If MCP server initialization is consistently slow (>90s), raise threshold before declaring stall

---

### Pattern 1: Read-Only Knowledge Query (No CLI Needed)

For questions about another crew's architecture, decisions, or current state — read their `.crew/` metadata directly.

**Protocol:**
1. Read target repo's `.crew/team.md` → get stack, members, issue source
2. Read `.crew/decisions.md` → get architectural decisions
3. Read `.crew/routing.md` → understand who handles what
4. Read `.crew/identity/now.md` → get current focus
5. Scan code structure if needed (csproj files, directory layout)

**Requirements:**
- Target repo must be cloned locally or accessible via git
- No authentication needed beyond git read access

**Example:**
```powershell
# Query another crew's architecture
$targetRepo = "C:\repos\platform-crew-repo"
Get-Content "$targetRepo\.crew\team.md"
Get-Content "$targetRepo\.crew\decisions.md"
Get-Content "$targetRepo\.crew\identity\now.md"
```

**Response quality:** ⭐⭐⭐⭐ — excellent for structural/architectural questions.

---

### Pattern 2: Async Task Request (Git-Based)

For work that needs the target crew to execute (PR reviews, issue analysis, code changes).

**Protocol:**
1. Create request file in YOUR repo: `.crew/cross-crew/requests/{timestamp}-{target}-{id}.yaml`
2. Commit and push
3. Target crew's Ralph detects on next cycle
4. Target crew processes and writes response to their `.crew/cross-crew/responses/`
5. Your Ralph picks up the response

**Request File Format:**
```yaml
id: req-2026-06-13-001
source_crew: research-crew
source_repo: your-org/research-crew-repo
target_crew: platform-crew
target_repo: your-org/platform-crew-repo
request_type: knowledge_query | pr_review | task_delegation | dependency_check
priority: high | normal | low
created_at: 2026-06-13T10:00:00Z
query: "What is the current architecture of the platform?"
routing_hint: "lead"  # optional — which agent should handle this
status: pending
```

**Response File Format:**
```yaml
id: req-2026-06-13-001
responding_crew: platform-crew
responding_agent: lead
responded_at: 2026-06-13T10:15:00Z
status: completed | partial | rejected
response: |
  The platform architecture consists of...
artifacts: []  # optional file paths
```

---

### Pattern 3: Issue-Based Delegation (For GitHub-Hosted Repos)

For repos on GitHub, use issues with labels as the message bus.

**Protocol:**
1. Create issue in target repo with label `crew:cross-crew`
2. Include source crew identifier and routing hint in issue body
3. Target crew's Ralph picks up and routes to appropriate agent
4. Response posted as issue comment
5. Issue closed when complete

**Example:**
```bash
gh issue create \
  --repo your-org/platform-crew-repo \
  --title "[Cross-Crew] Architecture query from research-crew" \
  --body "Source: research-crew\nQuery: What services does the platform expose?\nRouting: lead" \
  --label "crew:cross-crew"
```

**Limitation:** Only works for repos on GitHub. Other platforms (Azure DevOps, GitLab, etc.) need different approach.

---

### Appendix: Cross-Repo Dependency Scan (Related Analysis Tool — Not a Communication Pattern)

> This section is intentionally listed as an appendix rather than "Pattern 4" — it is a one-off analysis utility for discovering how two repos relate, not a protocol the coordinator picks from the decision tree above. The four numbered communication patterns are 0–3.

For discovering how two repos relate to each other.

**Protocol:**
1. Search both repos for mutual references:
   ```powershell
   Select-String -Path (Get-ChildItem $repoA -Recurse -Include "*.md","*.cs","*.json","*.csproj") `
     -Pattern $repoB_name
   Select-String -Path (Get-ChildItem $repoB -Recurse -Include "*.md","*.cs","*.json","*.csproj") `
     -Pattern $repoA_name
   ```
2. Check shared NuGet packages / npm packages
3. Check shared ADO project or GitHub org
4. Document relationship type: code dependency, operational coupling, shared infra

---

## Discovery Protocol

Before sending any cross-crew request, verify the target:

```
1. Does .crew/team.md exist?           → Crew is installed
2. What is the issue_source?            → GitHub Issues | ADO | Planner
3. What agents are active?              → Check member status column
4. What is the routing table?           → Read routing.md
5. What is the current focus?           → Read identity/now.md
6. Is Ralph running?                    → Check for recent commits by Ralph
```

If `.crew/team.md` doesn't exist, the repo is not crew-enabled. Fall back to standard human communication.

---

## Platform Compatibility Matrix

| Source Issue Tracker | Target Issue Tracker | Mechanism |
|---------------------|---------------------|-----------|
| GitHub Issues | GitHub Issues | Issue-based (Pattern 3) |
| GitHub Issues | ADO Work Items | Git-based (Pattern 2) |
| GitHub Issues | Planner | Git-based (Pattern 2) |
| ADO Work Items | GitHub Issues | Issue-based (Pattern 3) via `gh` CLI |
| ADO Work Items | ADO Work Items | ADO cross-project work items |
| Any | Any | Git-based (Pattern 2) — universal fallback |

---

## Examples

### Example 1: research-crew queries platform-crew architecture

```powershell
# Step 1: Read metadata (Pattern 1)
$target = "C:\repos\platform-crew-repo"
$team = Get-Content "$target\.crew\team.md" -Raw
$decisions = Get-Content "$target\.crew\decisions.md" -Raw

# Step 2: Extract answer from metadata
# team.md reveals tech stack and member roles
# decisions.md reveals architectural choices

# Step 3: If deeper analysis needed, create async request (Pattern 2)
```

### Example 2: Request PR review from another crew

```yaml
# .crew/cross-crew/requests/2026-06-13-platform-crew-pr-review.yaml
id: pr-review-001
source_crew: research-crew
target_crew: platform-crew
request_type: pr_review
priority: normal
query: "Review PR #54 — package version fix. Check for correctness."
routing_hint: "lead"
status: pending
```

---

## Anti-Patterns

### ⚠️ Know when synchronous CLI is NOT the right choice
```powershell
# WRONG — don't use sync CLI for long-running tasks that need artifacts
copilot -C $targetRepo --agent crew -p (Get-Content $promptFile -Raw) --allow-all-tools
# If the task creates files, PRs, or takes multiple cycles → use async (Pattern 2 or 3)

# WRONG — don't use sync CLI when the target repo isn't cloned locally
copilot -C "C:\not\cloned\yet" --agent crew --allow-all-tools
# If the repo isn't available locally → use issue-based delegation (Pattern 3)
```
Synchronous CLI sessions (Pattern 0) are valid for quick queries and knowledge lookups. Use async patterns for work that needs to persist or where the target repo isn't available locally.

### ❌ Don't assume shared MCP tools
Each crew has its own MCP server instances. You cannot invoke another crew's ADO tools or GitHub tools from your session.

### ❌ Don't skip the discovery step
Always read `team.md` first. The target crew may use a different issue tracker, have different agents, or be in a different state than expected.

### ❌ Don't send requests to crews without Ralph
If the target crew doesn't have Ralph (Work Monitor) running, async requests will never be processed. Check for recent Ralph activity first.

### ❌ Don't mix up repo platforms
Different repos may use GitHub Issues vs Azure DevOps Work Items vs Jira. Check `team.md` / repository metadata for the right tooling before sending requests.

---

## Validation Status

This skill was originally drafted against two prototype crew setups (a GitHub-hosted platform crew with ~10 agents and an Azure DevOps-hosted automation crew with ~4 agents). The protocols are platform-agnostic; the examples in this document use generic names so you can substitute your own repos. Patterns 0 and 1 have been exercised end-to-end in those prototypes; Patterns 2 and 3 are documented from design but have not been end-to-end-validated against a live target repo.

| Scenario | Result |
|----------|--------|
| Knowledge query (read-only) | ✅ Works via Pattern 1 |
| Step handler discovery | ✅ Works via file scan |
| PR review (basic) | ⚠️ Partial — git log only, no API |
| Backlog enumeration | ⚠️ Partial — depends on issue platform |
| Dependency analysis | ✅ Works via cross-reference scan |
| CLI invocation (sync) + Liveness Protocol | ✅ Works — session launches successfully; log monitoring prevents false timeouts |

**Confidence: MEDIUM** — Synchronous CLI pattern (Pattern 0) validated end-to-end. Liveness protocol provides operational robustness against slow MCP initialization. Git-based async (Pattern 2) and issue-based (Pattern 3) untested end-to-end. Production readiness requires Ralph integration on both sides.
