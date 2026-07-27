---
name: "nap"
description: "Context hygiene — compress, prune, archive .crew/ state"
domain: "maintenance"
confidence: "medium"
source: "extracted"
---

# Skill: nap

> Context hygiene — compress, prune, archive .crew/ state

## What It Does

Reclaims context window budget by compressing agent histories, pruning old logs,
archiving stale decisions, and cleaning orphaned inbox files.

## When To Use

- Before heavy fan-out work (many agents will spawn)
- When history.md files exceed 15KB
- When .crew/ total size exceeds 1MB
- After long-running sessions or sprints

## Invocation

- CLI: `crew nap` / `crew nap --deep` / `crew nap --dry-run`
- REPL: `/nap` / `/nap --dry-run` / `/nap --deep`

## Confidence

medium — Confirmed by team vote (4-1) and initial implementation
