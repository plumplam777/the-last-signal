using System;
using System.Collections.Generic;

namespace LastSignal.Core
{
    /// <summary>
    /// Single store for every progression fact (power, doors, puzzles, logs, story flags, AI trust).
    /// Gameplay systems write facts here and read them back through conditions; the save system
    /// serialises a <see cref="WorldStateSnapshot"/>. Keys follow the patterns in <see cref="WorldKeys"/>.
    /// </summary>
    public sealed class WorldState
    {
        private readonly Dictionary<string, bool> _bools = new();
        private readonly Dictionary<string, int> _ints = new();
        private readonly Dictionary<string, string> _strings = new();

        /// <summary>Raised with the key whenever a value actually changes.</summary>
        public event Action<string> Changed;

        /// <summary>Raised after <see cref="Restore"/> or <see cref="Clear"/> replaced everything at once.</summary>
        public event Action Reset;

        public bool GetBool(string key, bool defaultValue = false)
            => _bools.TryGetValue(key, out var v) ? v : defaultValue;

        public int GetInt(string key, int defaultValue = 0)
            => _ints.TryGetValue(key, out var v) ? v : defaultValue;

        public string GetString(string key, string defaultValue = null)
            => _strings.TryGetValue(key, out var v) ? v : defaultValue;

        public bool HasKey(string key)
            => _bools.ContainsKey(key) || _ints.ContainsKey(key) || _strings.ContainsKey(key);

        public void SetBool(string key, bool value)
        {
            ValidateKey(key);
            if (_bools.TryGetValue(key, out var old) && old == value) return;
            _bools[key] = value;
            Changed?.Invoke(key);
        }

        public void SetInt(string key, int value)
        {
            ValidateKey(key);
            if (_ints.TryGetValue(key, out var old) && old == value) return;
            _ints[key] = value;
            Changed?.Invoke(key);
        }

        /// <summary>Adds <paramref name="delta"/> and clamps the result to [min, max].</summary>
        public int AddInt(string key, int delta, int min = int.MinValue, int max = int.MaxValue, int defaultValue = 0)
        {
            long next = (long)GetInt(key, defaultValue) + delta;
            int clamped = (int)Math.Clamp(next, min, max);
            SetInt(key, clamped);
            return clamped;
        }

        public void SetString(string key, string value)
        {
            ValidateKey(key);
            if (_strings.TryGetValue(key, out var old) && old == value) return;
            _strings[key] = value;
            Changed?.Invoke(key);
        }

        public void Clear()
        {
            _bools.Clear();
            _ints.Clear();
            _strings.Clear();
            Reset?.Invoke();
        }

        public WorldStateSnapshot CreateSnapshot() => new()
        {
            Bools = new Dictionary<string, bool>(_bools),
            Ints = new Dictionary<string, int>(_ints),
            Strings = new Dictionary<string, string>(_strings),
        };

        public void Restore(WorldStateSnapshot snapshot)
        {
            _bools.Clear();
            _ints.Clear();
            _strings.Clear();
            if (snapshot != null)
            {
                if (snapshot.Bools != null) foreach (var kv in snapshot.Bools) _bools[kv.Key] = kv.Value;
                if (snapshot.Ints != null) foreach (var kv in snapshot.Ints) _ints[kv.Key] = kv.Value;
                if (snapshot.Strings != null) foreach (var kv in snapshot.Strings) _strings[kv.Key] = kv.Value;
            }
            Reset?.Invoke();
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("WorldState key must not be empty.", nameof(key));
        }
    }

    /// <summary>Serializable copy of the world state used by save files.</summary>
    [Serializable]
    public sealed class WorldStateSnapshot
    {
        public Dictionary<string, bool> Bools = new();
        public Dictionary<string, int> Ints = new();
        public Dictionary<string, string> Strings = new();
    }

    /// <summary>Canonical key builders. Never hand-type these patterns elsewhere.</summary>
    public static class WorldKeys
    {
        public const string AiTrust = "ai.trust";
        public const int AiTrustDefault = 50;
        public const int AiTrustMin = 0;
        public const int AiTrustMax = 100;

        public static string Power(string sectorId) => $"power.{sectorId}";
        public static string DoorUnlocked(string doorId) => $"door.{doorId}.unlocked";
        public static string PuzzleSolved(string puzzleId) => $"puzzle.{puzzleId}.solved";
        public static string LogCollected(string logId) => $"log.{logId}.collected";
        public static string SectorVisited(string sectorId) => $"sector.{sectorId}.visited";
        public static string Story(string flag) => $"story.{flag}";
        public static string Choice(string choiceId) => $"choice.{choiceId}";
    }

    /// <summary>Sector identifiers shared by scenes, database rows and world-state keys.</summary>
    public static class SectorIds
    {
        public const string Docking = "docking";
        public const string Engineering = "engineering";
        public const string Medical = "medical";
        public const string Research = "research";
        public const string Reactor = "reactor";
    }
}
