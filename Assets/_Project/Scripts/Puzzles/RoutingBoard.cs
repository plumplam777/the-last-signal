using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Puzzles
{
    [Serializable]
    public sealed class RoutingSocket
    {
        public string id;
        [Tooltip("Localization key of the socket label.")]
        public string labelKey;
        [Tooltip("Must be plugged for the solution.")]
        public bool required;
        [Tooltip("Flag set (once) the first time a cable goes into this socket, e.g. story.fed_array.")]
        public string firstPlugFlag;
    }

    /// <summary>
    /// Power routing board: N sockets, a limited number of cables. Solved when exactly the required sockets
    /// are plugged. Plug state lives in WorldState ("routing.&lt;puzzleId&gt;.&lt;socket&gt;") so it survives saves.
    /// After solving, the board stays locked in the solved layout.
    /// </summary>
    public sealed class RoutingBoard : Puzzle
    {
        [SerializeField] private int cables = 3;
        [SerializeField] private List<RoutingSocket> sockets = new();

        public int Cables => cables;
        public IReadOnlyList<RoutingSocket> Sockets => sockets;
        public int PluggedCount => sockets.Count(s => IsPlugged(s.id));
        public int FreeCables => cables - PluggedCount;

        public event Action<string, bool> SocketChanged;

        public string Key(string socketId) => $"routing.{puzzleId}.{socketId}";

        public bool IsPlugged(string socketId) => GameServices.World.GetBool(Key(socketId));

        public RoutingSocket Find(string socketId) => sockets.FirstOrDefault(s => s.id == socketId);

        /// <summary>Plugs or unplugs. Returns false (with a feedback key) when not allowed.</summary>
        public bool Toggle(string socketId, out string feedbackKey)
        {
            feedbackKey = null;
            var socket = Find(socketId);
            if (socket == null || IsSolved) return false;
            bool plugged = IsPlugged(socketId);
            if (!plugged && FreeCables <= 0)
            {
                feedbackKey = "puzzle.routing.no_cables";
                ReportFailure(feedbackKey);
                return false;
            }
            GameServices.World.SetBool(Key(socketId), !plugged);
            if (!plugged && !string.IsNullOrEmpty(socket.firstPlugFlag)) GameServices.World.SetBool(socket.firstPlugFlag, true);
            SocketChanged?.Invoke(socketId, !plugged);
            if (sockets.All(s => IsPlugged(s.id) == s.required)) MarkSolved();
            return true;
        }

#if UNITY_EDITOR
        public void EditorSetSockets(int cableCount, List<RoutingSocket> newSockets)
        {
            cables = cableCount;
            sockets = newSockets;
        }
#endif
    }
}
