---
name: "personal-crew"
description: "User-level AI agents that travel with you across projects"
domain: "configuration"
confidence: "medium"
source: "manual"
---

# Personal Crew — Skill Document

## What is a Personal Crew?

A personal crew is a user-level collection of AI agents that travel with you across projects. Unlike project agents (defined in a project's `.crew/` directory), personal agents live in your global config directory and are automatically discovered when you start a crew session.

## Directory Structure

```
~/Library/Application Support/crew/personal-crew/    # macOS
~/.config/crew/personal-crew/                        # Linux
%APPDATA%/crew/personal-crew/                        # Windows
├── agents/
│   ├── {agent-name}/
│   │   ├── charter.md
│   │   └── history.md
│   └── ...
└── config.json                    # Optional: personal crew config
```

## How It Works

1. **Ambient Discovery:** When Crew starts a session, it checks for a personal crew directory
2. **Merge:** Personal agents are merged into the session cast alongside project agents
3. **Ghost Protocol:** Personal agents can read project state but not write to it
4. **Kill Switch:** Set `CREW_NO_PERSONAL=1` to disable ambient discovery

## Commands

- `crew personal init` — Bootstrap a personal crew directory
- `crew personal list` — List your personal agents
- `crew personal add {name} --role {role}` — Add a personal agent
- `crew personal remove {name}` — Remove a personal agent
- `crew cast` — Show the current session cast (project + personal)

## Ghost Protocol

See `templates/ghost-protocol.md` for the full rules. Key points:
- Personal agents advise; project agents execute
- No writes to project `.crew/` state
- Transparent origin tagging in logs
- Project agents take precedence on conflicts

## Configuration

Optional `config.json` in the personal crew directory:
```json
{
  "defaultModel": "auto",
  "ghostProtocol": true,
  "agents": {}
}
```

## Environment Variables

- `CREW_NO_PERSONAL` — Set to any value to disable personal crew discovery
- `CREW_PERSONAL_DIR` — Override the default personal crew directory path
