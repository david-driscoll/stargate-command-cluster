---
name: "crew-help"
description: "How to actually use Crew — Crew is a custom Copilot agent (invoked via the task tool with agent_type='Crew'), not a skill. This file explains the right invocation paths for setting up a team, listing crew commands, and initializing Crew in a new project."
allowedTools: []
confidence: high
domain: crew-onboarding
---

# Skill: crew-help

> **Quick reference.** If you're reading this because a user said "use crew" or "crew" or "set up a crew", you're in the right place — read on for the correct invocation paths.

---

## Crew is a custom agent, not a skill

The Crew framework registers a **custom Copilot CLI agent** at `.github/agents/crew.agent.md`. The agent is named **`Crew`** and its description is *"Your AI team. Describe what you're building, get a team of specialists that live in your repo."*

Copilot CLI agents and skills are different things:

| Thing | How to invoke | Example |
|---|---|---|
| **Skill** | `skill(name)` tool call or natural-language match | `skill(crew-commands)` |
| **Agent** | `task` tool with `agent_type=<name>` | `task(name="...", agent_type="Crew", prompt="...")` |
| **Slash command** | Built-in CLI keyword | `/agent`, `/skills`, `/mcp` |

Calling `skill(Crew)` will fail with *"Skill not found: Crew"* because Crew is the agent, not a skill. (`/crew` as a slash command also does not exist — only built-in CLI keywords like `/agent`, `/skills`, `/mcp` are slash commands. There's no way to map a skill name to a slash command without a Copilot CLI feature change.)

---

## How to actually use Crew

Pick the path that matches the user's intent:

### A) Invoke the Crew coordinator agent (most common)

The Crew coordinator orchestrates a team of specialists. It routes work to the right agent, scaffolds a team if none exists, and enforces handoffs.

```text
task(
  name="<short-task-name>",
  agent_type="Crew",
  prompt="<what you want the team to do>"
)
```

Use this when the user says things like:
- *"Use Crew to build X"*
- *"Set up an AI team for this project"*
- *"Have the Crew coordinator design Y"*
- *"Spawn Crew"* / *"Crew, help me with ..."*

### B) See what Crew commands exist

The `crew-commands` skill is a categorized catalog of common Crew operations. The coordinator presents it as an interactive menu.

Trigger by natural-language match: `"crew commands"`, `"what can crew do"`, `"show me crew options"`, `"slash commands"`, `"what commands are available"`.

Use this when the user says things like:
- *"What can Crew do?"*
- *"Show me the crew commands"*
- *"crew help"*

### C) Initialize Crew in a fresh project

`crew init` is a **shell command**, not a tool call. The user runs it in their terminal in a project that has no `.crew/` directory yet.

```bash
crew init
```

Do **not** try to invoke this from inside an existing Copilot session — `.crew/` is already initialized if you're reading this file.

---

## What NOT to do

- ❌ Do not call `skill(Crew)`, `skill(crew)`, or `skill(crew-coordinator)` — Crew is not a skill.
- ❌ Do not type `/crew` expecting a slash command — slash commands are CLI keywords, not skill names. Use `/agent` (browse) or invoke the `Crew` agent via the `task` tool.
- ❌ Do not call `task(agent_type="Crew", …)` for tiny tasks the current agent can handle directly. Crew is for work that needs orchestration; trivial edits do not.

---

## How this skill was discovered

This skill ships from the Crew SDK templates and is wired into `MANIFEST_SKILL_NAMES`. It lives at `.copilot/skills/crew-help/SKILL.md` so the Copilot CLI's `/skills` loader picks it up alongside the other bundled Crew skills.

If you removed this skill on purpose, the model will fall back to its own reasoning and may make the lookup mistakes described above.

---

## See also

- `.github/agents/crew.agent.md` — the actual Crew coordinator agent
- `.copilot/skills/crew-commands/SKILL.md` — the command catalog
- `.copilot/skills/crew-conventions/SKILL.md` — conventions for working on the Crew codebase itself
- `.copilot/skills/crew-version-check/SKILL.md` — version-stamping mechanics
