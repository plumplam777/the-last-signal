using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Dialogue
{
    /// <summary>
    /// Runs <see cref="DialogueGraph"/>s. Lines advance on their own after a reading time; when a node has choices
    /// the game switches to InMenu until the player picks one (keys 1–3 or mouse via the UI). Requests made while
    /// another conversation runs (or while a menu is open) are queued. Logic only; the UI listens to the events.
    /// </summary>
    public sealed class DialogueManager : MonoBehaviour
    {
        private const float WordsPerSecond = 2.6f;
        private const float MinLineSeconds = 2.2f;

        private readonly Queue<DialogueGraph> _queue = new();
        private DialogueGraph _graph;
        private DialogueNode _node;
        private List<DialogueChoice> _choices = new();
        private float _lineTimer;
        private bool _awaitingChoice;
        private bool _choiceModeActive;

        public bool IsRunning => _graph != null;
        public DialogueGraph Current => _graph;
        public DialogueNode CurrentNode => _node;
        public IReadOnlyList<DialogueChoice> CurrentChoices => _choices;
        public bool AwaitingChoice => _awaitingChoice;

        public event Action<DialogueGraph> Started;
        /// <summary>(node) — a line is now shown.</summary>
        public event Action<DialogueNode> LineShown;
        /// <summary>(node, visible choices)</summary>
        public event Action<DialogueNode, IReadOnlyList<DialogueChoice>> ChoicesShown;
        /// <summary>(choice, trust delta applied)</summary>
        public event Action<DialogueChoice, int> ChoiceMade;
        public event Action<DialogueGraph> Ended;

        public bool HasPlayed(DialogueGraph graph) => graph != null && GameServices.World.GetBool(graph.PlayedKey);

        /// <summary>Starts (or queues) a conversation. Returns false when it already played and is once-only.</summary>
        public bool Play(DialogueGraph graph)
        {
            if (graph == null) return false;
            if (graph.Once && (HasPlayed(graph) || _queue.Contains(graph) || _graph == graph)) return false;
            if (IsRunning) _queue.Enqueue(graph);
            else Begin(graph);
            return true;
        }

        public void Choose(int index)
        {
            if (!_awaitingChoice || index < 0 || index >= _choices.Count) return;
            var choice = _choices[index];
            int before = GameServices.World.GetInt(WorldKeys.AiTrust, WorldKeys.AiTrustDefault);
            Effects.ApplyAll(choice.effects);
            if (!string.IsNullOrEmpty(choice.id)) GameServices.World.SetString(WorldKeys.Choice(_graph.Id), choice.id);
            int delta = GameServices.World.GetInt(WorldKeys.AiTrust, WorldKeys.AiTrustDefault) - before;
            _awaitingChoice = false;
            ExitChoiceMode();
            ChoiceMade?.Invoke(choice, delta);
            GoTo(choice.next);
        }

        /// <summary>Finishes the current line now (Enter / click). Does nothing while choices are waiting.</summary>
        public void SkipLine()
        {
            if (IsRunning && !_awaitingChoice) _lineTimer = 0f;
        }

        /// <summary>Ends the current conversation immediately (scene change, cutscene).</summary>
        public void StopAll()
        {
            _queue.Clear();
            if (IsRunning) Finish();
        }

        public static float ReadingTime(string text)
        {
            int words = string.IsNullOrEmpty(text) ? 0 : Regex.Matches(text, @"\S+").Count;
            return Mathf.Max(MinLineSeconds, words / WordsPerSecond + 1.2f);
        }

        private void Begin(DialogueGraph graph)
        {
            _graph = graph;
            if (graph.Once) GameServices.World.SetBool(graph.PlayedKey, true);
            Log.Info(nameof(DialogueManager), $"Dialogue started: {graph.Id}");
            Started?.Invoke(graph);
            GoTo(graph.StartNode);
        }

        private void GoTo(string nodeId)
        {
            // skip nodes whose condition fails (trust/flag add-ons)
            var node = string.IsNullOrEmpty(nodeId) ? null : _graph.Find(nodeId);
            int guard = 0;
            while (node != null && !node.showWhen.Evaluate() && guard++ < 64)
                node = string.IsNullOrEmpty(node.next) ? null : _graph.Find(node.next);
            if (node == null)
            {
                Finish();
                return;
            }
            _node = node;
            Effects.ApplyAll(node.onEnter);
            _choices = node.choices.Where(c => c.showWhen.Evaluate()).ToList();
            _awaitingChoice = false;
            if (string.IsNullOrEmpty(node.lineKey))
            {
                // choice-only node: keep the previous line on screen and go straight to the choices
                _lineTimer = 0f;
                return;
            }
            _lineTimer = ReadingTime(GameServices.Localization.Get(node.lineKey));
            LineShown?.Invoke(node);
        }

        private void Update()
        {
            if (!IsRunning) return;
            if (GameServices.State.Current == GameState.Paused) return;

            if (_awaitingChoice)
            {
                if (!_choiceModeActive) TryEnterChoiceMode();
                ReadChoiceKeys();
                return;
            }

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) SkipLine();
            _lineTimer -= Time.unscaledDeltaTime;
            if (_lineTimer > 0f) return;
            if (_choices.Count > 0)
            {
                _awaitingChoice = true;
                TryEnterChoiceMode();
            }
            else
            {
                GoTo(_node.next);
            }
        }

        private void TryEnterChoiceMode()
        {
            // wait until no other screen has focus; choices need the cursor
            if (GameServices.State.Current != GameState.Playing) return;
            _choiceModeActive = true;
            GameServices.State.Set(GameState.InMenu);
            ChoicesShown?.Invoke(_node, _choices);
        }

        private void ExitChoiceMode()
        {
            if (!_choiceModeActive) return;
            _choiceModeActive = false;
            if (GameServices.State.Current == GameState.InMenu) GameServices.State.Set(GameState.Playing);
        }

        private void ReadChoiceKeys()
        {
            if (!_choiceModeActive) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Choose(2);
        }

        private void Finish()
        {
            var ended = _graph;
            _graph = null;
            _node = null;
            _choices = new List<DialogueChoice>();
            _awaitingChoice = false;
            ExitChoiceMode();
            Ended?.Invoke(ended);
            if (_queue.Count > 0) Begin(_queue.Dequeue());
        }
    }
}
