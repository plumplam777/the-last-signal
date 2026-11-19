# The Last Signal — Handoff for a new chat

**Date:** 2026-11-19 · **Deadline:** ≈ 2026-12-19 · **State:** Phases 1–6 done, Phase 7 (polish & testing) in progress — playthrough tests + §16 checklist done.
**Last commit:** see `git log -1` (Phase 6 + this handoff). Everything is committed locally; **there is no GitHub
remote yet** — the user will provide the URL; then add it and push.

> **New chat: read this file first**, then skim `DecisionsLog.md` (42 decisions) and the parts of
> `Narrative/StoryBible.md` you need. Do not re-ask the user anything listed under "User decisions".

---

## 1. What the project is

A diploma-project 3D first-person narrative puzzle game in **Unity 6.3 (6000.3.10f1), URP, Input System only**, on macOS
(Apple Silicon). The player (Noa Varden, relay technician) answers a distress call from Lantern Station above the ice
world Nereth. The only voice aboard is the station AI **CAIRN**. Five linear areas:
**Docking Bay → Engineering → Medical Bay → Research Lab → Reactor Core + Command Center (finale)**,
14 audio logs, CAIRN dialogue with a 0–100 trust value, final decision with 3 endings (C hidden, needs all 14 logs).
Also required by the diploma: real local accounts (SQLite), admin login + admin panel with CRUD, save/load, EN + BG.

Solo developer = the user; Claude = technical lead (writes code, content, docs; decides when to use subagents).

## 2. User decisions (do not re-ask)

- Deadline ~1 month (≈ 2026-12-19). Languages **English + Bulgarian**, switchable in game.
- Story/names/anomaly drafted by Claude, approved by the user (StoryBible v1.0 is approved).
- Art: mix of asset packs + Blender props. Style = **stylized brass/teal/terracotta (BioShock Infinite) mixed with dark
  sci-fi console rooms**; dark navy bases, medium saturation, **never neon-bright** (`Documentation/ArtDirection.md`).
- Voices: AI TTS placeholders now; the user may record real voices later (drop-in WAV replacement).
- Real local accounts (SQLite), polished start screen. Start screen = **New game / Load game / Settings / Quit**;
  administrator create/login is a small link **inside Settings** (user request, decision 36).
- Final choice is always free; trust only changes dialogue/epilogue lines.
- Opening = non-interactive cutscene. GitHub: user connects it; Claude commits and pushes.

## 3. How to work on this machine (important)

- Project path: `/Users/plami/diplomna;3/Last_signal` — **the `;` in the path breaks Unity MCP `execute_code`**.
  Drive the editor with **menu items** (`Last Signal/...`), `refresh_unity`, `run_tests`, `manage_scene`, `manage_camera`.
- Unity editor is usually open with **MCP for Unity** connected (`.mcp.json`: `unity` and `blender` via
  `~/Library/Python/3.9/bin/uvx`). No Homebrew, no `gh`, no git-lfs, no node. `curl | sh` installers are blocked.
- **New C# files need a full refresh** (`refresh_unity` scope `all`, mode `force`); scope `scripts` does not import them.
- Editor unfocused ⇒ no frames rendered: MCP screenshots of overlay UI need
  `Last Signal/Diagnostics/Attach UI To Camera (Play Mode)` + screenshot with `camera`. For rooms, use positioned
  screenshots (`view_position` + `view_target`), which render a temp camera.
- Time-based PlayMode tests speed up `Time.timeScale`; the editor throttles unfocused play mode.
- **Every MonoBehaviour must live in a file with the same name** (otherwise player-only scene corruption).
- **Smoke-testing the built player:** never `kill` it (the user got blocking macOS "Reopen" dialogs). Always:
  ```bash
  "Builds/macOS/TheLastSignal.app/Contents/MacOS/The Last Signal" -batchmode -startScene Sector_Medical -quitAfterSeconds 8 -logFile <scratch>/smoke.log
  ```
  `-startScene` starts a fresh run in that scene; `-quitAfterSeconds` quits cleanly.
- Windows build needs a `sqlite3.dll` — **ask the user before downloading anything**.
- Commit style: one commit per finished step, message ends with the `Co-Authored-By` line the session gives.

## 4. Repository map

| Path | What |
|---|---|
| `The_Last_Signal_Master_Plan.md` | Original spec (§16 = acceptance criteria) |
| `Documentation/` | `TechnicalArchitecture.md`, `DevelopmentRoadmap.md`, `DecisionsLog.md`, `DatabaseSchema.md`, `ArtDirection.md`, `AssetRegister.md`, `Narrative/StoryBible.md`, `Screenshots/` |
| `Assets/_Project/Scripts/Core` | `GameServices` (service locator, created before first scene), `WorldState` + `WorldKeys`, `Conditions`/`Effects`, `SceneLoader` + `SceneIds`, `LocalizationService` (`Loc.Get`), `SettingsService`, `GameStateMachine`, `BootstrapEntry` (`-startScene`, `-quitAfterSeconds`) |
| `Scripts/Player` | `PlayerController`, `FlashlightController` (battery), `SectorRoot` (spawns player at spawn id), `SpawnPoint` |
| `Scripts/Interaction` | `Interactable` base (requirements, effects, one-shot), `DoorController`, `SectorHatch`, `PickupInteractable`, `ItemSocketInteractable`, `LeverInteractable`, `PoweredObject`, `AudioLogPickup`, `ConditionToggle`, `LiftInteractable`, `CrankInteractable` |
| `Scripts/Puzzles` | `Puzzle` base (solved = `puzzle.<id>.solved`), `SequencePuzzle`, `RoutingBoard`, `CodeLock` + terminals, `SymbolSequenceLock` + `SymbolDecodePuzzle` + `SymbolKeyInteractable`, `ReactorBalance` + `ReactorBalancePuzzle` + `ValveInteractable` |
| `Scripts/Dialogue` | `DialogueGraph` (nodes with trust/flag conditions), `DialogueManager`, `DialogueTrigger` (OnSectorStart / OnFlag / OnEnterVolume), `ConsoleDialogueInteractable` |
| `Scripts/Endings` | `EndingScript` (lines per ending/trust), `EndingController` (Ending scene), `EscapeSequence` (Ending A countdown), `BoardShipInteractable` |
| `Scripts/Database` | Own SQLite wrapper over system `libsqlite3` (`Sqlite.cs`), schema v1, repositories (Users/Sectors/Items/Progress), `AuthService` (PBKDF2) |
| `Scripts/SaveSystem` | `SaveManager` (JSON per user, atomic write + .bak, checkpoints) |
| `Scripts/Admin` | `AdminManager` (session-only overrides: unlock sectors, teleport, give items) |
| `Scripts/UI` | `RuntimeUi` (code-built uGUI helpers), `StartScreen`, `AdminPanel`, pause menu, HUD, inventory, terminal, dialogue, decision screens |
| `Scripts/Editor` | **All content is generated by code**: `DockingBayBuilder`, `EngineeringBuilder`, `MedicalBuilder`, `ResearchBuilder`, `ReactorCommandBuilder`, `CutsceneBuilder`, `EndingBuilder`, `MainMenuBuilder`, `PlayerPrefabBuilder`, `ContentPipeline` (Build All Sectors); data: `DockingBayContent` (items, logs, quest objectives, registry), `EngineeringContent`, `StationContent` (Phase 6 items/logs/terminals/flags), `DialogueContent` (all CAIRN graphs); tools: `BuildTools`, `DebugMenu`, `UiPreview`, `ProjectSetup` |
| `Assets/_Project/Localization/Resources/Localization/{en,bg}.json` | **All player-facing text** (keys: `log.<id>.line.N`, `dlg.<id>.<node>`, `obj.<id>`, `terminal.<id>.*`, `end.*`, `epi.*`, `ui.*`) |
| `Assets/_Project/Audio/Resources/{Voice,Sfx,Ambience}` | TTS voices (+ `voice_manifest.json` subtitle timings), procedural SFX/ambience |
| `Tools/generate_voices.py`, `Tools/generate_sfx.py` | Regenerate audio (`python3 Tools/generate_voices.py` ≈ 5 min, macOS `say`) |
| `Assets/_Project/Tests/{EditMode,PlayMode}` | 27 EditMode + 54 PlayMode tests (`FullPlaythroughTests` = 4 complete runs, one per ending) |
| `Documentation/AcceptanceChecklist.md` | Spec §16 bullets with test/screenshot evidence |

**Content pipeline:** edit a builder/content class → `refresh_unity` → menu `Last Signal/Build Content/Build All Sectors`
(regenerates all scenes, data assets and Build Settings) → run tests. Never hand-edit generated scenes.
If localization/voice text changes: run `Tools/generate_voices.py`, refresh, then Build All Sectors again (logs read
`voice_manifest.json` at build time).

**Useful menus:** `Last Signal/Build/macOS` (`(clean)` variant), `Last Signal/Debug/*` (give all items, restore sector
power, open decision screen, …), `Last Signal/Diagnostics/*` (UI previews, attach UI to camera).

## 5. Gameplay flow as implemented (spoiler map)

1. **Opening cutscene** "The Call" (skippable) → Docking Bay.
2. **Docking Bay:** flashlight → 30A fuse under a tarp → fuse box + breaker B-2 → inner door. LOG_DOCK_01/02, AI_DOCK_01/02.
3. **Engineering:** power coupling → generator start order coolant→fuel→ignition→load (Kaye's diagram) → routing board
   (Medical, Life support, Lighting; Array feed = temptation AI_ENG_03) → Medical corridor door. LOG_ENG_01–03, AI_ENG_01–03.
4. **Medical Bay:** wipe frost on Pod 3 nameplate → PIN **0417** on the patient terminal → entry "Release secured items"
   → cabinet → research keycard → hatch to Research. LOG_MED_01–03 (03 appears after unlocking), AI_MED_01 (near pod), AI_MED_02 (after LOG_MED_03).
5. **Research Lab:** board shows trials 2/4/6 (answer = sent row reversed); enter Trial 7 = **■ ● ▲ ● ■ ◆** on the decode
   console. 2 misses → AI_RES_HINT (high/mid/low by trust). Solved → LOG_RES_02 + safe → core keycard → hatch to Reactor.
   First-try solve (or high hint) sets `noticed_mirror`. LOG_RES_01/03, AI_RES_01/02.
6. **Reactor Core:** 3 coolant valves (Low/Mid/High, start Mid). Stable (output and temperature both 40–60%) ⇔ every valve
   on a different setting. Output > 75% = alarm, valves reset to Mid after 4 s. Stable → power → lift to Command. AI_CORE_01.
7. **Command:** captain's chair log (LOG_CORE_01), comms console original warning (LOG_CORE_02, critical → AI_CORE_02),
   CAIRN private archive (LOG_CORE_03, only after LOG_RES_03 was heard), captain's console (needs LOG_CORE_02) → AI_CORE_03 → decision.
8. **Decision:** A destroy / B restore / C silence (only with 14/14 logs).
   - **A** is played: 120 s countdown, lift down → emergency service tunnel (trust ≥ 50 CAIRN opens it; < 50 sealed →
     manual crank 6 turns) → Docking Bay next to the airlock → "Board the Tern" → Ending scene. Timeout → reload checkpoint.
   - **B / C** go straight to the Ending scene (exterior event, lines, epilogue by trust ≥ 50 / < 50, credits → main menu).

## 6. Status by phase

| Phase | Status |
|---|---|
| 1 Foundation | ✅ (GitHub remote still missing) |
| 2 First playable | ✅ |
| 3 Core systems (quests, puzzles, terminals, inventory, logs) | ✅ |
| 4 Narrative (CAIRN, cutscene, decision, endings) | ✅ |
| 5 Database & admin (SQLite, accounts, start screen, admin CRUD, saves) | ✅ |
| 6 Content (Medical, Research, Reactor+Command, Ending A escape, 14 logs, EN+BG, voices, SFX) | ✅ commit `4ce7238` |
| 7 Polish & testing | **in progress** — steps 1–2 done (automated playthroughs, §16 checklist) |

**Verified at handoff:** 27/27 EditMode, 42/42 PlayMode tests pass; macOS build (134 MB) starts all 8 scenes
(MainMenu, Cutscene_Opening, 5 sectors, Ending) without errors or missing translation keys.
**Not verified:** nobody has played the complete game start → ending by hand yet.

## 7. Work to be done (Phase 7, in priority order)

0. ✅ **Done 2026-11-19:** `FullPlaythroughTests` plays all four endings start → main menu through the real hatches,
   with Load game in every sector and mid-escape, the whole run once in Bulgarian, and 14/14 logs. It found and fixed a
   bug: New game after a previous run started with obj_01 already done (`StartNewRun`/`Continue` now reset/restore the
   inventory before the world). `PlayerMovementTests` added. §16 evidence is in `AcceptanceChecklist.md`.
   A by-hand playthrough by the user is still worth doing for feel (mouse look, pacing, readability).
1. **Full playthrough by hand** (now mostly automated, see 0): New game → each ending, at high and low trust, plus Ending C with
   14/14 logs. Check objectives advance in order (`quest_main`: obj_01 … obj_17a), checkpoints/Load game resume in every
   sector, the escape after Load game, BG language throughout. Fix what breaks; add regression tests.
   Tip: `Last Signal/Debug/*` menus and `-startScene` help jump around.
2. **Spec §16 acceptance checklist** (`The_Last_Signal_Master_Plan.md`): go through every bullet and record evidence
   (test name or screenshot) — useful for the diploma documentation. Admin bullets: admin can unlock sectors/bypass
   progression and manage records; ordinary players cannot.
3. **Art pass** (biggest visual gap) — **started 2026-11-19**: `Tools/blender/build_props.py` builds hero props
   headlessly (`/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python
   Tools/blender/build_props.py -- [--preview <dir>] [--only name,…]`); author parts in *Unity* coords (script converts).
   Builders place them with `PropModels.Place(id, parent, pos, rot, overrides)` and keep the greybox as fallback.
   Done: cryo pods (Medical), bridge workstations + comms console, captain's chair (Command), valve wheels/bodies
   (Reactor). **Batch 2 (2026-11-20):** captain's console, patient terminal kiosk, decode console, Okafor + Rhee
   desks, office chairs, cabinet + safe shells (colliders stay on invisible `HollowCase`), ceiling pipe runs
   (`PropModels.PipeRun`) on Medical W/E, Research W/S, Command W/E walls. User chose **Blender only, no paid generation**. Every new prop collider must keep interactables
   reachable — `InteractableReachabilityTests` enforces it. Next candidates: Docking Bay/Engineering props, listening booth, map table,
   lift cabins, hatch frames; then lighting/audio polish.
   Old note: Medical/Research/Command props are greybox primitives. Replace hero props with
   asset-pack or Blender models (Blender MCP available; `generate_3d` may cost the user credits — ask). Keep the palette
   in `ArtDirection.md`. Record every external asset in `Documentation/AssetRegister.md` (licence!).
   Known nits: Nereth through windows looks flat/bright; pod numbers float slightly; admin form input outlines faint on
   the right; admin form background blends into the dim layer.
4. **Lighting/audio polish:** per-sector ambience balance, footsteps volume, voice levels; maybe music stingers.
5. **BG translation pass** with the user (all keys exist in `bg.json`; check tone, gender forms, terms:
   Murmur = „Шепотът“, Array = „Решетката“, CAIRN stays Latin).
6. **Performance pass** on the M4 (profiler in Docking Bay and Reactor; reflection probes are baked per scene).
7. **Builds:** clean macOS build + smoke test; Windows build (ask before downloading `sqlite3.dll`).
8. **GitHub:** when the user gives the URL → `git remote add origin <url>` → push `main`.
9. **Diploma docs** (buffer week Dec 17–19): demo script, architecture/DB diagrams from `TechnicalArchitecture.md` and
   `DatabaseSchema.md`, screenshots in `Documentation/Screenshots/`.

**Cut list if behind schedule:** hidden Ending C → item combination → drag-and-drop inventory → jump →
merge Medical into Research → Windows build verification.

## 8. Gotchas learned the hard way

- Path-based `GameObject.Find("A/B")` also returns **inactive** children — tests check `activeInHierarchy`.
- `Loc.Get` of an unknown key shows `#key#`; use `BuilderKit.RawText` for non-localized world text (numbers, symbols).
- LiberationSans lacks ◆ (U+25C6); the diamond is `<rotate=45>■</rotate>` (`SymbolDecodePuzzle.Glyph`).
- Hatches: `MedicalBuilder.Hatch(yaw)` — local +Z points out through the wall (south 180, north 0, east 90).
  World text is readable from its local −Z side.
- Wall screen glows must sit **in front** of the screen box (an earlier bug hid them behind).
- Autosaves are suppressed during new-run reset/restore (`GameServices.SuppressCheckpoints`).
- `say` output differs slightly between runs; regenerating voices may touch a few old WAVs (harmless).
- `Assets/TextMesh Pro/.../LiberationSans SDF - Fallback.asset` and `ProjectSettings/EditorSettings.asset` change by
  themselves in the editor (dynamic glyphs incl. Cyrillic); committing them is fine.
