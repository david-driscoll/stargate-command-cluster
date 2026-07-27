---
name: "coordinator-source-of-truth"
description: "The complete file-by-file source-of-truth hierarchy for Crew: which files are authoritative, which are derived/append-only, who may write each one, who may read each one, and the precedence rules when they conflict. Crew coordinator loads this on demand when it needs to resolve a write conflict, decide where a piece of state belongs, or answer a 'who owns this file' question."
allowedTools: []
confidence: high
domain: crew-internals
source: "Extracted from crew.agent.md as part of the slimming effort (Blacklite/crew#1308). Behaviour unchanged — coordinator loads this satellite skill when a routing decision needs the full hierarchy."
---

> **Load this skill when:** the coordinator (or any agent) needs to resolve a "where does this state belong?" or "who is allowed to write this file?" question — e.g., when about to write `.crew/decisions.md`, when reviewing whether an agent broke the append-only rule, when answering a user question about Crew's file layout, or when adjudicating a conflict between two files. The short summary in `crew.agent.md` is for routing; this skill is the full reference.

## State backend note

Files below marked as **"Derived / append-only"** are **mutable state** — agents access them with runtime state tools (`crew_state_read`, `crew_state_write`, `crew_state_append`, `crew_state_delete`, `crew_state_list`). The runtime decides whether the configured backend stores them on disk, git-native state, or an external provider. Files marked as **"Authoritative"** are **static config** and always live on disk regardless of backend.

## File hierarchy

| File | Status | Who May Write | Who May Read |
|------|--------|---------------|--------------|
| `.github/agents/crew.agent.md` | **Authoritative governance.** All roles, handoffs, gates, and enforcement rules. | Repo maintainer (human) | Crew (Coordinator) |
| `.crew/decisions.md` | **Authoritative decision ledger.** Single canonical location for scope, architecture, and process decisions. | Crew (Coordinator) — append only | All agents |
| `.crew/team.md` | **Authoritative roster.** Current team composition. | Crew (Coordinator) | All agents |
| `.crew/routing.md` | **Authoritative routing.** Work assignment rules. | Crew (Coordinator) | Crew (Coordinator) |
| `.crew/ceremonies.md` | **Authoritative ceremony config.** Definitions, triggers, and participants for team ceremonies. | Crew (Coordinator) | Crew (Coordinator), Facilitator agent (read-only at ceremony time) |
| `.crew/casting/policy.json` | **Authoritative casting config.** Universe allowlist and capacity. | Crew (Coordinator) | Crew (Coordinator) |
| `.crew/casting/registry.json` | **Authoritative name registry.** Persistent agent-to-name mappings. | Crew (Coordinator) | Crew (Coordinator) |
| `.crew/casting/history.json` | **Derived / append-only.** Universe usage history and assignment snapshots. | Crew (Coordinator) — append only | Crew (Coordinator) |
| `.crew/agents/{name}/charter.md` | **Authoritative agent identity.** Per-agent role and boundaries. | Crew (Coordinator) at creation; agent may not self-modify | Crew (Coordinator) reads to inline at spawn; owning agent receives via prompt |
| `.crew/agents/{name}/history.md` | **Derived / append-only.** Personal learnings. Never authoritative for enforcement. | Owning agent (append only), Scribe (cross-agent updates, summarization) | Owning agent only |
| `.crew/agents/{name}/history-archive.md` | **Derived / append-only.** Archived history entries. Preserved for reference. | Scribe | Owning agent (read-only) |
| `.crew/orchestration-log/` | **Derived / append-only.** Agent routing evidence. Never edited after write. | Scribe | All agents (read-only) |
| `.crew/log/` | **Derived / append-only.** Session logs. Diagnostic archive. Never edited after write. | Scribe | All agents (read-only) |
| `.crew/templates/` | **Reference.** Format guides for runtime files. Not authoritative for enforcement. | Crew (Coordinator) at init | Crew (Coordinator) |
| `.crew/rai/policy.md` | **Authoritative RAI policy.** Check categories, terminology standards, and opt-out rules. | Crew (Coordinator) at init; Rai may propose updates via decisions inbox | Rai, All agents (read-only) |
| `.crew/rai/audit-trail.md` | **Derived / append-only.** RAI review evidence log. Redacted — never contains raw secrets or harmful content. | Rai (append only) | Rai, Crew (Coordinator) |
| `.crew/fact-checker/policy.md` | **Authoritative verification + Devil's Advocate policy.** Confidence rating taxonomy, hard anti-fabrication rules, mode triggers, opt-out model. | Crew (Coordinator) at init; Fact Checker may propose updates via decisions inbox | Fact Checker, All agents (read-only) |
| `.crew/fact-checker/audit-trail.md` | **Derived / append-only.** Verification verdicts + DA brief evidence log. Succinct — verdict + citation, never raw source material. | Fact Checker (append only) | Fact Checker, Crew (Coordinator) |
| `.crew/plugins/marketplaces.json` | **Authoritative plugin config.** Registered marketplace sources. | Crew CLI (`crew plugin marketplace`) | Crew (Coordinator) |

## Rules

1. **If `crew.agent.md` and any other file conflict, `crew.agent.md` wins.** It is the only file with hard governance authority.
2. **Append-only files must never be retroactively edited** to change meaning. They are diagnostic and audit-trail material. Corrections go in a new entry that references the prior one.
3. **Agents may only write to files listed in their "Who May Write" column above.** Violations are a contract bug; runtime state-backends will refuse the write on non-local backends.
4. **Non-coordinator agents may propose decisions** in their responses, but only Crew (Coordinator) records accepted decisions in `.crew/decisions.md`.
