# Development Roadmap (1 month: 2026-11-19 → 2026-12-19)

Solo developer + Claude as technical lead. Scope is cut to **5 compact areas**:
Docking Bay → Engineering → Medical Bay → Research Lab → Reactor Core & Command (finale).

| Week | Dates | Phases | Deliverable |
|---|---|---|---|
| 1 | Nov 19–25 | **P1 Foundation**, **P2 First playable**, start **P3** | Docking Bay greybox: walk, flashlight, pick up fuse, restore power, open door. Inventory, doors, puzzle framework, quest tracker. |
| 2 | Nov 26–Dec 2 | Finish **P3**, **P4 Narrative**, **P5 Database & Admin** | Terminal UI, audio logs w/ subtitles, AI dialogue + trust, endings logic. SQLite accounts, polished login/start screen, admin panel with 3 CRUDs, save/load. EN/BG UI. |
| 3 | Dec 3–9 | **P6 Content** | All 5 areas built with asset packs + Blender props, ≥ 4 puzzles, opening cutscene, TTS audio logs, both endings (+ hidden if on track). |
| 4 | Dec 10–16 | **P7 Polish & testing** | Lighting/audio/UI polish, BG translation pass, bug fixing, macOS build, Windows build. |
| — | Dec 17–19 | Buffer | Diploma documentation, demo script, recorded voices if desired. |

## Phase 1 — Foundation (done 2026-11-19, GitHub remote pending)
- [x] Unity 6.3 URP project, Input System only
- [x] MCP for Unity + Blender MCP set up
- [x] Folder structure, assembly definitions
- [x] Input actions asset (Player / Interaction / UI)
- [x] Core services: GameServices, SceneLoader, GameStateMachine, LocalizationService (EN/BG), WorldState, Log
- [x] Scenes: Bootstrap, MainMenu, Sector_DockingBay in Build Settings
- [ ] Git + Unity .gitignore + GitHub remote
- [x] macOS build launches

## Phase 2 — First playable (done 2026-11-19)
- [x] PlayerController (walk/sprint/jump/look), FlashlightController (pickup-gated, F)
- [x] PlayerInteractor + HUD prompt/reason/toasts, pause menu (Esc)
- [x] ItemDefinition + InventoryManager, Condition/Effect system (pulled forward from Phase 3)
- [x] Pickup, ItemSocket (fuse box), PoweredObject, DoorController
- [x] Docking Bay greybox in the Brass & Signal art direction (code-built, see `DockingBayBuilder`)
- [x] EditMode (9) + PlayMode (4) tests, macOS build starts in the sector without errors
- [ ] Footstep/door/flashlight audio (needs audio assets)

## Phase 3 — Core gameplay systems (done 2026-11-19)
- [x] Mouse sensitivity setting (default ×2.5 after playtest) + invert Y in the pause menu (`SettingsService`)
- [x] QuestDefinition + QuestManager (sequential, cascading, completion stored in WorldState) + HUD objective panel
- [x] Puzzle base + PuzzleManager, SequencePuzzle (+ step interactables), CodeLock, LeverInteractable (breaker)
- [x] TerminalDefinition + TerminalInteractable + TerminalScreen (entries, PIN lock, read tracking)
- [x] InventoryScreen (Q): items with 3D inspection (ItemInspector), audio logs tab with replay and x/14 counter
- [x] AudioLogDefinition + AudioLogManager (clip per language or subtitle-only timing) + SubtitleHud
- [x] Keycards: `AccessLevelAtLeast` / `HasItem` conditions on doors (content arrives with Medical/Research)
- [x] GameContent registry (Resources) for id → asset resolution
- [x] Docking Bay content from the story bible: OBJ_01–04, LOG_DOCK_01/02, Bay Control terminal, fuse + breaker B-2
- [x] Tests: 12 EditMode + 10 PlayMode passing; macOS build smoke test

## Phase 3b — Playtest fixes + Engineering (done 2026-11-19)
- [x] Cursor capture fix; flashlight battery + HUD panel + longer hint
- [x] Sector hatches (Docking ↔ Engineering) with spawn points
- [x] Engineering: power coupling, generator start sequence (Kaye's diagram), routing board with the Array-feed temptation (`story.fed_array`), LOG_ENG_01–03, status terminal, interlock panel, Medical corridor door
- [x] Objectives OBJ_05–07; puzzle failure feedback on the HUD
- [x] 12 EditMode + 14 PlayMode tests; clean macOS build verified for both sectors

## Phase 4 — Narrative (done 2026-11-19, voices pending)
- [x] DialogueGraph / DialogueManager / DialogueTrigger (sector start, flag, volume) + CAIRN comms panel (eye, typewriter, choices 1–3, trust flash)
- [x] CAIRN conversations AI_DOCK_01/02, AI_ENG_01/02/03 with scripted trust deltas and flags (EN/BG)
- [x] Trust: 0–100 from 50; add-on lines at ≥65 / ≤35; epilogues split at 50
- [x] Opening cutscene "The Call": 14 shots, cockpit + exterior sets (Nereth, the Murmur, Lantern Station, Tern), hold Space to skip
- [x] Decision screen (A / B / hidden C with all 14 logs, confirmation) + Ending scene (events, lines, epilogue, credits → main menu)
- [x] 16 EditMode + 20 PlayMode tests; clean macOS build, all 5 scenes load without errors
- [ ] Text-to-speech voices for CAIRN, logs and cutscene (subtitles already work without audio)
- [ ] Ending A escape countdown and the finale sector (Phase 6)

## Phase 5 — Database & administration ✅
- [x] Own SQLite wrapper over system libsqlite3 (replaces sqlite-net), schema v1 + migrations
- [x] AuthService (PBKDF2), start screen: New game / Load game / Settings / Quit (admin setup and login inside Settings), EN|БГ toggle
- [x] Admin panel: Users, Sectors, Items CRUD + read-only Progress; pause menu Save + admin tools
- [x] SaveManager JSON (atomic, .bak, checkpoints); autosave suppressed during reset/restore
- [x] 23 EditMode + 27 PlayMode tests; clean macOS build, all 5 scenes smoke-tested without errors

## Phase 6 — Content
Sectors, puzzles (power routing, keycard security, decode sequence, reactor balance), props, lighting,
audio, TTS voice lines, endings.

## Phase 7 — Polish & testing
Acceptance checklist from the spec §16, performance pass, builds.

## Cut list if behind schedule (in this order)
1. Hidden ending C  2. Item combination  3. Drag-and-drop inventory  4. Jump
5. Merge Medical into Research  6. Windows build verification.
