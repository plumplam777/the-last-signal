# Handoff — where the project stands (2026-11-19)

Read first: `The_Last_Signal_Master_Plan.md` (spec), `Documentation/TechnicalArchitecture.md`, `DevelopmentRoadmap.md`,
`DecisionsLog.md` (36 decisions), `Narrative/StoryBible.md`, `ArtDirection.md`.

## Done (committed locally; no GitHub remote yet — user to provide URL)
- Phases 1–4 complete: core services, player, interaction, inventory + 3D inspect, quests, puzzles, terminals,
  audio logs, CAIRN dialogue with trust, opening cutscene, decision screen + 3 endings.
- Sectors built: Docking Bay, Engineering (code-built by `Last Signal/Build Content/Build All Sectors`).
- Audio: placeholder TTS voices (`Tools/generate_voices.py`) + procedural SFX/ambience (`Tools/generate_sfx.py`).
- Phase 5 backend: own SQLite wrapper (`Scripts/Database/Sqlite.cs`, system libsqlite3), schema v1, repositories
  (Users/Sectors/Items CRUD + progress), PBKDF2 accounts (`AuthService`), JSON `SaveManager` (atomic, .bak, checkpoints),
  `AdminManager` (session-only overrides). 7 DB EditMode tests pass.
- Phase 5 UI: runtime-built `StartScreen` (first admin / login / register / menu / settings),
  `AdminPanel` (CRUD), pause menu Save + Admin tools, `MainMenuBuilder` scene.

## Phase 5 verified (2026-11-19)
- All 27 PlayMode + 23 EditMode tests pass.
- Start screen and admin panel checked by eye: `Documentation/Screenshots/phase5_start_screen.png`,
  `phase5_admin_panel_users.png`. Preview tool: enter play mode in MainMenu, then
  `Last Signal/Diagnostics/Preview Admin Panel (Play Mode)` (in-memory admin session, editor-only, no DB writes).
- Clean macOS build OK (124 MB). Smoke test of MainMenu, Cutscene_Opening, Sector_DockingBay, Sector_Engineering,
  Ending with `-batchmode -startScene <Scene> -quitAfterSeconds 8`: all exit 0, no errors, DB opens in the player.
- Small visual nits for Phase 7 polish: first-admin panel has empty space at the bottom; admin form input outlines
  are faint on the right edge; admin form background nearly blends into the dim layer.

## Next
- Phase 6: Medical Bay, Research Lab, Reactor+Command (finale, Ending A countdown), remaining 9 logs, CAIRN lines §7.3–7.5.
- Phase 7: polish (real art from asset packs/Blender, better voices), Windows build (needs sqlite3.dll — ask before download).

## Working notes
- Unity editor is usually open → use Unity MCP (menu items, refresh, run_tests); `execute_code` fails because of the `;` in the path.
- Every MonoBehaviour must be in a file with the same name (player-only scene corruption otherwise).
- Editor throttles play mode when unfocused; time-based tests speed up `Time.timeScale`.
- Unfocused editor renders no frames, so MCP screenshots miss overlay UI: run
  `Last Signal/Diagnostics/Attach UI To Camera (Play Mode)` and screenshot with `camera: MenuCamera` (or Main Camera).
- The player binary is `Builds/macOS/TheLastSignal.app/Contents/MacOS/The Last Signal` (with spaces).
