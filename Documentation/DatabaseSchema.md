# Database Schema (draft — implemented in Phase 5)

SQLite file: `Application.persistentDataPath/lastsignal.db`. All access through `DatabaseManager`
repositories with parameterised queries. Schema version stored in `PRAGMA user_version`.

## Review of the spec schema
The spec puts `IsCompleted` on `Puzzles` and `IsCollected` on `AudioLogs`. Those are **per-player**
facts, so storing them on global catalog rows would make every account share progress. Decision:
catalog tables stay global; per-player progress lives in the JSON save; the DB keeps a per-user
summary (`GameProgress`) and the decisions (`PlayerChoices`) for the admin overview.

## Tables
**Users** — `UserId INTEGER PK`, `Username TEXT UNIQUE NOT NULL COLLATE NOCASE`, `PasswordHash TEXT`,
`PasswordSalt TEXT`, `HashIterations INTEGER`, `Role TEXT CHECK(Role IN ('Player','Admin'))`,
`PreferredLanguage TEXT DEFAULT 'en'`, `CreatedAt TEXT`, `LastLoginAt TEXT`.

**Sectors** — `SectorId TEXT PK` (`docking` …), `SectorName TEXT`, `Description TEXT`,
`SceneName TEXT`, `SortOrder INTEGER`, `IsUnlocked INTEGER` (admin demo override).

**Items** — `ItemId TEXT PK`, `ItemName TEXT`, `Description TEXT`, `ItemType TEXT`,
`AccessLevel INTEGER DEFAULT 0`.

**Puzzles** — `PuzzleId TEXT PK`, `PuzzleName TEXT`, `SectorId TEXT FK`, `PuzzleType TEXT`.

**AudioLogs** — `LogId TEXT PK`, `Title TEXT`, `Character TEXT`, `SectorId TEXT FK`, `CountsForHiddenEnding INTEGER`.

**GameProgress** — `ProgressId INTEGER PK`, `UserId FK UNIQUE`, `CurrentSector TEXT`,
`CurrentObjective TEXT`, `LogsCollected INTEGER`, `PlaytimeSeconds INTEGER`, `EndingReached TEXT NULL`, `LastSavedAt TEXT`.

**PlayerInventory** — `InventoryId INTEGER PK`, `UserId FK`, `ItemId FK`, `Quantity INTEGER`
(mirror of the latest save for the admin overview; JSON stays authoritative).

**PlayerChoices** — `ChoiceId INTEGER PK`, `UserId FK`, `ChoiceName TEXT`, `ChoiceValue TEXT`, `MadeAt TEXT`.

## CRUD demonstrated in the admin panel
Users (create/read/update role & password/delete — cannot delete the last admin or yourself),
Sectors (create/read/update/delete test records; built-in sectors protected from delete),
Items (create/read/update/delete).

## Seeding
On first run: create tables, seed Sectors/Items/Puzzles/AudioLogs from the ScriptableObject registry,
then show the "Create administrator" screen.
