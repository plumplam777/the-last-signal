# The Last Signal — Technical Architecture

Source of truth for system boundaries. Spec: `The_Last_Signal_Master_Plan.md`.
Any change to a public API or data contract below must be recorded in `DecisionsLog.md`.

## 1. Guiding idea

One small, explicit **world state** holds everything that progression depends on.
Gameplay objects (doors, puzzles, quests, dialogue) never talk to each other directly —
they read **conditions** against that state and write **facts** into it.
Save/load then only has to serialise: world state + inventory + quests + player transform.

```
 Interactables ──► facts ──► WorldState ◄── conditions ◄── Doors / Quests / Dialogue
                                 │
                       SaveManager (JSON)
```

## 2. Runtime layout

| Scene | Purpose |
|---|---|
| `Bootstrap` | Build index 0. Creates the persistent `GameServices` root (DontDestroyOnLoad), then loads `MainMenu`. |
| `MainMenu` | Start screen: New game (register/sign in) / Load game (sign in → continue or restart) / Settings (with administrator setup + login → admin panel) / Quit. |
| `Cutscene_Opening` | Data-driven `CutsceneDirector` shot list (decision 30); ends by loading the Docking Bay. |
| `Sector_DockingBay` | Tutorial: flashlight, fuse, breaker, inner door. Also the end of the Ending A escape (service tunnel exit, board the Tern). |
| `Sector_Engineering` | Coupling, generator start order, power routing (Array-feed temptation). |
| `Sector_Medical` | Pod 3 nameplate, patient record PIN 0417, confiscated cabinet → research keycard. |
| `Sector_Research` | Response Trials board, Trial 7 decode (mirror rule), safe → core keycard. |
| `Sector_ReactorCommand` | Reactor shaft (valve balance) and Command Center 20 m above (lift); comms log, CAIRN archive, captain's console → decision; service tunnel for Ending A. |
| `Ending` | Ending sequences / epilogue / credits. |

Sectors load one at a time (`LoadSceneMode.Single`); the persistent services survive.
Each sector contains a `SectorRoot` with `SpawnPoint`s (string ids). The player prefab
is instantiated by `SceneLoader` at the requested spawn point. No additive streaming
unless profiling on the M4 proves we need it.

Opening the game from any sector scene in the Editor works: `GameServices` auto-creates
itself if missing (`RuntimeInitializeOnLoadMethod`), and a dev user session is used.

## 3. Systems and responsibilities

All managers are components under the persistent `GameServices` object and are reached through
`GameServices.X` (one explicit access point — no scattered singletons).

| System | Folder | Responsibility | Does NOT |
|---|---|---|---|
| `GameServices` | Core | Creates/owns managers, exposes them. | Contain gameplay logic. |
| `GameStateMachine` | Core | Boot / Menu / Cutscene / Playing / Paused / Ending; cursor lock; time scale. | Load scenes itself. |
| `SceneLoader` | Core | Async scene loads, fade, spawn player at spawn id. | Decide *which* scene (callers do). |
| `LocalizationService` | Core/Localization | EN/BG string tables, `Get(key, args)`, `LanguageChanged` event, persists choice. | Store gameplay text in code. |
| `InputService` | Core/Input | Owns the single generated `LastSignalControls` instance; switches Player/UI maps. | — |
| `WorldState` | Core/State | Typed flags (`bool`/`int`/`string`) with change event. Keys are namespaced strings (see §5). | Know about doors, quests… |
| `SectorRoot` / `SpawnPoint` | Player | One per sector scene: spawns the player prefab at `SceneLoader.PendingSpawnId`, marks the sector visited, enters Playing. | — |
| `PlayerController` | Player | CharacterController movement, look, sprint, optional jump, footstep events. | Interaction, flashlight. |
| `PlayerInteractor` | Interaction | Raycast from camera, finds `IInteractable`, raises prompt events, calls `Interact`. | Item-specific logic. |
| `FlashlightController` | Player | Toggle, battery (optional), flicker. | — |
| `InventoryManager` | Inventory | Add/remove/has, selection, change events, combine recipes. | UI. |
| `PuzzleManager` | Puzzles | Registry of `Puzzle` instances, completion events, writes `puzzle.<id>.solved`. | Puzzle-specific rules. |
| `QuestManager` | Quests | Objective states (Locked/Active/Completed) driven by `Condition`s; current objective text. | UI layout. |
| `DialogueManager` | Dialogue | Runs `DialogueGraph` assets, evaluates conditions, applies effects (trust, flags). | Rendering — UI listens to events. |
| `AudioLogManager` | Audio | Collect/play logs, subtitles timeline, collected set. | — |
| `AudioManager` | Audio | Mixer groups, ambience per sector, one-shots, volume settings. | — |
| `SaveManager` | SaveSystem | JSON save/load per user, versioning, checkpoints, corruption handling. | SQL. |
| `DatabaseManager` | Database | Opens SQLite, migrations, repositories. Only class that touches SQL. | Game rules. |
| `AuthService` | Database | Register / login / roles, PBKDF2 password hashing, current `UserSession`. | UI. |
| `AdminManager` | Admin | Admin-only commands (unlock all, teleport to sector, grant items, toggle god-flags); refuses if session is not Admin. | Change the normal player's save unless explicitly "applied". |

### 3.1 Implemented in Phase 3
`SettingsService` (Core), `GameContent` registry (Core, `Resources/GameContent.asset`), `QuestManager` + `QuestDefinition` (Quests),
`PuzzleManager` + `Puzzle`/`SequencePuzzle`/`SequenceStepInteractable`/`CodeLock` (Puzzles), `TerminalDefinition` + `TerminalInteractable` (Puzzles),
`AudioLogManager` + `AudioLogDefinition` (Audio), `AudioLogPickup`/`LeverInteractable` (Interaction), UI: `UiScreen` base (one open screen at a time →
`GameState.InMenu`), `InventoryScreen` + `ItemInspector`, `TerminalScreen`, `ObjectiveHud`, `SubtitleHud`. `GameServices` is the composition root
and may reference every system; nothing else references `GameServices` internals.

### 3.2 Implemented in Phases 4–6
Dialogue: `DialogueGraph`/`DialogueManager`/`DialogueTrigger`, `ConsoleDialogueInteractable` (dialogue → decision screen).
Endings: `EndingScript` (lines by ending + trust), `EndingController` (Ending scene), `EscapeSequence` (Ending A 120 s countdown,
persists across scene loads, re-arms from WorldState after a load), `BoardShipInteractable`.
Puzzles: `RoutingBoard`, `SymbolSequenceLock` + `SymbolDecodePuzzle` + `SymbolKeyInteractable`, `ReactorBalance` + `ReactorBalancePuzzle` +
`ValveInteractable` (pure logic classes are unit-tested in EditMode). Interaction: `ConditionToggle` (show/hide objects by a condition),
`LiftInteractable` (in-scene teleport), `CrankInteractable`, `SectorHatch`. Database/accounts/saves/admin: see §3 table and `DatabaseSchema.md`.
UI: `StartScreen`, `AdminPanel`, `DecisionScreen`, all built in code with `RuntimeUi`. Content is generated by editor builders
(`Scripts/Editor/*Builder.cs`, `ContentPipeline`) — scenes are never edited by hand.

## 4. Dependency map

Arrows = "depends on / calls". No cycles allowed; lower rows never reference upper rows.

```
UI layer (menus, HUD, inventory UI, terminal UI, dialogue UI, admin UI)
   │  listens to events, calls public APIs
   ▼
Gameplay: PlayerController, PlayerInteractor, Interactables, Doors, Puzzles,
          QuestManager, DialogueManager, AudioLogManager, AdminManager
   │
   ▼
State & data: WorldState, InventoryManager, Condition/Effect evaluators,
              ScriptableObject definitions (Items, Puzzles, Quests, Dialogue, AudioLogs)
   │
   ▼
Persistence: SaveManager (JSON) ── reads/writes ──► WorldState, Inventory, Quests, player transform
             DatabaseManager/AuthService (SQLite) ──► accounts, catalogs, progress summary
   │
   ▼
Core: GameServices, GameStateMachine, SceneLoader, InputService, LocalizationService, Log
```

Specific edges worth stating:
- `Door` → `Condition` → (`WorldState`, `InventoryManager`). Doors never call QuestManager.
- `Puzzle` → `PuzzleManager` → `WorldState` (writes `puzzle.<id>.solved`).
- `QuestManager` → `WorldState` (subscribes to changes; re-evaluates objective conditions).
- `DialogueManager` → `WorldState` (conditions, trust, flags), `InventoryManager` (conditions).
- `SaveManager` → `WorldState`, `InventoryManager`, `QuestManager`, `AudioLogManager`, `SceneLoader`.
- `SaveManager` → `DatabaseManager` only to write the `GameProgress` summary **after** a successful JSON write.
- `AdminManager` → `AuthService` (role check), `WorldState`, `InventoryManager`, `SceneLoader`, `DatabaseManager`.

## 5. Shared data contracts

### 5.1 WorldState keys (lowercase, dot-namespaced)
| Pattern | Type | Example |
|---|---|---|
| `power.<sector>` | bool | `power.docking` |
| `door.<doorId>.unlocked` | bool | `door.dock_inner.unlocked` |
| `puzzle.<puzzleId>.solved` | bool | `puzzle.eng_power_routing.solved` |
| `log.<logId>.collected` | bool | `log.log_eng_01.collected` |
| `story.<flag>` | bool/int | `story.met_ai` |
| `choice.<choiceId>` | string | `choice.final = "destroy"` |
| `ai.trust` | int 0–100 (start 50) | |
| `sector.<sector>.visited` | bool | |

Sector ids: `docking`, `engineering`, `medical`, `research`, `reactor`.

### 5.2 Conditions and effects (serialisable, used in Inspector)
```csharp
[Serializable] public class Condition { ConditionType Type; string Key; int Value; ItemDefinition Item; bool Negate; }
// Types: FlagTrue, IntAtLeast, IntBelow, HasItem, PuzzleSolved, LogCollected, SectorPowered
[Serializable] public class Effect { EffectType Type; string Key; int Value; ItemDefinition Item; }
// Types: SetFlag, AddInt, GiveItem, RemoveItem, CompleteObjective, PlayLog, StartDialogue
```
`ConditionSet` = list + AND/OR. Doors, quests, dialogue choices and terminals all use these.

### 5.3 Interaction
```csharp
public interface IInteractable {
    string GetPromptText();                      // already localized, e.g. "Pick up Fuse"
    bool CanInteract(PlayerInteractor who, out string reasonKey);   // reasonKey = localization key or null
    void Interact(PlayerInteractor who);
    bool IsInteractionEnabled { get; }           // false = invisible to the interactor (used one-shot)
}
```
Base component `Interactable : MonoBehaviour, IInteractable` with `ConditionSet requirements`,
`List<Effect> effects`, `oneShot` (remembered via `story.used.<id>`) and `UnityEvent onInteracted` — most props need no custom script.
Implemented subclasses: `PickupInteractable` (item → inventory), `ItemSocketInteractable` (insert/consume item, e.g. fuse box),
`DoorController` (sliding door; unlock remembered in `door.<id>.unlocked`). `PoweredObject` toggles objects on `power.<sector>`.

### 5.4 Definitions (ScriptableObjects, all with stable string `Id`)
`ItemDefinition` (Id, NameKey, DescriptionKey, Icon, InspectPrefab, ItemType, AccessLevel),
`PuzzleDefinition` (Id, SectorId, Type, config), `QuestDefinition` (ordered `ObjectiveDefinition`s with
activate/complete `ConditionSet`s), `DialogueGraph` (nodes, choices with conditions + effects),
`AudioLogDefinition` (Id, SpeakerKey, TitleKey, AudioClip, subtitle lines with timestamps, counts for hidden ending).
A `GameDatabaseRegistry` SO lists all definitions so saves can resolve ids back to assets.

### 5.5 Save file (JSON, `persistentDataPath/Saves/<userId>/<slot>.json`)
```json
{ "version": 1, "userId": 3, "savedAtUtc": "...", "scene": "Sector_Engineering",
  "spawnId": "", "playerPos": [0,0,0], "playerYaw": 0, "playerPitch": 0,
  "inventory": [{"id":"fuse_basic","qty":1}], "selectedItem": "",
  "world": {"bools":{}, "ints":{}, "strings":{}},
  "quests": {"q_main": {"active":"obj_x", "completed":["obj_a"]}},
  "playtimeSeconds": 0 }
```
Writes go to `<slot>.json.tmp` then atomic replace; a `.bak` of the previous save is kept.
Unknown/newer `version` → refuse with a clear message; older → migrate step by step.

### 5.6 SQLite vs JSON (no overlap)
- **SQLite** (`persistentDataPath/lastsignal.db`): `Users`, `Sectors`, `Items` (admin-editable catalogs),
  `AudioLogs` catalog, `GameProgress` (one summary row per user: sector, objective, last saved, ending),
  `PlayerChoices` (final decisions, for the admin overview). See `DatabaseSchema.md`.
- **JSON**: the full resumable run state. JSON is authoritative for gameplay; the DB summary is derived from it.
- Admin `Sectors.IsUnlocked = 1` is a global *demo override* read by doors; it never writes into a player's JSON.

## 6. Localization
- Tables: `Assets/_Project/Localization/en.json`, `bg.json` — flat `{ "key": "text" }`, loaded as TextAssets.
- Keys: `ui.*`, `prompt.*`, `item.<id>.name|desc`, `obj.<id>`, `log.<id>.title|line.N`, `dlg.<id>`, `cut.*`.
- `LocalizedText` component on any TMP text; refreshes on `LanguageChanged`.
- Missing key → falls back to English, then to `#key#`, and logs a warning once.
- Font: one TMP font asset with Latin + Cyrillic coverage (to be chosen with the art direction).

## 7. Input
One asset `LastSignalControls.inputactions` (generated C# class) with maps:
- **Player**: Move (WASD), Look (Mouse delta), Sprint (LShift), Jump (Space), Flashlight (F)
- **Interaction**: Interact (E), Inventory (Q)
- **UI**: Navigate, Submit, Cancel/Pause (Esc), Point, Click, ScrollWheel
`InputService` enables Player+Interaction while playing and only UI when a menu/terminal is open.
Rebinding saved to PlayerPrefs via `SaveBindingOverridesAsJson`.

## 8. Assemblies
- `LastSignal.Runtime` (`Assets/_Project/Scripts`) — references Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI, Unity.Timeline.
- `LastSignal.Editor` (`Assets/_Project/Scripts/Editor`) — project setup and validation tools.
- `LastSignal.Tests` (`Assets/_Project/Tests`) — EditMode tests for WorldState, conditions, inventory, save serialization, auth hashing.

## 9. Error handling & logging
`Log.Info/Warn/Error(system, message)` prefixes `[System]`. Missing references are validated in
`OnValidate` and by an Editor "Validate Scenes" tool. Save/DB failures surface a UI toast and are never swallowed.

## 10. Performance rules (M4, 16 GB)
URP PC asset only; baked lighting for static geometry, ≤ 4 real-time shadowed lights visible;
texture max 2048 (props 1024); ASTC/BC per platform defaults; one sector loaded at a time;
profile before pooling.
