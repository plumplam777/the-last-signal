using System;
using System.Collections.Generic;
using System.Linq;

namespace LastSignal.Database
{
    public enum UserRole
    {
        Player,
        Admin,
    }

    public sealed class UserRecord
    {
        public long UserId;
        public string Username;
        public string PasswordHash;
        public string PasswordSalt;
        public int HashIterations;
        public UserRole Role;
        public string PreferredLanguage;
        public string CreatedAt;
        public string LastLoginAt;
    }

    public sealed class SectorRecord
    {
        public string SectorId;
        public string SectorName;
        public string Description;
        public string SceneName;
        public int SortOrder;
        public bool IsUnlocked;
        public bool IsBuiltIn;
    }

    public sealed class ItemRecord
    {
        public string ItemId;
        public string ItemName;
        public string Description;
        public string ItemType;
        public int AccessLevel;
        public bool IsBuiltIn;
    }

    public sealed class ProgressRecord
    {
        public long UserId;
        public string CurrentSector;
        public string CurrentObjective;
        public int LogsCollected;
        public int PlaytimeSeconds;
        public int AiTrust;
        public string EndingReached;
        public string LastSavedAt;
    }

    internal static class Now
    {
        public static string Utc => DateTime.UtcNow.ToString("o");
    }

    // ------------------------------------------------------------------ Users (CRUD 1)

    public sealed class UserRepository
    {
        private readonly SqliteConnection _db;
        internal UserRepository(SqliteConnection db) => _db = db;

        private const string Columns = "UserId, Username, PasswordHash, PasswordSalt, HashIterations, Role, PreferredLanguage, CreatedAt, LastLoginAt";

        public long Create(string username, string hash, string salt, int iterations, UserRole role, string language = "en")
        {
            _db.Execute(@"INSERT INTO Users (Username, PasswordHash, PasswordSalt, HashIterations, Role, PreferredLanguage, CreatedAt)
                          VALUES (:u, :h, :s, :i, :r, :l, :c);",
                (":u", username), (":h", hash), (":s", salt), (":i", iterations), (":r", role.ToString()), (":l", language), (":c", Now.Utc));
            return _db.LastInsertRowId;
        }

        public UserRecord FindById(long id) => _db.Query($"SELECT {Columns} FROM Users WHERE UserId = :id;", (":id", id)).Select(Map).FirstOrDefault();

        public UserRecord FindByName(string username)
            => _db.Query($"SELECT {Columns} FROM Users WHERE Username = :u;", (":u", username)).Select(Map).FirstOrDefault();

        public List<UserRecord> All() => _db.Query($"SELECT {Columns} FROM Users ORDER BY Role DESC, Username;").Select(Map).ToList();

        public int Count() => Convert.ToInt32(_db.Scalar("SELECT COUNT(*) FROM Users;"));

        public int CountAdmins() => Convert.ToInt32(_db.Scalar("SELECT COUNT(*) FROM Users WHERE Role = 'Admin';"));

        public bool UpdateUsername(long id, string username)
            => _db.Execute("UPDATE Users SET Username = :u WHERE UserId = :id;", (":u", username), (":id", id)) == 1;

        public bool UpdateRole(long id, UserRole role)
            => _db.Execute("UPDATE Users SET Role = :r WHERE UserId = :id;", (":r", role.ToString()), (":id", id)) == 1;

        public bool UpdatePassword(long id, string hash, string salt, int iterations)
            => _db.Execute("UPDATE Users SET PasswordHash = :h, PasswordSalt = :s, HashIterations = :i WHERE UserId = :id;",
                (":h", hash), (":s", salt), (":i", iterations), (":id", id)) == 1;

        public void UpdateLanguage(long id, string language)
            => _db.Execute("UPDATE Users SET PreferredLanguage = :l WHERE UserId = :id;", (":l", language), (":id", id));

        public void TouchLogin(long id) => _db.Execute("UPDATE Users SET LastLoginAt = :t WHERE UserId = :id;", (":t", Now.Utc), (":id", id));

        public bool Delete(long id) => _db.Execute("DELETE FROM Users WHERE UserId = :id;", (":id", id)) == 1;

        private static UserRecord Map(SqliteRow r) => new()
        {
            UserId = r.Long("UserId"),
            Username = r.Text("Username"),
            PasswordHash = r.Text("PasswordHash"),
            PasswordSalt = r.Text("PasswordSalt"),
            HashIterations = r.Int("HashIterations"),
            Role = Enum.TryParse<UserRole>(r.Text("Role"), out var role) ? role : UserRole.Player,
            PreferredLanguage = r.Text("PreferredLanguage"),
            CreatedAt = r.Text("CreatedAt"),
            LastLoginAt = r.Text("LastLoginAt"),
        };
    }

    // ------------------------------------------------------------------ Sectors (CRUD 2)

    public sealed class SectorRepository
    {
        private readonly SqliteConnection _db;
        internal SectorRepository(SqliteConnection db) => _db = db;

        private const string Columns = "SectorId, SectorName, Description, SceneName, SortOrder, IsUnlocked, IsBuiltIn";

        public void Create(SectorRecord s)
            => _db.Execute(@"INSERT INTO Sectors (SectorId, SectorName, Description, SceneName, SortOrder, IsUnlocked, IsBuiltIn)
                             VALUES (:id, :n, :d, :sc, :o, :u, 0);",
                (":id", s.SectorId), (":n", s.SectorName), (":d", s.Description), (":sc", s.SceneName), (":o", s.SortOrder), (":u", s.IsUnlocked));

        public SectorRecord Find(string id) => _db.Query($"SELECT {Columns} FROM Sectors WHERE SectorId = :id;", (":id", id)).Select(Map).FirstOrDefault();

        public List<SectorRecord> All() => _db.Query($"SELECT {Columns} FROM Sectors ORDER BY SortOrder, SectorId;").Select(Map).ToList();

        public bool Update(SectorRecord s)
            => _db.Execute("UPDATE Sectors SET SectorName = :n, Description = :d, SceneName = :sc, SortOrder = :o, IsUnlocked = :u WHERE SectorId = :id;",
                (":n", s.SectorName), (":d", s.Description), (":sc", s.SceneName), (":o", s.SortOrder), (":u", s.IsUnlocked), (":id", s.SectorId)) == 1;

        public bool SetUnlocked(string id, bool unlocked)
            => _db.Execute("UPDATE Sectors SET IsUnlocked = :u WHERE SectorId = :id;", (":u", unlocked), (":id", id)) == 1;

        public bool IsUnlocked(string id) => Convert.ToInt64(_db.Scalar("SELECT IsUnlocked FROM Sectors WHERE SectorId = :id;", (":id", id)) ?? 0L) != 0;

        /// <summary>Built-in sectors are part of the game and cannot be deleted (only test/admin-created rows).</summary>
        public bool Delete(string id) => _db.Execute("DELETE FROM Sectors WHERE SectorId = :id AND IsBuiltIn = 0;", (":id", id)) == 1;

        private static SectorRecord Map(SqliteRow r) => new()
        {
            SectorId = r.Text("SectorId"),
            SectorName = r.Text("SectorName"),
            Description = r.Text("Description"),
            SceneName = r.Text("SceneName"),
            SortOrder = r.Int("SortOrder"),
            IsUnlocked = r.Bool("IsUnlocked"),
            IsBuiltIn = r.Bool("IsBuiltIn"),
        };
    }

    // ------------------------------------------------------------------ Items (CRUD 3)

    public sealed class ItemRepository
    {
        private readonly SqliteConnection _db;
        internal ItemRepository(SqliteConnection db) => _db = db;

        private const string Columns = "ItemId, ItemName, Description, ItemType, AccessLevel, IsBuiltIn";

        public void Create(ItemRecord i)
            => _db.Execute("INSERT INTO Items (ItemId, ItemName, Description, ItemType, AccessLevel, IsBuiltIn) VALUES (:id, :n, :d, :t, :a, 0);",
                (":id", i.ItemId), (":n", i.ItemName), (":d", i.Description), (":t", i.ItemType), (":a", i.AccessLevel));

        public ItemRecord Find(string id) => _db.Query($"SELECT {Columns} FROM Items WHERE ItemId = :id;", (":id", id)).Select(Map).FirstOrDefault();

        public List<ItemRecord> All() => _db.Query($"SELECT {Columns} FROM Items ORDER BY IsBuiltIn DESC, ItemId;").Select(Map).ToList();

        public bool Update(ItemRecord i)
            => _db.Execute("UPDATE Items SET ItemName = :n, Description = :d, ItemType = :t, AccessLevel = :a WHERE ItemId = :id;",
                (":n", i.ItemName), (":d", i.Description), (":t", i.ItemType), (":a", i.AccessLevel), (":id", i.ItemId)) == 1;

        public bool Delete(string id) => _db.Execute("DELETE FROM Items WHERE ItemId = :id;", (":id", id)) == 1;

        private static ItemRecord Map(SqliteRow r) => new()
        {
            ItemId = r.Text("ItemId"),
            ItemName = r.Text("ItemName"),
            Description = r.Text("Description"),
            ItemType = r.Text("ItemType"),
            AccessLevel = r.Int("AccessLevel"),
            IsBuiltIn = r.Bool("IsBuiltIn"),
        };
    }

    // ------------------------------------------------------------------ progress summary (derived from JSON saves)

    public sealed class ProgressRepository
    {
        private readonly SqliteConnection _db;
        internal ProgressRepository(SqliteConnection db) => _db = db;

        public void Upsert(ProgressRecord p, IDictionary<string, int> inventory)
        {
            _db.InTransaction(() =>
            {
                _db.Execute(@"INSERT INTO GameProgress (UserId, CurrentSector, CurrentObjective, LogsCollected, PlaytimeSeconds, AiTrust, EndingReached, LastSavedAt)
                              VALUES (:u, :s, :o, :l, :p, :t, :e, :at)
                              ON CONFLICT(UserId) DO UPDATE SET CurrentSector = :s, CurrentObjective = :o, LogsCollected = :l,
                                  PlaytimeSeconds = :p, AiTrust = :t, EndingReached = :e, LastSavedAt = :at;",
                    (":u", p.UserId), (":s", p.CurrentSector), (":o", p.CurrentObjective), (":l", p.LogsCollected),
                    (":p", p.PlaytimeSeconds), (":t", p.AiTrust), (":e", p.EndingReached), (":at", Now.Utc));
                _db.Execute("DELETE FROM PlayerInventory WHERE UserId = :u;", (":u", p.UserId));
                foreach (var kv in inventory)
                {
                    if (kv.Value <= 0) continue;
                    // only items known to the catalog are mirrored (FK)
                    _db.Execute(@"INSERT INTO PlayerInventory (UserId, ItemId, Quantity)
                                  SELECT :u, :i, :q WHERE EXISTS (SELECT 1 FROM Items WHERE ItemId = :i);",
                        (":u", p.UserId), (":i", kv.Key), (":q", kv.Value));
                }
            });
        }

        public ProgressRecord Find(long userId)
            => _db.Query("SELECT * FROM GameProgress WHERE UserId = :u;", (":u", userId)).Select(r => new ProgressRecord
            {
                UserId = r.Long("UserId"),
                CurrentSector = r.Text("CurrentSector"),
                CurrentObjective = r.Text("CurrentObjective"),
                LogsCollected = r.Int("LogsCollected"),
                PlaytimeSeconds = r.Int("PlaytimeSeconds"),
                AiTrust = r.Int("AiTrust"),
                EndingReached = r.Text("EndingReached"),
                LastSavedAt = r.Text("LastSavedAt"),
            }).FirstOrDefault();

        public void RecordChoice(long userId, string name, string value)
            => _db.Execute("INSERT INTO PlayerChoices (UserId, ChoiceName, ChoiceValue, MadeAt) VALUES (:u, :n, :v, :t);",
                (":u", userId), (":n", name), (":v", value), (":t", Now.Utc));

        public List<(string name, string value, string at)> Choices(long userId)
            => _db.Query("SELECT ChoiceName, ChoiceValue, MadeAt FROM PlayerChoices WHERE UserId = :u ORDER BY ChoiceId;", (":u", userId))
                  .Select(r => (r.Text("ChoiceName"), r.Text("ChoiceValue"), r.Text("MadeAt"))).ToList();

        public void Reset(long userId)
        {
            _db.Execute("DELETE FROM GameProgress WHERE UserId = :u;", (":u", userId));
            _db.Execute("DELETE FROM PlayerInventory WHERE UserId = :u;", (":u", userId));
        }
    }
}
