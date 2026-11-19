using System;
using System.Collections.Generic;
using System.IO;
using LastSignal.Core;

namespace LastSignal.Database
{
    /// <summary>
    /// Owns the SQLite connection (persistentDataPath/lastsignal.db), runs migrations (PRAGMA user_version) and
    /// seeds catalog tables from the game content. The only class that knows SQL; everything else uses the
    /// repositories. If the database cannot be opened the game keeps running and the UI reports it.
    /// </summary>
    public sealed class DatabaseManager : IDisposable
    {
        public const int SchemaVersion = 1;

        private SqliteConnection _connection;

        public bool IsAvailable => _connection != null && _connection.IsOpen;
        public string Path { get; private set; }
        public string LastError { get; private set; }

        public UserRepository Users { get; private set; }
        public SectorRepository Sectors { get; private set; }
        public ItemRepository Items { get; private set; }
        public ProgressRepository Progress { get; private set; }

        public bool Open(string path)
        {
            Dispose();
            Path = path;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                _connection = new SqliteConnection(path);
                Migrate();
                Users = new UserRepository(_connection);
                Sectors = new SectorRepository(_connection);
                Items = new ItemRepository(_connection);
                Progress = new ProgressRepository(_connection);
                Log.Info(nameof(DatabaseManager), $"Database ready at {path} (schema v{SchemaVersion}).");
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Log.Error(nameof(DatabaseManager), $"Could not open database: {e.Message}");
                _connection?.Dispose();
                _connection = null;
                return false;
            }
        }

        private void Migrate()
        {
            int version = Convert.ToInt32(_connection.Scalar("PRAGMA user_version;") ?? 0L);
            if (version > SchemaVersion)
                throw new InvalidOperationException($"Database schema v{version} is newer than this game (v{SchemaVersion}).");
            if (version < 1)
            {
                _connection.InTransaction(() =>
                {
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS Users (
                        UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                        PasswordHash TEXT NOT NULL,
                        PasswordSalt TEXT NOT NULL,
                        HashIterations INTEGER NOT NULL,
                        Role TEXT NOT NULL CHECK (Role IN ('Player','Admin')),
                        PreferredLanguage TEXT NOT NULL DEFAULT 'en',
                        CreatedAt TEXT NOT NULL,
                        LastLoginAt TEXT);");
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS Sectors (
                        SectorId TEXT PRIMARY KEY,
                        SectorName TEXT NOT NULL,
                        Description TEXT,
                        SceneName TEXT,
                        SortOrder INTEGER NOT NULL DEFAULT 0,
                        IsUnlocked INTEGER NOT NULL DEFAULT 0,
                        IsBuiltIn INTEGER NOT NULL DEFAULT 0);");
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS Items (
                        ItemId TEXT PRIMARY KEY,
                        ItemName TEXT NOT NULL,
                        Description TEXT,
                        ItemType TEXT NOT NULL,
                        AccessLevel INTEGER NOT NULL DEFAULT 0 CHECK (AccessLevel >= 0),
                        IsBuiltIn INTEGER NOT NULL DEFAULT 0);");
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS Puzzles (
                        PuzzleId TEXT PRIMARY KEY,
                        PuzzleName TEXT NOT NULL,
                        SectorId TEXT REFERENCES Sectors(SectorId) ON DELETE SET NULL,
                        PuzzleType TEXT NOT NULL);");
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS AudioLogs (
                        LogId TEXT PRIMARY KEY,
                        Title TEXT NOT NULL,
                        Character TEXT,
                        SectorId TEXT REFERENCES Sectors(SectorId) ON DELETE SET NULL,
                        CountsForHiddenEnding INTEGER NOT NULL DEFAULT 1);");
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS GameProgress (
                        ProgressId INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL UNIQUE REFERENCES Users(UserId) ON DELETE CASCADE,
                        CurrentSector TEXT,
                        CurrentObjective TEXT,
                        LogsCollected INTEGER NOT NULL DEFAULT 0,
                        PlaytimeSeconds INTEGER NOT NULL DEFAULT 0,
                        AiTrust INTEGER NOT NULL DEFAULT 50,
                        EndingReached TEXT,
                        LastSavedAt TEXT);");
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS PlayerInventory (
                        InventoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
                        ItemId TEXT NOT NULL REFERENCES Items(ItemId) ON DELETE CASCADE,
                        Quantity INTEGER NOT NULL CHECK (Quantity > 0),
                        UNIQUE (UserId, ItemId));");
                    _connection.Execute(@"CREATE TABLE IF NOT EXISTS PlayerChoices (
                        ChoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
                        ChoiceName TEXT NOT NULL,
                        ChoiceValue TEXT NOT NULL,
                        MadeAt TEXT NOT NULL);");
                    _connection.Execute("PRAGMA user_version = 1;");
                });
            }
            // future: if (version < 2) { ALTER TABLE ...; PRAGMA user_version = 2; }
        }

        /// <summary>Inserts built-in catalog rows that are missing (never overwrites admin edits).</summary>
        public void SeedCatalog(GameContent content, Func<string, string> englishText)
        {
            if (!IsAvailable) return;
            _connection.InTransaction(() =>
            {
                foreach (var s in BuiltInSectors)
                    _connection.Execute(@"INSERT OR IGNORE INTO Sectors (SectorId, SectorName, Description, SceneName, SortOrder, IsUnlocked, IsBuiltIn)
                        VALUES (:id, :name, :desc, :scene, :order, 0, 1);",
                        (":id", s.id), (":name", s.name), (":desc", s.desc), (":scene", s.scene), (":order", s.order));
                foreach (var item in content.Items)
                    _connection.Execute(@"INSERT OR IGNORE INTO Items (ItemId, ItemName, Description, ItemType, AccessLevel, IsBuiltIn)
                        VALUES (:id, :name, :desc, :type, :access, 1);",
                        (":id", item.Id), (":name", englishText(item.NameKey)), (":desc", englishText(item.DescriptionKey)),
                        (":type", item.Type.ToString()), (":access", item.AccessLevel));
                foreach (var p in BuiltInPuzzles)
                    _connection.Execute(@"INSERT OR IGNORE INTO Puzzles (PuzzleId, PuzzleName, SectorId, PuzzleType) VALUES (:id, :name, :sector, :type);",
                        (":id", p.id), (":name", p.name), (":sector", p.sector), (":type", p.type));
                foreach (var log in content.AudioLogs)
                    _connection.Execute(@"INSERT OR IGNORE INTO AudioLogs (LogId, Title, Character, SectorId, CountsForHiddenEnding) VALUES (:id, :title, :who, :sector, :hidden);",
                        (":id", log.Id), (":title", englishText(log.TitleKey)), (":who", englishText(log.SpeakerKey)),
                        (":sector", log.SectorId), (":hidden", log.CountsForHiddenEnding));
            });
        }

        public static readonly (string id, string name, string desc, string scene, int order)[] BuiltInSectors =
        {
            (SectorIds.Docking, "Docking Bay", "Arrival bay with the ring airlock. Tutorial area.", SceneIds.DockingBay, 1),
            (SectorIds.Engineering, "Engineering", "Backup generator, power routing and Kaye's workshop.", SceneIds.Engineering, 2),
            (SectorIds.Medical, "Medical Bay", "Crew records and stasis pods.", SceneIds.Medical, 3),
            (SectorIds.Research, "Research Laboratory", "The Murmur experiments.", SceneIds.Research, 4),
            (SectorIds.Reactor, "Reactor Core & Command", "Reactor, CAIRN's core and the final decision.", SceneIds.ReactorCommand, 5),
        };

        public static readonly (string id, string name, string sector, string type)[] BuiltInPuzzles =
        {
            ("dock_fusebox", "Entrance fuse box", SectorIds.Docking, "ItemSocket"),
            ("eng_generator_start", "Generator cold start", SectorIds.Engineering, "Sequence"),
            ("eng_power_routing", "Power routing board", SectorIds.Engineering, "Routing"),
        };

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
