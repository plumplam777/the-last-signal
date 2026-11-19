# Handoff — where the project stands (2026-11-19)

Read first: `The_Last_Signal_Master_Plan.md` (spec), `Documentation/TechnicalArchitecture.md`, `DevelopmentRoadmap.md`,
`DecisionsLog.md` (42 decisions), `Narrative/StoryBible.md`, `ArtDirection.md`.

## Done (committed locally; no GitHub remote yet — user to provide URL)
- Phases 1–4 complete: core services, player, interaction, inventory + 3D inspect, quests, puzzles, terminals,
  audio logs, CAIRN dialogue with trust, opening cutscene, decision screen + 3 endings.
- All 5 areas built in code by `Last Signal/Build Content/Build All Sectors`: Docking Bay, Engineering, Medical Bay,
  Research Lab, Reactor Core + Command Center (one scene, joined by a lift).
- Audio: placeholder TTS voices (`Tools/generate_voices.py`) + procedural SFX/ambience (`Tools/generate_sfx.py`).
- Phase 5 backend: own SQLite wrapper (`Scripts/Database/Sqlite.cs`, system libsqlite3), schema v1, repositories
  (Users/Sectors/Items CRUD + progress), PBKDF2 accounts (`AuthService`), JSON `SaveManager` (atomic, .bak, checkpoints),
  `AdminManager` (session-only overrides). 7 DB EditMode tests pass.
- Phase 5 UI: runtime-built `StartScreen` (New game / Load game / Settings / Quit; admin create/login under Settings → Administrator),
  `AdminPanel` (CRUD), pause menu Save + Admin tools, `MainMenuBuilder` scene.

## Phase 5 verified (2026-11-19)
- All 31 PlayMode + 23 EditMode tests pass (4 new start-screen flow tests).
- Start screen and admin panel checked by eye: `Documentation/Screenshots/phase5_start_screen.png`,
  `phase5_settings.png`, `phase5_admin_setup.png`, `phase5_admin_panel_users.png`. Preview tool: enter play mode in MainMenu, then
  `Last Signal/Diagnostics/Preview Settings Page | Preview Administrator Page | Preview Admin Panel (Play Mode)` (in-memory admin session, editor-only, no DB writes).
- Clean macOS build OK (124 MB). Smoke test of MainMenu, Cutscene_Opening, Sector_DockingBay, Sector_Engineering,
  Ending with `-batchmode -startScene <Scene> -quitAfterSeconds 8`: all exit 0, no errors, DB opens in the player.
- Small visual nits for Phase 7 polish: admin form input outlines
  are faint on the right edge; admin form background nearly blends into the dim layer.

## Phase 6 done (2026-11-19)
- Medical (`MedicalBuilder`): wipe Pod 3 nameplate → PIN 0417 → "Release secured items" entry → cabinet → research keycard.
- Research (`ResearchBuilder`): Response Trials board, decode console (Trial 7 = sent row reversed), CAIRN hint after
  2 misses, protected files + specimen safe → core keycard.
- Reactor + Command (`ReactorCommandBuilder`): 3 valves (stable ⇔ one of each Low/Mid/High; overshoot alarm + reset) →
  lift → comms console (LOG_CORE_02) → captain's console (AI_CORE_03) → decision. CAIRN's archive (LOG_CORE_03)
  opens once LOG_RES_03 was played.
- Ending A is now played (`Endings/EscapeSequence`): 120 s countdown, lift → service tunnel (crank at trust < 50) →
  Docking Bay → board the Tern → Ending scene. Timeout reloads the last checkpoint.
- Content: 14/14 logs, CAIRN §7.3–7.5 (8 graphs), objectives OBJ_08–17A, EN+BG text, TTS voices, new SFX.
- New runtime pieces: ConditionToggle, SymbolDecodePuzzle/SymbolSequenceLock, ReactorBalance(+Puzzle)/ValveInteractable,
  LiftInteractable, CrankInteractable, ConsoleDialogueInteractable, BoardShipInteractable.
- Screenshots: `Documentation/Screenshots/phase6_*.png`.

## Next
- Phase 7: polish (real art from asset packs/Blender, better voices), Windows build (needs sqlite3.dll — ask before download).
  Known visual nits: Nereth through the windows is flat/bright; pod numbers float slightly off the pods; greybox props
  everywhere (Medical/Research/Command are primitives).
- Playtest the full run start → ending by hand (each ending, both trust bands).

## Working notes
- Unity editor is usually open → use Unity MCP (menu items, refresh, run_tests); `execute_code` fails because of the `;` in the path.
- Every MonoBehaviour must be in a file with the same name (player-only scene corruption otherwise).
- Editor throttles play mode when unfocused; time-based tests speed up `Time.timeScale`.
- Unfocused editor renders no frames, so MCP screenshots miss overlay UI: run
  `Last Signal/Diagnostics/Attach UI To Camera (Play Mode)` and screenshot with `camera: MenuCamera` (or Main Camera).
- Positioned MCP screenshots (`view_position` + `view_target`) render a temp camera: good for checking rooms.
- The player binary is `Builds/macOS/TheLastSignal.app/Contents/MacOS/The Last Signal` (with spaces).
