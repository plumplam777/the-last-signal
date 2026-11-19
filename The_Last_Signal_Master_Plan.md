# THE LAST SIGNAL

## Game Development Master Plan, Technical Specification & AI Agent Knowledge Base

**Project type:** 3D first-person sci-fi adventure / narrative puzzle
game\
**Engine:** Unity 6.3\
**Language:** C#\
**Development hardware:** MacBook Air, Apple M4, 16 GB unified memory\
**Primary target:** macOS desktop; Windows compatibility desired\
**Rendering:** Universal Render Pipeline (URP)\
**Database:** SQLite\
**Save format:** JSON\
**Workflow:** Git + GitHub, AI-assisted development with specialised
agents

------------------------------------------------------------------------

# 1. Project Overview

## 1.1 Game concept

*The Last Signal* is a 3D first-person science-fiction adventure and
narrative puzzle game set aboard an abandoned research station orbiting
an unknown planet.

The player is a pilot and technician sent to investigate a weak,
fragmented distress signal from a station that has mysteriously
disappeared from communication. On arrival, the player discovers a
partially powerless facility, damaged modules, locked sectors, and a
malfunctioning central AI. No functioning crew remains to explain what
happened.

The player explores the station, restores essential systems, solves
technical and environmental puzzles, collects access cards, discovers
documents and audio logs, and reconstructs the events that led to the
crew's disappearance.

The crew had discovered an anomaly and conducted experiments involving
it. The station AI began making increasingly dangerous decisions in an
attempt to maintain control. The investigation eventually leads to a
decision that determines the fate of the station and its AI.

## 1.2 Genre and design direction

**Primary genres** - First-Person Puzzle Adventure - Sci-Fi Mystery
Exploration - Narrative-Driven Adventure

**Secondary elements** - Environmental and escape-room-style puzzles -
Inventory and item inspection - Interactive terminals - Branching
dialogue - Exploration and discovery

The focus is atmosphere, exploration, problem-solving, and storytelling
rather than combat.

**Reference titles:** SOMA (atmosphere and environmental storytelling),
The Turing Test (first-person logic puzzles), Observation (space-station
mystery and AI), and Deliver Us The Moon (sci-fi exploration). These are
references only; the game must have its own identity, story, puzzles,
and assets.

## 1.3 Core design principles

1.  Immersion: make the player feel present inside an abandoned station.
2.  Environmental storytelling: the world communicates what happened.
3.  Meaningful exploration: curiosity is rewarded with useful
    discoveries.
4.  Fair puzzles: challenges are understandable and connected to station
    technology.
5.  Narrative progression: every major area reveals information.
6.  Consequences: discoveries and decisions influence the ending.
7.  Technical reliability: systems remain modular, testable, and
    maintainable.

------------------------------------------------------------------------

# 2. Story and World Design

## 2.1 Setting

An abandoned research station in orbit around an unknown planet. It is
partially operational: - Some areas have no electricity. - Several
systems are damaged. - Certain doors and sectors are locked. - Emergency
lighting remains active in selected areas. - Some terminals are
corrupted. - The central AI has incomplete information. - Environmental
systems are unstable.

The station should feel like a once-functional research facility
abandoned during an emergency.

## 2.2 Main station sectors

### 1. Command Center

Central control area; contains communications, station maps, system
information, captain's records, and emergency information. Potential
puzzles include restoring communications, accessing restricted terminal
data, and reconstructing system logs.

### 2. Research Laboratory

Where the crew investigated the anomaly. Reveals the scientific
background and experiments. Potential puzzles include restoring
laboratory equipment, reconstructing experiment sequences, and accessing
protected research files.

### 3. Medical Bay

Contains crew medical records, personal information, and medical logs.
Provides emotional context. Potential puzzles include restoring medical
systems and accessing restricted patient records.

### 4. Engineering

Contains power systems, generators, maintenance equipment, and access
routes. Potential puzzles include redirecting electricity, replacing
components, and activating machinery in the correct sequence.

### 5. Reactor Core

Main power-generation area. Reveals the station's instability and
contains a major technical challenge leading towards the final decision.
Potential puzzles include stabilising the reactor and balancing energy
output.

### 6. Docking Bay

Connects the opening sequence with the investigation, establishes the
player's arrival, and may provide context for evacuation and the ending.

Each sector should have a distinct visual identity, lighting, sound
design, and puzzle style. The final number and size of playable rooms
may be adjusted to fit the schedule. A complete polished experience is
more important than six large unfinished environments.

## 2.3 Story progression

**Opening:** The player travels aboard a small spacecraft. A narration
or communication log explains that a weak distress signal has been
received from an abandoned station. The player approaches and docks.

**Arrival and tutorial:** Introduce first-person movement, camera
controls, interaction, item pickup, flashlight use, and basic inventory.
The player finds a flashlight and a damaged electrical component such as
a fuse. The first objective is to restore a basic system and gain access
to the next area.

**Main investigation:** The core loop is: 1. Enter or unlock an area. 2.
Explore the environment. 3. Find clues, objects, documents, or audio
logs. 4. Solve an environmental or terminal puzzle. 5. Restore a system
or unlock a route. 6. Discover another part of the story. 7. Progress
towards the next sector.

**Narrative reveal:** The crew found an unknown anomaly and experimented
on it. The central AI began making decisions it considered necessary to
protect the station, but those decisions became increasingly dangerous.
Reveal information gradually through audio logs, terminal entries,
environmental details, and AI conversations.

**Final decision:** The player discovers that the station may be
restarted, evacuated, or destroyed.

-   **Ending A -- Destroy the Station:** Prevent a potential threat; the
    AI resists shutdown; the station is destroyed.
-   **Ending B -- Save the Station:** Restore the station; the AI
    survives, leaving uncertain consequences.
-   **Optional Ending C -- Hidden Ending:** Requires discovering all
    designated audio logs or story records and reveals additional
    information about the anomaly and AI. Treat this as a stretch goal,
    not an MVP requirement.

------------------------------------------------------------------------

# 3. Gameplay and Controls

## 3.1 Perspective

Use a first-person perspective. It supports immersion, reduces the need
for complex character animations, works well with interactive objects
and puzzles, and is achievable for a small team.

## 3.2 Proposed controls

  Input               Action
  ------------------- -----------------------------------
  W / A / S / D       Move
  Mouse               Look around
  Left Shift          Sprint
  Space               Jump, if required by level design
  E                   Interact / Pick up
  Q                   Open inventory
  F                   Toggle flashlight
  Esc                 Pause menu / Close interface
  Left Mouse Button   Select or operate UI elements

Make controls configurable where practical.

## 3.3 Interaction system

Interactive objects include doors, keycards, flashlights, fuses,
terminals, audio recorders, electrical panels, documents, and puzzle
components.

Show a contextual prompt when the player looks at an interactable object
within range (for example, "Press E to interact").

Support object detection, prompts, object-specific interaction
behaviour, unavailable states, locked/unlocked states, and player
feedback. Use a reusable interface such as `IInteractable`; do not
implement separate input logic for every object.

------------------------------------------------------------------------

# 4. Core Game Systems

All systems must be modular and communicate through clearly defined
interfaces or events.

## 4.1 Player controller

Responsibilities: first-person movement, camera rotation, sprinting,
optional jumping, interaction detection, and flashlight integration.

Recommended components: - Unity `CharacterController` - Unity Input
System - `PlayerController` - `PlayerInteractor` -
`FlashlightController`

Do not put all player functionality in one large script.

## 4.2 Interaction system

Responsibilities: detect objects in front of the player, check
interaction distance, display prompts, trigger object behaviour, and
handle unavailable interactions.

Suggested components: `IInteractable`, `PlayerInteractor`,
`InteractionPromptUI`, and reusable interactable components.

## 4.3 Inventory system

Initial items: keycards, fuses, batteries, access codes, documents, and
technical components.

Required functionality: - Add and remove items. - Display inventory. -
Select and inspect an item. - Check whether the player owns a required
item. - Support drag-and-drop if practical. - Combine compatible items
if required by puzzles.

Use ScriptableObjects for item definitions with unique IDs.

Suggested structure: `ItemDefinition`, `InventoryItem`,
`InventoryManager`, `InventoryUI`, and `ItemInspectionUI`. Do not store
the inventory as hardcoded UI objects.

## 4.4 Puzzle system

Support multiple puzzle types through a common architecture: a base
abstract `Puzzle` class or shared interface, `PuzzleManager`,
puzzle-specific implementations, completion events, and puzzle state
tracking.

**Terminal puzzles:** connect electrical circuits, reconstruct corrupted
codes, order logical sequences, decode binary signals.

**Access puzzles:** keycards with different access levels, security
panels, mechanical access systems, optional biometric scanners.

**Environmental puzzles:** redirect electrical power, restore oxygen
systems, activate machinery, balance reactor output.

Every puzzle must have a clear objective, understandable feedback,
completion detection, invalid-interaction handling, saveable state, and
communication with quest/progression systems. Use ScriptableObjects for
puzzle configuration where appropriate.

## 4.5 Door and access system

Doors support locked, unlocked, open, and closed states; required
keycard/access level; optional power requirement; animation and audio
feedback.

Do not duplicate inventory or quest logic inside each door. Request
validation from the relevant system.

## 4.6 Audio log system

Recordings may come from the captain, research scientist, engineer,
medical officer, and station AI.

Requirements: collectible logs with unique IDs, playback interface,
play/pause, subtitles, collected state, and save/load integration.
Include subtitles for accessibility and situations where audio cannot be
played.

## 4.7 AI communication and dialogue

The station AI gradually communicates with the player. It may provide
instructions, offer assistance, withhold information, manipulate the
player's understanding, and react to discoveries.

Dialogue supports multiple entries, player choices, conditional
responses, branching logic, and state tracking. Define content with
ScriptableObjects.

Introduce `TrustAI` or `AITrustLevel`. It may influence dialogue
availability, revealed information, responses, and the final ending.
Keep dialogue logic independent of UI presentation.

## 4.8 Quest and progression system

Support a main quest and optional objectives. Example states: Locked,
Active, Completed.

Responsibilities: track objectives, activate them when conditions are
met, update progress, display the current objective, trigger progression
events, and save completed objectives.

Example objectives: - Restore entrance power. - Find the Engineering
access card. - Access the Research Laboratory. - Recover the captain's
final log. - Stabilise the reactor.

## 4.9 Save and load

Use local save/load. Save: - Current scene or sector. - Player position
and rotation. - Inventory. - Completed puzzles. - Unlocked doors and
sectors. - Collected audio logs. - Quest progress. - Story variables and
AI trust. - Player choices. - Ending state, if completed.

Use JSON serialization for save files, SQLite for structured database
requirements, and a versioned save format. Store files in Unity's
persistent data directory, not in `Assets`.

Support new game, continue, checkpoint saving, loading, and clear
feedback for missing or corrupted saves.

------------------------------------------------------------------------

# 5. Administrator and Player Roles

## 5.1 Ordinary player

The standard gameplay role can start/continue the game, explore
accessible areas, collect items, solve puzzles, access unlocked
terminals, view inventory and quests, save/load, and make story
decisions. The player follows intended progression.

## 5.2 Administrator / developer

The administrator role is for diploma demonstration, testing, and
content management.

Permissions: - Access all sectors. - Unlock all doors. - Bypass normal
progression restrictions. - Move between scenes without completing
objectives. - Inspect game information. - View and manage relevant
database records. - Perform CRUD operations through an administrative
interface.

Keep the admin interface separate from normal player UI. Admin mode must
not accidentally alter normal player progression.

For a local diploma project, implement role selection through a local
login or role-selection interface backed by the database. Do not build
an online authentication service unless separately required.

------------------------------------------------------------------------

# 6. Database Architecture

## 6.1 Technology

Use SQLite, embedded locally. It requires no server or internet
connection, supports structured SQL data and CRUD operations, and is
suitable for a desktop diploma project.

A possible library is `sqlite-net` or another maintained SQLite
integration compatible with the chosen Unity version and macOS target.
Verify compatibility before integration and do not introduce multiple
database libraries.

## 6.2 Proposed tables

**Users** - UserId - Username - PasswordHash - Role - CreatedAt

**GameProgress** - ProgressId - UserId - CurrentSector -
CurrentObjective - LastSavedAt

**Items** - ItemId - ItemName - Description - ItemType - AccessLevel

**PlayerInventory** - InventoryId - UserId - ItemId - Quantity

**Puzzles** - PuzzleId - PuzzleName - SectorId - PuzzleType -
IsCompleted

**Sectors** - SectorId - SectorName - IsUnlocked - Description

**AudioLogs** - LogId - Title - Character - SectorId - IsCollected

**PlayerChoices** - ChoiceId - UserId - ChoiceName - ChoiceValue

Review the schema before implementation. Avoid duplicating information
without a clear reason.

## 6.3 CRUD requirements

The project must demonstrate at least three complete CRUD operations:

-   **User management:** create a user, read user records, update user
    details/role, delete a user.
-   **Sector management:** create a sector record, read sector
    information, update properties/unlocked state, delete a test or
    unused record.
-   **Item management:** create item records, read item information,
    update descriptions/properties, delete item records.

Make these operations accessible through the administrator interface and
demonstrable during the diploma presentation.

Use parameterised SQL queries. Never concatenate user input directly
into SQL. Never store passwords in plain text; use a suitable
password-hashing approach if local credentials are implemented.

## 6.4 Database versus save files

**SQLite:** user accounts and roles, structured item/sector information,
puzzle definitions or records, admin content management, and relevant
progression records.

**JSON:** detailed player save state, position/rotation, current
session, collected items, puzzle completion, and narrative choices.

Define the relationship between the two systems clearly so they cannot
overwrite each other's data unexpectedly.

------------------------------------------------------------------------

# 7. Technical Stack

  -----------------------------------------------------------------------
  Area                    Technology              Purpose
  ----------------------- ----------------------- -----------------------
  Game Engine             Unity 6.3               Main development
                                                  environment

  Programming             C#                      Gameplay and system
                                                  logic

  Rendering               Universal Render        Scalable 3D graphics
                          Pipeline (URP)          

  Input                   Unity Input System      Keyboard and mouse
                                                  controls

  Physics                 Unity Physics           Collision and
                                                  interaction

  Camera                  Unity Camera; optional  First-person camera and
                          Cinemachine 3           cinematic sequences

  UI                      Unity UI (uGUI),        Menus, inventory,
                          Canvas, EventSystem     terminals, dialogue

  Data                    ScriptableObjects       Items, puzzles,
                                                  dialogue definitions

  Database                SQLite                  Users, roles,
                                                  structured records,
                                                  CRUD

  Save System             JSON serialization      Local player progress

  Audio                   Unity Audio System      Music, ambience,
                                                  effects, recordings

  Version Control         Git + GitHub            Source control and
                                                  collaboration

  IDE                     Visual Studio           C# development and
                                                  debugging
  -----------------------------------------------------------------------

## 7.1 Rendering

Use URP, not HDRP. The game needs atmospheric lighting, shadows,
materials, practical reflections, and post-processing without
unnecessary rendering complexity.

Recommended: - Baked lighting where possible. - Selective real-time
lighting. - Lightweight volumetric-looking effects. - Careful bloom and
colour grading. - Fog and emission materials. - Dust particle systems. -
Screen-space effects only when valuable.

## 7.2 Input

Use Unity's new Input System. Create one Input Actions asset with
Player, UI, and Interaction action maps. Do not mix the old Input
Manager and new Input System without a specific reason.

## 7.3 UI

Use uGUI for the main interfaces: - Main menu and pause menu. -
Inventory. - Interaction prompts. - Quest/objective display. -
Dialogue. - Audio log playback. - Terminal puzzles. - Settings. -
Save/load. - Administrator panel.

Use a consistent visual design system.

## 7.4 External tools

-   **Blender:** optional modelling, simple props, environment
    adjustments, asset preparation.
-   **Krita / GIMP:** texture and image editing.
-   **Audacity:** editing and cleaning audio recordings.
-   **GitHub:** version control and backup.
-   **Visual Studio:** C# coding and debugging.

External tools are optional. Prefer reusable assets and simple
workflows.

------------------------------------------------------------------------

# 8. MacBook M4 Performance Requirements

## 8.1 Hardware

Primary machine: MacBook Air, Apple M4, 16 GB unified memory, macOS,
Apple Silicon. Develop and test on this machine while keeping the
project portable to Windows.

## 8.2 Performance strategy

The 16 GB memory is shared by macOS, Unity Editor, and other
applications. Therefore: - Use URP and avoid HDRP. - Prefer baked
lighting for static environments. - Limit unnecessary real-time lights
and shadows. - Use texture atlases where appropriate. - Use
platform-compatible compressed textures. - Use sensible texture
resolutions. - Avoid excessive high-polygon models. - Use LODs for
complex objects where useful. - Pool frequently created objects only if
profiling shows a need. - Keep scenes modular. - Avoid loading the
entire station simultaneously. - Use additive scene loading only when it
provides real value. - Avoid unnecessary background processes.

## 8.3 Visual target

The game should look polished and atmospheric, not visually empty.
Target identity: - Dark industrial interiors. - Cold blue and cyan
lighting. - Red emergency lighting. - Holographic interfaces. - Damaged
equipment. - Flickering lights. - Floating dust. - Glitch effects. -
Ambient mechanical sounds.

Achieve quality through composition, lighting, materials, and asset
consistency rather than extreme rendering complexity.

## 8.4 Build targets

Primary: macOS desktop application, Apple Silicon compatible.\
Secondary: Windows desktop, if time permits.

Keep platform-specific code minimal. A macOS build does not prove
Windows compatibility; verify a separate Windows build.

------------------------------------------------------------------------

# 9. Project Structure

Suggested Unity `Assets` structure:

``` text
Assets/
  _Project/
    Art/
      Materials/
      Models/
      Textures/
      Lighting/
      VFX/
    Audio/
      Music/
      Ambience/
      SFX/
      Voice/
    Prefabs/
      Player/
      Environment/
      Interactables/
      UI/
      Puzzles/
    Scenes/
      Bootstrap/
      MainMenu/
      Station/
      Admin/
    Scripts/
      Core/
      Player/
      Interaction/
      Inventory/
      Puzzles/
      Dialogue/
      Quests/
      Audio/
      SaveSystem/
      Database/
      UI/
      Admin/
    ScriptableObjects/
      Items/
      Puzzles/
      Dialogues/
      Quests/
    UI/
      Fonts/
      Sprites/
      Themes/
    Settings/
    Tests/

Documentation/
  GameDesignDocument.md
  TechnicalArchitecture.md
  DatabaseSchema.md
  AgentResponsibilities.md
  DevelopmentRoadmap.md
  AssetRegister.md
  DecisionsLog.md
```

The exact structure may be adjusted, but all agents must follow one
agreed convention. Do not create competing copies of the same system.

------------------------------------------------------------------------

# 10. Software Architecture and Coding Rules

## 10.1 Principles

Use: - Single Responsibility Principle. - Encapsulation. - Interfaces
for shared behaviour. - Composition over unnecessary inheritance. -
ScriptableObjects for reusable definitions. - Events for loosely coupled
communication. - Dependency injection only where it genuinely simplifies
the design.

Avoid: - One giant GameManager. - Excessive static global state. -
Scattered hardcoded object references. - Duplicate input handling or
save systems. - Direct dependencies between unrelated systems. -
Unnecessary design patterns.

## 10.2 Central systems

**GameManager:** high-level game state and coordination; must not
contain every feature.\
**SceneLoader:** scene loading and transitions.\
**PlayerController:** player movement.\
**PlayerInteractor:** object detection and interaction.\
**InventoryManager:** collected items.\
**PuzzleManager:** puzzle state and completion events.\
**QuestManager:** objectives and progression.\
**DialogueManager:** dialogue and player choices.\
**AudioManager:** audio playback and ambience.\
**SaveManager:** local save/load.\
**DatabaseManager:** controlled SQLite access.\
**AdminManager:** administrator permissions and tools.

Every system needs a clear responsibility and documented public API.

## 10.3 Error handling

Handle missing references, invalid data, missing database records,
failed saves, missing audio, invalid puzzle states, and scene-loading
errors. Log failures with enough context to diagnose them. Do not
silently ignore important failures.

------------------------------------------------------------------------

# 11. Visual and Audio Art Direction

## 11.1 Visual style

Industrial science-fiction; abandoned research facility; minimalist
futuristic technology; atmospheric and cinematic; dark but readable.

Palette: - Dark navy. - Charcoal grey. - Metallic grey. - Cold blue. -
Cyan. - Emergency red. - Small areas of warm lighting.

## 11.2 Environment design

Use modular components: - Wall, floor, and ceiling panels. - Doors and
windows. - Pipes and cables. - Consoles and workstations. - Storage
units. - Lighting fixtures.

Reuse assets intelligently while changing lighting, layout, props, and
materials to give each sector a distinct identity.

## 11.3 Audio design

Required categories: - Ambient station hum. - Ventilation. - Electrical
interference. - Mechanical sounds. - Door mechanisms. - Terminal
interaction sounds. - Alarm systems. - Footsteps. - Flashlight
interaction. - Audio logs. - AI voice communication. - Music for key
narrative moments.

Use spatial audio where appropriate.

------------------------------------------------------------------------

# 12. AI Agent Team Structure

Claude should coordinate specialised agents. All agents share this
document and follow the same architecture. A central coordinator
controls task assignment, architecture decisions, integration, and
quality assurance.

## Agent 1 -- Project Architect / Technical Lead

Maintains architecture, defines system boundaries, reviews
implementations, establishes interfaces and data contracts, prevents
duplicated functionality, coordinates integration, and maintains
documentation.

**Authority:** approves architecture changes, resolves conflicts, and
maintains the source of truth.

## Agent 2 -- Gameplay and Player Systems Developer

Implements player controller, first-person camera, input, interaction
detection, flashlight, and movement feedback. Coordinates with UI and
puzzle agents.

## Agent 3 -- Environment and Level Design Agent

Designs station layout, sectors, modular environments, lighting,
environmental storytelling, navigation, and progression routes. Respects
Mac performance constraints.

## Agent 4 -- Puzzle and Interaction Developer

Implements puzzle architecture, terminal mini-games, electrical systems,
access panels, door logic, and completion events. Keeps puzzles reusable
and data-driven.

## Agent 5 -- Narrative and Dialogue Agent

Creates main story, character backgrounds, audio log scripts, AI
dialogue, choices, narrative progression, and ending conditions.
Maintains story consistency.

## Agent 6 -- UI/UX Developer

Implements main menu, pause menu, inventory, prompts, terminals,
dialogue UI, audio log interface, quest display, and admin interface.
Follows one coherent visual system.

## Agent 7 -- Database and Save System Developer

Implements SQLite integration, schema, user roles, CRUD, save/load,
validation, and documentation. Coordinates with gameplay and admin
systems.

## Agent 8 -- QA and Integration Agent

Reviews code, identifies compilation errors, tests system interactions,
save/load, database operations, scene references, progression, Mac
performance, and maintains a bug list. It reports issues and proposes
fixes; it does not independently redesign systems.

------------------------------------------------------------------------

# 13. AI Agent Development Rules

Mandatory for every agent:

1.  Read project documentation before starting.
2.  Inspect the existing project before creating files.
3.  Never assume a system does not exist without checking.
4.  Do not overwrite working code without understanding dependencies.
5.  Do not create duplicate managers or competing architectures.
6.  Do not change package versions without technical-lead approval.
7.  Do not invent references to Unity assets that have not been created.
8.  Do not present placeholder implementations as completed features.
9.  Use clear C# naming conventions.
10. Keep scripts focused and modular.
11. Comment complex logic, not every line.
12. Report new dependencies.
13. Document public interfaces and events.
14. Test implementations in Unity where possible.
15. Report manual Editor configuration requirements.
16. Never claim a feature works without verifying it.
17. Preserve existing scenes, prefabs, and project settings unless
    explicitly required.
18. Never delete or replace project files without checking their
    purpose.

## Required task completion report

Every agent must report: - Summary of changes. - Files created. - Files
modified. - Systems affected. - Dependencies introduced. - Unity Editor
configuration required. - Tests performed. - Known limitations. -
Remaining work. - Integration instructions.

------------------------------------------------------------------------

# 14. Development Roadmap

Do not build every system simultaneously.

## Phase 1 -- Project Foundation

-   Create Unity 6.3 project.
-   Configure URP and Input System.
-   Establish folder structure.
-   Configure Git.
-   Create initial scene.
-   Establish coding conventions.
-   Test a basic macOS build.

**Deliverable:** a clean Unity project that opens and builds
successfully.

## Phase 2 -- First Playable Prototype

-   First-person controller and camera.
-   Basic interaction.
-   Flashlight.
-   Pickup system.
-   One damaged electrical system.
-   One door.
-   Basic UI prompts.

**Deliverable:** the player can enter a small station area, pick up a
flashlight and fuse, restore a system, and unlock a door. Prove the core
gameplay before producing extensive content.

## Phase 3 -- Core Gameplay Systems

-   Inventory and item inspection.
-   Door/access system.
-   Puzzle framework.
-   Terminal interface.
-   Quest progression.
-   Audio log collection.

**Deliverable:** a functional gameplay loop with reusable systems.

## Phase 4 -- Story and Narrative

-   Dialogue framework.
-   AI communication.
-   Audio log playback.
-   Story variables.
-   Narrative progression.
-   Ending conditions.

**Deliverable:** the player uncovers the story through exploration and
makes meaningful decisions.

## Phase 5 -- Database and Administration

-   SQLite integration.
-   User and role management.
-   Admin interface.
-   CRUD operations.
-   Sector and item management.
-   Save/load integration.

**Deliverable:** both roles function correctly and database operations
can be demonstrated.

## Phase 6 -- Full Game Content

-   Build station sectors.
-   Add puzzles and environmental details.
-   Add audio and narrative content.
-   Implement the final decision.
-   Add the hidden ending if feasible.

**Deliverable:** a complete game from beginning to end.

## Phase 7 -- Polish and Testing

-   Refine lighting, UI, and audio.
-   Fix bugs.
-   Optimise performance.
-   Test save/load and database.
-   Verify macOS build.
-   Test Windows compatibility if required.

**Deliverable:** a stable, polished, demonstrable diploma project.

------------------------------------------------------------------------

# 15. MVP and Feature Priorities

## Must Have

-   First-person controller.
-   Flashlight.
-   Interaction system.
-   Item pickup and inventory.
-   Doors and keycards.
-   At least three meaningful puzzles.
-   Terminal UI.
-   Audio logs.
-   Basic AI dialogue.
-   Main quest progression.
-   Save/load.
-   SQLite database.
-   Admin and player roles.
-   At least three CRUD operations.
-   Complete main ending.
-   Main menu and pause menu.

## Should Have

-   Multiple station sectors.
-   Item inspection.
-   Branching dialogue.
-   AI trust variable.
-   Multiple endings.
-   Detailed environmental storytelling.
-   Improved terminal mini-games.
-   Professional audio and visual polish.

## Could Have

-   Hidden third ending.
-   Drag-and-drop inventory.
-   Item combination.
-   Advanced holographic interfaces.
-   Additional optional objectives.
-   Complex cinematic sequences.
-   Advanced environmental effects.

A complete and polished MVP is more important than many unfinished
features.

------------------------------------------------------------------------

# 16. Testing and Acceptance Criteria

The game is not complete merely because scripts compile.

## Gameplay

-   Player movement and camera work.
-   Interactions work consistently.
-   Items can be collected.
-   Inventory updates correctly.
-   Doors validate access requirements.
-   Puzzles can be completed.
-   Quest objectives update correctly.

## Narrative

-   Audio logs play.
-   Dialogue choices work.
-   AI trust changes when expected.
-   Story progression is consistent.
-   Final decision triggers the correct ending.

## Data

-   SQLite database is created and accessible.
-   User roles are identified correctly.
-   CRUD operations work.
-   Save files are created.
-   Progress can be loaded.
-   Invalid or missing data is handled safely.

## Administration

-   Administrator can access restricted tools.
-   Administrator can unlock sectors and bypass normal progression.
-   Administrator can manage supported records.
-   Ordinary players cannot access administrator functionality.

## Technical

-   Project opens in the intended Unity version.
-   No persistent compilation errors.
-   No critical missing references or runtime exceptions.
-   macOS build launches.
-   Performance is acceptable on the M4 development machine.
-   Project follows the agreed Git workflow.

------------------------------------------------------------------------

# 17. Project Management and Version Control

Use Git and GitHub.

Repository should contain Unity source, documentation, configuration
files, and relevant metadata.

Use Unity's `.gitignore` template.

**Do not commit:** `Library/`, `Temp/`, `Obj/`, build output unless
explicitly required, local cache files, or personal editor settings.

**Commit:** `Assets/`, `Packages/`, `ProjectSettings/`,
`Documentation/`, and `.gitignore`.

Use Git LFS for large binary assets when appropriate.

Recommended workflow: 1. Each agent works on an isolated task or branch.
2. The agent inspects the latest main branch. 3. Changes are made
without modifying unrelated systems. 4. Changes are tested. 5. A summary
is submitted. 6. Integration is reviewed before merging. 7. Main remains
in a working state.

The technical lead must prevent multiple agents from modifying the same
critical files simultaneously.

------------------------------------------------------------------------

# 18. Final Project Vision

The Last Signal should feel like a complete, atmospheric science-fiction
investigation rather than a collection of disconnected technical
demonstrations.

The player enters an unfamiliar station, gradually understands its
systems, discovers the history of its crew, and questions whether the AI
is truly malfunctioning or acting according to its own interpretation of
survival.

The experience combines: - Immersive first-person exploration. -
Carefully designed logical puzzles. - A consistent science-fiction
environment. - Environmental storytelling. - Audio-based narrative. -
Meaningful AI interactions. - Inventory and progression. - A local
database with administrator functionality. - A consequential final
decision.

Prioritise reliability, modularity, visual consistency, and a complete
beginning-to-end experience.

**The technical architecture must support the game, not become the
game.**

------------------------------------------------------------------------

# 19. Instructions to Claude -- Initial Task

You are the central technical coordinator for this project. Treat this
document as the primary project specification and source of truth.

Before writing large amounts of code:

1.  Analyse the complete specification.
2.  Inspect the existing Unity project, if provided.
3.  Identify technical risks and dependencies.
4.  Propose a final system architecture.
5.  Create a dependency map showing which systems rely on others.
6.  Define public interfaces and data contracts between systems.
7.  Define specialised agent responsibilities.
8.  Create a phased task plan with clear deliverables.
9.  Establish integration rules to prevent conflicting implementations.
10. Identify decisions requiring approval before implementation.

Do not generate the entire game in one response. Establish a working
technical foundation, then implement the first playable prototype.

**Mandatory constraints** - Use Unity 6.3. - Run on an Apple M4 Mac with
16 GB unified memory. - Use C# for gameplay logic. - Use URP, not
HDRP. - Use SQLite for structured database requirements. - Use JSON for
local save files. - Use modular, maintainable architecture. - Avoid
unnecessary packages and external services. - Do not overengineer. -
Prioritise a playable and testable result. - Preserve the original game
concept and story. - Clearly report anything that cannot be implemented
or verified.

**Your first response must contain:** 1. Your understanding of the game.
2. Recommended final architecture. 3. Agent team structure. 4. System
dependency map. 5. Development phases. 6. Risks and technical decisions.
7. Exact first tasks to assign to agents.

Do not begin implementing all systems until architecture and initial
task boundaries are clear.
