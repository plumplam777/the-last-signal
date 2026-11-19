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
        public static DialogueGraph AiMed01, AiMed02, AiRes01, AiResHint, AiRes02, AiCore01, AiCore02, AiCore03;
        public static readonly string HeardAllLogs = WorldKeys.Story("heard_all_logs");

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

            CreatePhase6();
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Phase 6 (StoryBible §7.3–7.5)

        private static ConditionSet TrustAtLeast(int v) => new() { conditions = { new Condition { type = ConditionType.IntAtLeast, key = WorldKeys.AiTrust, value = v } } };
        private static ConditionSet TrustBelow(int v) => new() { conditions = { new Condition { type = ConditionType.IntBelow, key = WorldKeys.AiTrust, value = v } } };
        private static ConditionSet FlagIs(string flag, bool value)
            => new() { conditions = { new Condition { type = value ? ConditionType.FlagTrue : ConditionType.FlagFalse, key = flag } } };

        private static DialogueChoice ChoiceTo(string id, string textN, string replyNode, int trustDelta, ConditionSet showWhen = null, string setsFlag = null)
        {
            var c = new DialogueChoice { id = $"{id}_{replyNode}", textKey = $"dlg.{id}.{textN}", next = replyNode, showWhen = showWhen ?? new ConditionSet() };
            if (trustDelta != 0) c.effects.Add(new Effect { type = EffectType.AddInt, key = WorldKeys.AiTrust, value = trustDelta });
            if (!string.IsNullOrEmpty(setsFlag)) c.effects.Add(Effect.Flag(setsFlag));
            return c;
        }

        private static DialogueNode Line(string id, string node, string next, ConditionSet showWhen = null, string onEnterFlag = null)
        {
            var n = new DialogueNode { id = node, lineKey = $"dlg.{id}.{node}", next = next, showWhen = showWhen ?? new ConditionSet() };
            if (onEnterFlag != null) n.onEnter.Add(Effect.Flag(onEnterFlag));
            return n;
        }

        private static DialogueNode Reply(string id, string node) => new() { id = node, lineKey = $"dlg.{id}.{node}" };

        private static void CreatePhase6()
        {
            AiMed01 = Graph("ai_med_01", Question("ai_med_01", Choice("ai_med_01", 1, 0), Choice("ai_med_01", 2, +5), Choice("ai_med_01", 3, -10)));

            var med02 = new List<DialogueNode>
            {
                Line("ai_med_02", "high", "low", TrustAtLeast(HighTrust), StationContent.KnowsJonahDeath),
                Line("ai_med_02", "low", "ask", TrustBelow(HighTrust), StationContent.KnowsJonahDeath),
                new() { id = "ask", choices = { Choice("ai_med_02", 1, -10), Choice("ai_med_02", 2, +5), Choice("ai_med_02", 3, 0) } },
            };
            AddReplies("ai_med_02", med02, 3);
            AiMed02 = Graph("ai_med_02", med02);

            AiRes01 = Graph("ai_res_01", Question("ai_res_01",
                Choice("ai_res_01", 1, +10, StationContent.ListenedMurmur), Choice("ai_res_01", 2, -5), Choice("ai_res_01", 3, 0)));

            var mid = TrustAtLeast(LowTrustBelow);
            mid.conditions.AddRange(TrustBelow(HighTrust).conditions);
            AiResHint = Graph("ai_res_hint", new List<DialogueNode>
            {
                // the high-trust hint gives the rule away: entering it now counts as noticing the mirror
                Line("ai_res_hint", "high", "mid", TrustAtLeast(HighTrust), StationContent.NoticedMirror),
                Line("ai_res_hint", "mid", "low", mid),
                Line("ai_res_hint", "low", null, TrustBelow(LowTrustBelow)),
            });

            var res02 = new List<DialogueNode>
            {
                Line("ai_res_02", "l0", "mirror"),
                Line("ai_res_02", "mirror", "ask", FlagIs(StationContent.NoticedMirror, true)),
                new() { id = "ask", choices = { Choice("ai_res_02", 1, -5, StationContent.ToldCairnMirror), Choice("ai_res_02", 2, +10), Choice("ai_res_02", 3, -5) } },
            };
            AddReplies("ai_res_02", res02, 3);
            AiRes02 = Graph("ai_res_02", res02);

            AiCore01 = Graph("ai_core_01", Question("ai_core_01", Choice("ai_core_01", 1, -5), Choice("ai_core_01", 2, +5)));

            AiCore02 = Graph("ai_core_02", new List<DialogueNode>
            {
                Line("ai_core_02", "l0", "high", null, StationContent.HeardOriginalWarning),
                Line("ai_core_02", "high", "low", TrustAtLeast(HighTrust)),
                Line("ai_core_02", "low", "ask", TrustBelow(LowTrustBelow)),
                new()
                {
                    id = "ask",
                    choices =
                    {
                        ChoiceTo("ai_core_02", "c1", "r1", -10),
                        ChoiceTo("ai_core_02", "c2", "r2a", +5, TrustAtLeast(50)),
                        ChoiceTo("ai_core_02", "c2", "r2b", 0, TrustBelow(50)),
                        ChoiceTo("ai_core_02", "c3", "r3", +5),
                    },
                },
                Reply("ai_core_02", "r1"), Reply("ai_core_02", "r2a"), Reply("ai_core_02", "r2b"), Reply("ai_core_02", "r3"),
            });

            AiCore03 = Graph("ai_core_03", new List<DialogueNode>
            {
                Line("ai_core_03", "a", "b"),
                Line("ai_core_03", "b", "high"),
                Line("ai_core_03", "high", "low", TrustAtLeast(HighTrust)),
                Line("ai_core_03", "low", "c", TrustBelow(LowTrustBelow)),
                Line("ai_core_03", "c", "ask", StationContent.AllLogsCollected(), HeardAllLogs),
                new()
                {
                    id = "ask",
                    choices =
                    {
                        ChoiceTo("ai_core_03", "c1", "r1", 0),
                        ChoiceTo("ai_core_03", "c2", "r2a", 0, FlagIs(HeardAllLogs, false)),
                        ChoiceTo("ai_core_03", "c2", "r2b", 0, FlagIs(HeardAllLogs, true)),
                        new DialogueChoice { id = "ai_core_03_c3", textKey = "dlg.ai_core_03.c3", next = null },
                    },
                },
                Reply("ai_core_03", "r1"), Reply("ai_core_03", "r2a"), Reply("ai_core_03", "r2b"),
            });
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
