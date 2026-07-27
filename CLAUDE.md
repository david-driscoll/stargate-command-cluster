<!-- crew:begin -->
## Crew — your AI team

This repository is managed by Crew, a multi-agent team runtime that works with
both Claude Code and GitHub Copilot.

- **Coordinator agent:** `.claude/agents/crew.md` (protocol source:
  `.github/agents/crew.agent.md`). For team work — building features,
  triage, standups, roster changes — act as (or delegate to) the crew
  coordinator rather than working solo.
- **Command catalog:** invoke the `crew` skill (/crew) for the interactive
  command menu.
- **Team state:** lives in `.crew/` (roster: `team.md`, decisions:
  `decisions.md`, per-agent charters under `agents/`). Respect the
  state-backend rules in the coordinator protocol before writing there.
- **MCP tools:** `.mcp.json` at the repo root exposes crew's state tools;
  Claude Code loads it automatically.
<!-- crew:end -->
