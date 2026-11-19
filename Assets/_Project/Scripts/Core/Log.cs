using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Thin logging wrapper that prefixes every message with the owning system,
    /// e.g. "[SaveManager] Failed to write save: ...". Use it instead of Debug.Log.
    /// </summary>
    public static class Log
    {
        public static void Info(string system, string message, Object context = null)
            => Debug.Log($"[{system}] {message}", context);

        public static void Warn(string system, string message, Object context = null)
            => Debug.LogWarning($"[{system}] {message}", context);

        public static void Error(string system, string message, Object context = null)
            => Debug.LogError($"[{system}] {message}", context);
    }
}
