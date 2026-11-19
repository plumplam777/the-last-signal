using System.Collections.Generic;
using LastSignal.Core;
using LastSignal.Dialogue;
using UnityEditor;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// CAIRN conversations for the Docking Bay and Engineering (StoryBible §7.1–7.2). Trust deltas and flags exactly
    /// as scripted. Text lives in the localization tables under dlg.&lt;id&gt;.*.
    /// </summary>
    public static class DialogueContent
    {
        private const string Folder = "Assets/_Project/ScriptableObjects/Dialogues";

        public const int HighTrust = 65;  // ≥ 65
        public const int LowTrustBelow = 36; // ≤ 35

        public static readonly string NoticedGuidance = WorldKeys.Story("noticed_guidance");
        public static readonly string RefusedInterlock = WorldKeys.Story("refused_interlock");
        public static readonly string HeardLockout = WorldKeys.Story("heard_lockout");

        public static DialogueGraph AiDock01 { get; private set; }
        public static DialogueGraph AiDock02 { get; private set; }
        public static DialogueGraph AiEng01 { get; private set; }
        public static DialogueGraph AiEng02 { get; private set; }
        public static DialogueGraph AiEng03 { get; private set; }

        public static void CreateAll()
        {
            AiDock01 = Graph("ai_dock_01", Question("ai_dock_01",
                Choice("ai_dock_01", 1, 0),
                Choice("ai_dock_01", 2, +5),
                Choice("ai_dock_01", 3, -5, NoticedGuidance)));

            AiDock02 = Graph("ai_dock_02", Question("ai_dock_02",
                Choice("ai_dock_02", 1, -5),
                Choice("ai_dock_02", 2, +5),
                Choice("ai_dock_02", 3, 0)));

            AiEng01 = Graph("ai_eng_01", Question("ai_eng_01",
                Choice("ai_eng_01", 1, 0),
                Choice("ai_eng_01", 2, -5)));

            // l0 → (high add-on) → (low add-on) → choices
            var eng02 = new List<DialogueNode>
            {
                new() { id = "l0", lineKey = "dlg.ai_eng_02.l0", next = "high", onEnter = { Effect.Flag(HeardLockout) } },
                new()
                {
                    id = "high", lineKey = "dlg.ai_eng_02.high", next = "low",
                    showWhen = new ConditionSet { conditions = { new Condition { type = ConditionType.IntAtLeast, key = WorldKeys.AiTrust, value = HighTrust } } },
                },
                new()
                {
                    id = "low", lineKey = "dlg.ai_eng_02.low", next = "ask",
                    showWhen = new ConditionSet { conditions = { new Condition { type = ConditionType.IntBelow, key = WorldKeys.AiTrust, value = LowTrustBelow } } },
                },
                new()
                {
                    id = "ask", lineKey = null,
                    choices =
                    {
                        Choice("ai_eng_02", 1, -5),
                        Choice("ai_eng_02", 2, +10),
                        Choice("ai_eng_02", 3, -10, RefusedInterlock),
                    },
                },
            };
            AddReplies("ai_eng_02", eng02, 3);
            AiEng02 = Graph("ai_eng_02", eng02);

            AiEng03 = Graph("ai_eng_03", Question("ai_eng_03",
                Choice("ai_eng_03", 1, +10, EngineeringContent.FedArrayFlag),
                Choice("ai_eng_03", 2, -5)));

            AssetDatabase.SaveAssets();
        }

        /// <summary>Opening line with choices, each followed by its reply node (r1..rN).</summary>
        private static List<DialogueNode> Question(string id, params DialogueChoice[] choices)
        {
            var nodes = new List<DialogueNode> { new() { id = "l0", lineKey = $"dlg.{id}.l0", choices = new List<DialogueChoice>(choices) } };
            AddReplies(id, nodes, choices.Length);
            return nodes;
        }

        private static void AddReplies(string id, List<DialogueNode> nodes, int count)
        {
            for (int i = 1; i <= count; i++) nodes.Add(new DialogueNode { id = $"r{i}", lineKey = $"dlg.{id}.r{i}" });
        }

        private static DialogueChoice Choice(string id, int n, int trustDelta, string setsFlag = null)
        {
            var c = new DialogueChoice { id = $"{id}_c{n}", textKey = $"dlg.{id}.c{n}", next = $"r{n}" };
            if (trustDelta != 0) c.effects.Add(new Effect { type = EffectType.AddInt, key = WorldKeys.AiTrust, value = trustDelta });
            if (!string.IsNullOrEmpty(setsFlag)) c.effects.Add(Effect.Flag(setsFlag));
            return c;
        }

        private static DialogueGraph Graph(string id, List<DialogueNode> nodes)
        {
            var g = DockingBayContent.Asset<DialogueGraph>(Folder, $"dlg_{id}");
            g.EditorInit(id, true, nodes);
            EditorUtility.SetDirty(g);
            return g;
        }
    }
}
