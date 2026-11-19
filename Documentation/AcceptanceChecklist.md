# Acceptance Checklist (Master Plan §16)

Evidence for every acceptance bullet of `The_Last_Signal_Master_Plan.md` §16. Test names refer to
`Assets/_Project/Tests/{EditMode,PlayMode}` (class.method). Screenshots are in `Documentation/Screenshots/`.

**Last full run:** 2026-11-19 — EditMode 27/27, PlayMode 54/54 (incl. the 5 full-playthrough tests and 5 reachability checks).

Status: ✅ verified by an automated test · 🖼 verified by screenshot/manual check · ⏳ still open

## The full-playthrough tests (Phase 7)

`FullPlaythroughTests` plays the whole game four times, start to finish, with **no state set by hand**: the player
travels only through the real hatches, answers every CAIRN conversation (always the most trusting, or always the most
hostile answer), picks up every audio log it can reach, and in **every sector** saves → wipes the run → *Load game*,
checking that sector, objective, trust, logs and inventory come back identical. Every visible text is scanned for
untranslated keys (`#key#`) and any unexpected error log fails the run.

| Test | Covers |
|---|---|
| `EndingA_HighTrust_EscapeThroughOpenTunnel` | trusting run → trust ≥ 50 → Destroy → CAIRN opens the tunnel → Docking Bay → board the Tern → Ending A → credits → main menu |
| `EndingA_LowTrust_LoadGameMidEscape_CrankAndBoard` | hostile run → trust < 50 → Destroy → tunnel sealed → **Load game in the middle of the escape** → countdown re-arms → crank → board → Ending A |
| `EndingB_HighTrust_InBulgarian` | the whole game in **Bulgarian** → Restore → Ending B → main menu, no missing keys anywhere |
| `EndingC_AllFourteenLogs_LowTrust` | proves all **14/14 logs** are reachable in a normal run → hidden Silence option → Ending C |
| `NewRun_AfterAnotherRun_StartsAtTheFirstObjective` | regression for the bug the playthrough found (see below) |

**Bug found and fixed by these runs:** `GameServices.StartNewRun` cleared the world before the inventory, so the
leftover flashlight from a previous run instantly re-completed *obj_01* — a New game after an earlier run started on
"find the fuse". `SaveManager.Continue` had the same ordering issue (world restored before the inventory). Both now
reset/restore the inventory first.

## Gameplay

| Criterion | Status | Evidence |
|---|---|---|
| Player movement and camera work | ✅ / 🖼 | `PlayerMovementTests.Walk_Forward_ThenSprint_IsFaster`, `MenuOpen_FreezesMovement` (virtual keyboard through the Input System). Mouse look: manual (needs a captured cursor). |
| Interactions work consistently | ✅ | `InteractableReachabilityTests` (per sector: no interactable is buried inside another collider, so the look-ray can reach it — found 3 Phase 6 bugs: both keycards sealed inside solid cabinet/safe bodies, LOG_RES_03 inside the booth terminal); every sector test + `FullPlaythroughTests` drive the real `Interactable`s; locked ones report their reason key (`FuseAndBreaker_RestorePower_AndUnlockDoor`, `FullMedicalFlow`) |
| Items can be collected | ✅ | `DockingBaySliceTests.Flashlight_RequiresPickup`, `FullMedicalFlow` (keycard), `Decode_WrongTwice_GivesHint_ThenSolvingOpensSafe` |
| Inventory updates correctly | ✅ | `InventoryAndConditionTests.*`, `DockingBaySliceTests.Inventory_ShowsPickedUpItems`, fuse consumed in `FuseAndBreaker_…`; 🖼 `phase3_inventory.png` |
| Doors validate access requirements | ✅ | `FuseAndBreaker_…` (no power), `FullEngineeringFlow` (no routed power), `FullMedicalFlow` / `Decode_…` (keycards), `Tunnel_IsSealed_OutsideTheEscape` |
| Puzzles can be completed | ✅ | `SequencePuzzleTests.*`, `FullEngineeringFlow` (generator + routing), `FullMedicalFlow` (PIN), `Decode_*`, `Balancing_PowersTheLift_ToCommand`, `Overshoot_RaisesAlarm_AndResetsToMid`, `Phase6LogicTests.*` |
| Quest objectives update correctly | ✅ | `Objectives_AdvanceThroughTheBay`, `QuestAndPuzzleLogicTests.*`; `FullPlaythroughTests` checks the objective at every step obj_01 → obj_17a |

## Narrative

| Criterion | Status | Evidence |
|---|---|---|
| Audio logs play | ✅ | `AudioLog_CollectsPlaysSubtitles_AndCounts`, `AudioTests.AudioLogs_HaveRecordingsAndPerLanguageTimings` |
| Dialogue choices work | ✅ | `DialogueTests.*`; every CAIRN conversation answered in `FullPlaythroughTests`; 🖼 `phase4_cairn_choices.png` |
| AI trust changes when expected | ✅ | `DialogueTests` (+5 / −10), `WorldStateTests.AddInt_ClampsTrust`; playthroughs assert always-trusting ⇒ ≥ 50, always-hostile ⇒ < 50 |
| Story progression is consistent | ✅ | `FullPlaythroughTests` (sector order, gated reveals: LOG_MED_03, LOG_RES_02, LOG_CORE_03), `OpeningCutscene_PlaysAllShots_ThenLoadsDockingBay` |
| Final decision triggers the correct ending | ✅ | the four ending playthroughs, `Decision_HiddenOptionLocked_…`, `EscapeTests.*`, `EndingScriptTests.*` |

## Data

| Criterion | Status | Evidence |
|---|---|---|
| SQLite database is created and accessible | ✅ | `DatabaseTests.Reopen_KeepsData_AndSchemaVersion`; every PlayMode account test opens a fresh DB |
| User roles are identified correctly | ✅ | `DatabaseTests.UsersCrud_AndAdminRules`, `Settings_AdminLogin_RejectsPlayers_AcceptsAdmins` |
| CRUD operations work | ✅ | `DatabaseTests.UsersCrud_…`, `SectorsCrud_ProtectsBuiltIns`, `ItemsCrud_AndInventoryMirrorCascade`; 🖼 `phase5_admin_panel_users.png` |
| Save files are created | ✅ | `Save_ThenContinue_RestoresSceneInventoryAndPose` ("entering a sector writes a checkpoint") |
| Progress can be loaded | ✅ | `Save_ThenContinue_…`; `FullPlaythroughTests` loads in all 5 sectors and mid-escape |
| Invalid or missing data is handled safely | ✅ | `CorruptSave_FallsBackToBackup`, `Register_ValidatesInput`, `Wrapper_BindsParameters_NoInjection`, `Passwords_AreHashed_AndVerified` |

## Administration

| Criterion | Status | Evidence |
|---|---|---|
| Administrator can access restricted tools | ✅ | `Settings_AdminLogin_RejectsPlayers_AcceptsAdmins`, `Settings_FirstAdmin_CanBeCreatedThere`; 🖼 `phase5_admin_setup.png` |
| Administrator can unlock sectors and bypass progression | ✅ | `AdminOverrides_OnlyForAdmins_AndNotSaved` (unlock, teleport, give items; `SectorHatch` honours overrides) |
| Administrator can manage supported records | ✅ | `DatabaseTests.*Crud*`; admin panel tabs Users / Sectors / Items / Progress (🖼 `phase5_admin_panel_users.png`) |
| Ordinary players cannot access administrator functionality | ✅ | `AdminOverrides_OnlyForAdmins_AndNotSaved`, `Settings_AdminLogin_RejectsPlayers_AcceptsAdmins`, `UsersCrud_AndAdminRules` |

## Technical

| Criterion | Status | Evidence |
|---|---|---|
| Project opens in the intended Unity version | ✅ | Unity 6000.3.10f1 (`ProjectSettings/ProjectVersion.txt`) |
| No persistent compilation errors | ✅ | clean console after full refresh; all suites compile and run |
| No critical missing references or runtime exceptions | ✅ | Unity Test Framework fails any test that logs an unexpected error; 81 tests incl. 4 complete runs pass |
| macOS build launches | ✅ | Phase 6 build smoke-tested in all 8 scenes (`-batchmode -startScene … -quitAfterSeconds`); ⏳ re-run on the final build |
| Performance is acceptable on the M4 | ⏳ | profiler pass (Docking Bay, Reactor) still to do |
| Project follows the agreed Git workflow | ⏳ | one commit per step on `main`, Unity `.gitignore`; GitHub remote not connected yet |
