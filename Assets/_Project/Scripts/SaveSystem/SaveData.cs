using System;
using System.Collections.Generic;
using LastSignal.Core;

namespace LastSignal.SaveSystem
{
    /// <summary>
    /// The JSON save file (versioned). Authoritative for gameplay state; the database only keeps a summary.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long userId;
        public string savedAtUtc;
        public string scene;
        public float[] playerPosition = new float[3];
        public float playerYaw;
        public float playerPitch;
        public Dictionary<string, int> inventory = new();
        public WorldStateSnapshot world = new();
        public float playtimeSeconds;
        public string currentObjective;
        public int logsCollected;
    }
}
