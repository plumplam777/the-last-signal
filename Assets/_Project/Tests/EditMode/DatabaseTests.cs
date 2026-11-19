using System.Collections.Generic;
using System.IO;
using LastSignal.Database;
using NUnit.Framework;

namespace LastSignal.Tests
{
    /// <summary>SQLite wrapper, schema, the three CRUD areas, accounts and security rules — on a temporary database.</summary>
    public class DatabaseTests
    {
        private string _path;
        private DatabaseManager _db;
        private AuthService _auth;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), $"lastsignal_test_{System.Guid.NewGuid():N}.db");
            _db = new DatabaseManager();
            Assert.IsTrue(_db.Open(_path), _db.LastError);
            _auth = new AuthService(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
            foreach (var f in new[] { _path, _path + "-wal", _path + "-shm" })
                if (File.Exists(f)) File.Delete(f);
        }

        [Test]
        public void Wrapper_BindsParameters_NoInjection()
        {
            // a hostile username is stored literally, not executed
            var hostile = "x'); DROP TABLE Users; --";
            Assert.AreEqual("auth.err.username_format", AuthService.ValidateUsername(hostile));
            var (hash, salt, it) = PasswordHasher.Hash("secret1");
            _db.Users.Create(hostile, hash, salt, it, UserRole.Player);
            Assert.AreEqual(hostile, _db.Users.FindByName(hostile).Username);
            Assert.AreEqual(1, _db.Users.Count(), "Users table must still exist.");
        }

        [Test]
        public void Passwords_AreHashed_AndVerified()
        {
            Assert.IsNull(_auth.Register("noa", "lantern7", "lantern7", UserRole.Player, out var id));
            var stored = _db.Users.FindById(id);
            Assert.AreNotEqual("lantern7", stored.PasswordHash);
            Assert.IsFalse(stored.PasswordHash.Contains("lantern7"));
            Assert.IsNull(_auth.Login("NOA", "lantern7"), "Usernames are case-insensitive.");
            Assert.AreEqual("auth.err.invalid_login", _auth.Login("noa", "wrong-pass"));
            Assert.AreEqual("auth.err.invalid_login", _auth.Login("nobody", "lantern7"), "Same message for unknown users.");
        }

        [Test]
        public void Register_ValidatesInput()
        {
            Assert.AreEqual("auth.err.username_format", _auth.Register("a", "secret1", "secret1", UserRole.Player, out _));
            Assert.AreEqual("auth.err.password_short", _auth.Register("pilot", "123", "123", UserRole.Player, out _));
            Assert.AreEqual("auth.err.password_mismatch", _auth.Register("pilot", "secret1", "secret2", UserRole.Player, out _));
            Assert.IsNull(_auth.Register("pilot", "secret1", "secret1", UserRole.Player, out _));
            Assert.AreEqual("auth.err.username_taken", _auth.Register("Pilot", "secret1", "secret1", UserRole.Player, out _));
        }

        [Test]
        public void UsersCrud_AndAdminRules()
        {
            Assert.IsTrue(_auth.NeedsFirstAdmin);
            Assert.IsNull(_auth.Register("admin", "admin123", "admin123", UserRole.Admin, out var adminId));
            Assert.IsFalse(_auth.NeedsFirstAdmin);
            Assert.IsNull(_auth.Register("player1", "secret1", "secret1", UserRole.Player, out var playerId));
            _auth.Login("admin", "admin123");

            var admin = _db.Users.FindById(adminId);
            Assert.AreEqual("auth.err.delete_self", _auth.CanDelete(admin));
            Assert.AreEqual("auth.err.last_admin", _auth.CanChangeRole(admin, UserRole.Player));

            Assert.IsTrue(_db.Users.UpdateUsername(playerId, "player_one"));        // update
            Assert.IsTrue(_db.Users.UpdateRole(playerId, UserRole.Admin));          // update role
            Assert.AreEqual(2, _db.Users.CountAdmins());
            Assert.IsNull(_auth.ChangePassword(playerId, "newpass1"));
            Assert.IsNull(_auth.CanDelete(_db.Users.FindById(playerId)));
            Assert.IsTrue(_db.Users.Delete(playerId));                              // delete
            Assert.IsNull(_db.Users.FindById(playerId));
            Assert.AreEqual(1, _db.Users.All().Count);                               // read
        }

        [Test]
        public void SectorsCrud_ProtectsBuiltIns()
        {
            _db.SeedCatalog(UnityEngine.ScriptableObject.CreateInstance<LastSignal.Core.GameContent>(), k => k);
            Assert.AreEqual(5, _db.Sectors.All().Count);
            _db.Sectors.Create(new SectorRecord { SectorId = "test_wing", SectorName = "Test Wing", SortOrder = 9 });
            var s = _db.Sectors.Find("test_wing");
            s.Description = "Edited";
            s.IsUnlocked = true;
            Assert.IsTrue(_db.Sectors.Update(s));
            Assert.IsTrue(_db.Sectors.IsUnlocked("test_wing"));
            Assert.IsTrue(_db.Sectors.Delete("test_wing"));
            Assert.IsFalse(_db.Sectors.Delete("docking"), "Built-in sectors cannot be deleted.");
            Assert.AreEqual(5, _db.Sectors.All().Count);
        }

        [Test]
        public void ItemsCrud_AndInventoryMirrorCascade()
        {
            _db.Items.Create(new ItemRecord { ItemId = "item_test_card", ItemName = "Test card", ItemType = "Keycard", AccessLevel = 2 });
            var item = _db.Items.Find("item_test_card");
            Assert.AreEqual(2, item.AccessLevel);
            item.ItemName = "Renamed card";
            Assert.IsTrue(_db.Items.Update(item));
            Assert.AreEqual("Renamed card", _db.Items.Find("item_test_card").ItemName);

            _auth.Register("pilot", "secret1", "secret1", UserRole.Player, out var uid);
            _db.Progress.Upsert(new ProgressRecord { UserId = uid, CurrentSector = "Sector_DockingBay", AiTrust = 55 },
                new Dictionary<string, int> { ["item_test_card"] = 1, ["unknown_item"] = 3 });
            Assert.AreEqual(55, _db.Progress.Find(uid).AiTrust);

            Assert.IsTrue(_db.Items.Delete("item_test_card"));
            Assert.IsNull(_db.Items.Find("item_test_card"));
            // deleting the user cascades to progress
            _db.Users.Delete(uid);
            Assert.IsNull(_db.Progress.Find(uid));
        }

        [Test]
        public void Reopen_KeepsData_AndSchemaVersion()
        {
            _auth.Register("pilot", "secret1", "secret1", UserRole.Player, out _);
            _db.Dispose();
            var again = new DatabaseManager();
            Assert.IsTrue(again.Open(_path));
            Assert.AreEqual(1, again.Users.Count());
            again.Dispose();
            _db.Open(_path);
        }
    }
}
