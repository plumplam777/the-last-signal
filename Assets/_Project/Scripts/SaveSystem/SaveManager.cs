using System;
using System.IO;
using LastSignal.Core;
using LastSignal.Database;
using Newtonsoft.Json;
using UnityEngine;

namespace LastSignal.SaveSystem
{
    public enum SaveResult
    {
        Ok,
        NoSession,
        NotInSector,
        Failed,
    }

    /// <summary>
    /// JSON save/load per user (persistentDataPath/Saves/&lt;userId&gt;/autosave.json). Writes are atomic
    /// (temp file → replace) and keep the previous save as .bak; a corrupt file falls back to the backup.
    /// After a successful write the database progress summary is updated (never the other way round).
    /// Checkpoints: entering a sector and completing an objective; manual save from the pause menu.
    /// </summary>
    public sealed class SaveManager
    {
        private const string FileName = "autosave.json";
        private readonly DatabaseManager _db;
        private readonly AuthService _auth;
        private string _root;
        private float _sessionStart;
        private float _loadedPlaytime;

        public string LastError { get; private set; }
        public event Action<SaveResult> Saved;

        /// <summary>Player pose to apply after the next sector load (set by Load, consumed by SectorRoot).</summary>
        public SaveData PendingRestore { get; private set; }

        public SaveManager(DatabaseManager db, AuthService auth, string root = null)
        {
            _db = db;
            _auth = auth;
            _root = root ?? Path.Combine(Application.persistentDataPath, "Saves");
            _sessionStart = Time.realtimeSinceStartup;
        }

        public void SetRoot(string root) => _root = root;

        public string PathFor(long userId) => Path.Combine(_root, userId.ToString(), FileName);

        public bool HasSave(long userId) => File.Exists(PathFor(userId)) || File.Exists(PathFor(userId) + ".bak");

        public bool HasSaveForCurrentUser => _auth.IsLoggedIn && HasSave(_auth.Current.UserId);

        public void BeginNewRun()
        {
            _loadedPlaytime = 0f;
            _sessionStart = Time.realtimeSinceStartup;
            PendingRestore = null;
        }

        public float Playtime => _loadedPlaytime + (Time.realtimeSinceStartup - _sessionStart);

        public SaveData Capture(string scene, Vector3 position, float yaw, float pitch)
        {
            return new SaveData
            {
                userId = _auth.Current?.UserId ?? 0,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                scene = scene,
                playerPosition = new[] { position.x, position.y, position.z },
                playerYaw = yaw,
                playerPitch = pitch,
                inventory = GameServices.Inventory.CreateSnapshot(),
                world = GameServices.World.CreateSnapshot(),
                playtimeSeconds = Playtime,
                currentObjective = GameServices.Quests.CurrentObjective?.id,
                logsCollected = GameServices.AudioLogs.CollectedCount,
            };
        }

        public SaveResult Save(SaveData data)
        {
            if (!_auth.IsLoggedIn) return Report(SaveResult.NoSession);
            try
            {
                var path = PathFor(_auth.Current.UserId);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                }
                File.Move(tmp, path);
                UpdateSummary(data);
                Log.Info(nameof(SaveManager), $"Saved {data.scene} for user {data.userId}.");
                return Report(SaveResult.Ok);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Log.Error(nameof(SaveManager), $"Save failed: {e.Message}");
                return Report(SaveResult.Failed);
            }
        }

        /// <summary>Reads the current user's save (falls back to the backup). Null with <see cref="LastError"/> on failure.</summary>
        public SaveData Read(long userId)
        {
            var path = PathFor(userId);
            foreach (var candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(candidate));
                    if (data == null) throw new JsonException("empty file");
                    if (data.version > SaveData.CurrentVersion)
                    {
                        LastError = "save.err.newer_version";
                        return null;
                    }
                    // future: migrate older versions step by step here
                    if (candidate != path) Log.Warn(nameof(SaveManager), "Main save was unreadable; using the backup.");
                    return data;
                }
                catch (Exception e)
                {
                    Log.Warn(nameof(SaveManager), $"Unreadable save {candidate}: {e.Message}");
                    LastError = "save.err.corrupt";
                }
            }
            LastError ??= "save.err.missing";
            return null;
        }

        /// <summary>Restores world/inventory from the save and loads its scene; the pose is applied on arrival.</summary>
        public bool Continue()
        {
            LastError = null;
            if (!_auth.IsLoggedIn)
            {
                LastError = "save.err.no_session";
                return false;
            }
            var data = Read(_auth.Current.UserId);
            if (data == null) return false;
            GameServices.SuppressCheckpoints = true;
            try
            {
                // inventory first, so the world restore evaluates objectives against the saved items
                GameServices.Inventory.Restore(data.inventory, id => GameServices.Content.FindItem(id));
                GameServices.World.Restore(data.world);
            }
            finally
            {
                GameServices.SuppressCheckpoints = false;
            }
            _loadedPlaytime = data.playtimeSeconds;
            _sessionStart = Time.realtimeSinceStartup;
            PendingRestore = data;
            GameServices.Scenes.Load(string.IsNullOrEmpty(data.scene) ? SceneIds.DockingBay : data.scene);
            return true;
        }

        public SaveData ConsumePendingRestore()
        {
            var d = PendingRestore;
            PendingRestore = null;
            return d;
        }

        public void DeleteSave(long userId)
        {
            foreach (var p in new[] { PathFor(userId), PathFor(userId) + ".bak" })
                if (File.Exists(p)) File.Delete(p);
            if (_db.IsAvailable) _db.Progress.Reset(userId);
        }

        private void UpdateSummary(SaveData data)
        {
            if (!_db.IsAvailable || data.userId <= 0) return;
            try
            {
                var trust = data.world.Ints != null && data.world.Ints.TryGetValue(WorldKeys.AiTrust, out var t) ? t : WorldKeys.AiTrustDefault;
                data.world.Strings.TryGetValue("choice.final", out var ending);
                _db.Progress.Upsert(new ProgressRecord
                {
                    UserId = data.userId,
                    CurrentSector = data.scene,
                    CurrentObjective = data.currentObjective,
                    LogsCollected = data.logsCollected,
                    PlaytimeSeconds = Mathf.RoundToInt(data.playtimeSeconds),
                    AiTrust = trust,
                    EndingReached = ending,
                }, data.inventory);
            }
            catch (Exception e)
            {
                // the JSON save already succeeded; the summary is secondary
                Log.Warn(nameof(SaveManager), $"Progress summary not updated: {e.Message}");
            }
        }

        private SaveResult Report(SaveResult r)
        {
            Saved?.Invoke(r);
            return r;
        }
    }
}
