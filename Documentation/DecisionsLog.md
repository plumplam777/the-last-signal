# Decisions Log

| # | Date | Decision | Reason |
|---|---|---|---|
| 1 | 2026-11-19 | Unity 6000.3.10f1 (Unity 6.3), URP "Universal 3D" template, Input System only (`activeInputHandler: 1`). | Spec §7. |
| 2 | 2026-11-19 | Scope: 5 compact areas (Docking, Engineering, Medical, Research, Reactor+Command merged). | 1-month deadline, solo developer. |
| 3 | 2026-11-19 | Languages EN + BG, switchable in game; custom lightweight JSON string tables instead of the Unity Localization package. | Small surface, easy for humans/agents to edit and diff, no extra package. |
| 4 | 2026-11-19 | Real local accounts in SQLite with PBKDF2 password hashing; first launch asks to create the administrator account (no hard-coded default password). | User decision; spec §6.3 security rules. |
| 5 | 2026-11-19 | Final decision is always free; AI trust affects dialogue, revealed info and epilogue only. | User decision. |
| 6 | 2026-11-19 | Opening = non-interactive Timeline cutscene. | User decision. |
| 7 | 2026-11-19 | Audio logs: AI text-to-speech first, user may re-record later. Subtitles always available. | User decision. |
| 8 | 2026-11-19 | Art: asset packs + Blender props; user will provide style inspiration. | User decision. |
| 9 | 2026-11-19 | Single `WorldState` flag store + serialisable `Condition`/`Effect` for doors, quests, dialogue, terminals. | Removes cross-system coupling; makes save/load trivial. |
| 10 | 2026-11-19 | One sector scene loaded at a time + persistent `GameServices` from `Bootstrap`. | M4 memory budget, simplicity. |
| 11 | 2026-11-19 | Removed template packages not used by the game: Visual Scripting, Unity Version Control (collab-proxy), Multiplayer Center. Removed template TutorialInfo/Readme and SampleScene. | Less compile time and noise; Git is the VCS. |
| 12 | 2026-11-19 | IDE: VS Code (Unity extension) or Rider — Visual Studio for Mac was retired by Microsoft. | Spec listed Visual Studio. |
| 13 | 2026-11-19 | Planned SQLite library: `sqlite-net` (single source file) over the native SQLite (system lib on macOS, bundled `sqlite3.dll` on Windows). To be verified in Phase 5. | Spec §6.1; one library only. |
