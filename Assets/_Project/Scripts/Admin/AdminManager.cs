using System.Linq;
using LastSignal.Core;
using LastSignal.Database;

namespace LastSignal.Admin
{
    /// <summary>
    /// Administrator tools. Every command checks the session role. Overrides are session-only (not written to
    /// WorldState or the save), so an admin's testing never changes normal player progression:
    /// unlock-all lives here in memory; sector unlocks are the database demo override (Sectors.IsUnlocked).
    /// </summary>
    public sealed class AdminManager
    {
        private readonly DatabaseManager _db;
        private readonly AuthService _auth;

        public AdminManager(DatabaseManager db, AuthService auth)
        {
            _db = db;
            _auth = auth;
            _auth.SessionChanged += _ => UnlockAllDoors = false;
        }

        public bool IsAdmin => _auth.IsAdmin;

        /// <summary>Session-only: every door and hatch ignores its requirements.</summary>
        public bool UnlockAllDoors { get; private set; }

        public bool SetUnlockAllDoors(bool on)
        {
            if (!IsAdmin) return Denied();
            UnlockAllDoors = on;
            Log.Info(nameof(AdminManager), $"Unlock all doors: {on}");
            return true;
        }

        /// <summary>Doors/hatches ask this before checking their own requirements.</summary>
        public bool OverridesAccessTo(string sectorId)
        {
            if (UnlockAllDoors && IsAdmin) return true;
            return !string.IsNullOrEmpty(sectorId) && _db.IsAvailable && _db.Sectors.IsUnlocked(sectorId);
        }

        public bool GiveAllItems()
        {
            if (!IsAdmin) return Denied();
            foreach (var item in GameServices.Content.Items) GameServices.Inventory.Add(item);
            GameServices.World.SetBool(Player.FlashlightController.OwnedFlag, true);
            return true;
        }

        public bool RestorePower(string sectorId)
        {
            if (!IsAdmin) return Denied();
            GameServices.World.SetBool(WorldKeys.Power(sectorId), true);
            return true;
        }

        /// <summary>Loads a sector scene directly, bypassing progression.</summary>
        public bool Teleport(string sectorId)
        {
            if (!IsAdmin) return Denied();
            var scene = DatabaseManager.BuiltInSectors.FirstOrDefault(s => s.id == sectorId).scene;
            if (!string.IsNullOrEmpty(_db.IsAvailable ? _db.Sectors.Find(sectorId)?.SceneName : null))
                scene = _db.Sectors.Find(sectorId).SceneName;
            if (string.IsNullOrEmpty(scene) || !UnityEngine.Application.CanStreamedLevelBeLoaded(scene))
            {
                Log.Warn(nameof(AdminManager), $"Sector '{sectorId}' has no loadable scene.");
                return false;
            }
            GameServices.Scenes.Load(scene);
            return true;
        }

        private static bool Denied()
        {
            Log.Warn(nameof(AdminManager), "Admin command refused: not an administrator.");
            return false;
        }
    }
}
