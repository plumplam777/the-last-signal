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
- Phase 5 UI (just written): runtime-built `StartScreen` (first admin / login / register / menu / settings),
  `AdminPanel` (CRUD), pause menu Save + Admin tools, `MainMenuBuilder` scene.

## In progress at handoff
- Just fixed a startup-order bug in `GameServices.Awake` (quest subscription before creation) that broke PlayMode tests;
  re-run PlayMode tests (expect 27) and fix any remaining failures. New tests: `AccountsAndSaveTests`.
- Then: screenshot start screen + admin panel, clean macOS build (`Last Signal/Build/macOS (clean)`), smoke-test every
  scene with `-batchmode -startScene <Scene> -quitAfterSeconds 8` (never kill the player), update docs, commit.

## Next
- Phase 6: Medical Bay, Research Lab, Reactor+Command (finale, Ending A countdown), remaining 9 logs, CAIRN lines §7.3–7.5.
- Phase 7: polish (real art from asset packs/Blender, better voices), Windows build (needs sqlite3.dll — ask before download).

## Working notes
- Unity editor is usually open → use Unity MCP (menu items, refresh, run_tests); `execute_code` fails because of the `;` in the path.
- Every MonoBehaviour must be in a file with the same name (player-only scene corruption otherwise).
- Editor throttles play mode when unfocused; time-based tests speed up `Time.timeScale`.
