# Agent Responsibilities & Integration Rules

Claude (main session) = **Technical Lead / Integrator** (spec Agent 1). It owns the architecture,
is the only writer to shared core files, and merges everything into `main`.
Specialised agents are spun up only when a task is isolated enough to hand off.

| Role (spec §12) | How it is staffed | Owns (writes) |
|---|---|---|
| 1 Architect / Tech Lead | Main session | `Scripts/Core`, `Documentation/TechnicalArchitecture.md`, `DecisionsLog.md`, asmdefs, ProjectSettings, Packages |
| 2 Gameplay & Player | Main session | `Scripts/Player`, `Scripts/Interaction` |
| 3 Environment & Level | Main session + Blender MCP; asset research by subagent | `Art/`, sector scenes |
| 4 Puzzle & Interaction | Main session (subagent for isolated mini-game logic + tests) | `Scripts/Puzzles`, puzzle prefabs |
| 5 Narrative & Dialogue | Subagent | `Documentation/Narrative/`, dialogue/log content, `Localization/*.json` text (via lead) |
| 6 UI/UX | Main session | `Scripts/UI`, `Prefabs/UI`, `UI/` |
| 7 Database & Save | Main session (subagent for repository code + tests) | `Scripts/Database`, `Scripts/SaveSystem`, `DatabaseSchema.md` |
| 8 QA & Integration | Subagent review passes after each phase | Bug list in `Documentation/QA/BugList.md`; proposes fixes only |
| Translation (EN→BG) | Subagent | `Localization/bg.json` |

## Integration rules
1. Only one writer at a time touches a Unity scene, prefab, ProjectSettings or `Packages/manifest.json`.
2. Subagents never edit `Scripts/Core`, asmdefs, scenes or package versions — they deliver files in their own folder and the lead integrates.
3. New public APIs/events/WorldState keys go into `TechnicalArchitecture.md` in the same change.
4. Every change must compile in Unity (batchmode or MCP) before commit; `main` always opens and runs.
5. No placeholder presented as finished: TODOs are listed in the task report.
6. Each task ends with the completion report from spec §13.
