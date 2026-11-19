using System.Collections.Generic;
using LastSignal.Audio;
using LastSignal.Core;
using LastSignal.Inventory;
using LastSignal.Puzzles;
using UnityEditor;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Data assets of Phase 6 (StoryBible §6.3–6.5, §8): Medical Bay, Research Lab, Reactor Core + Command —
    /// keycards, LOG_MED/RES/CORE, terminals, ids and flags shared by the builders, dialogue and quest.
    /// </summary>
    public static class StationContent
    {
        // ids / flags
        public const string DecodePuzzleId = "res_trial7_decode";
        public const string ReactorPuzzleId = "core_reactor_balance";
        public static readonly string PodWipedFlag = WorldKeys.Story("med_pod_wiped");
        public static readonly string KnowsJonahDeath = WorldKeys.Story("knows_jonah_death");
        public static readonly string CabinetReleasedFlag = WorldKeys.Story("med_cabinet_released");
        public static readonly string NoticedMirror = WorldKeys.Story("noticed_mirror");
        public static readonly string ToldCairnMirror = WorldKeys.Story("told_cairn_mirror");
        public static readonly string DecodeHintFlag = WorldKeys.Story("res_decode_hint");
        public static readonly string ListenedMurmur = WorldKeys.Story("listened_murmur");
        public static readonly string CommandReachedFlag = WorldKeys.Story("command_reached");
        public static readonly string HeardOriginalWarning = WorldKeys.Story("heard_original_warning");

        public static ItemDefinition KeycardResearch { get; private set; }
        public static ItemDefinition KeycardCore { get; private set; }
        public static AudioLogDefinition LogMed01, LogMed02, LogMed03, LogRes01, LogRes02, LogRes03, LogCore01, LogCore02, LogCore03;
        public static TerminalDefinition PatientTerminal { get; private set; }
        public static TerminalDefinition OkaforNotes { get; private set; }
        public static TerminalDefinition BoothTerminal { get; private set; }
        public static TerminalDefinition CommsTerminal { get; private set; }

        public static string PatientTerminalPuzzleId => PatientTerminal.LockPuzzleId;

        /// <summary>Every audio log collected (Ending C condition, FLAG_HEARD_ALL_LOGS).</summary>
        public static ConditionSet AllLogsCollected()
        {
            var set = new ConditionSet();
            foreach (var id in new[]
                     {
                         "log_dock_01", "log_dock_02", "log_eng_01", "log_eng_02", "log_eng_03", "log_med_01", "log_med_02",
                         "log_med_03", "log_res_01", "log_res_02", "log_res_03", "log_core_01", "log_core_02", "log_core_03",
                     })
                set.conditions.Add(new Condition { type = ConditionType.LogCollected, key = id });
            return set;
        }

        public static void CreateAll(GameContent registry)
        {
            KeycardResearch = DockingBayContent.Item("item_keycard_research", ItemType.Keycard, "item.keycard_research");
            KeycardCore = DockingBayContent.Item("item_keycard_core", ItemType.Keycard, "item.keycard_core");

            LogMed01 = DockingBayContent.Log("log_med_01", SectorIds.Medical, "speaker.okafor", "date.day_m20", false, 9);
            LogMed02 = DockingBayContent.Log("log_med_02", SectorIds.Medical, "speaker.okafor", "date.day_m18", false, 7);
            LogMed03 = DockingBayContent.Log("log_med_03", SectorIds.Medical, "speaker.okafor", "date.day_m9", false, 8);
            LogRes01 = DockingBayContent.Log("log_res_01", SectorIds.Research, "speaker.rhee", "date.day_m48", false, 8);
            LogRes02 = DockingBayContent.Log("log_res_02", SectorIds.Research, "speaker.rhee", "date.day_m28", false, 7);
            LogRes03 = DockingBayContent.Log("log_res_03", SectorIds.Research, "speaker.cairn", "date.day_m24", false, 7);
            LogCore01 = DockingBayContent.Log("log_core_01", SectorIds.Reactor, "speaker.dunn", "date.day_m6", false, 7);
            LogCore02 = DockingBayContent.Log("log_core_02", SectorIds.Reactor, "speaker.dunn", "date.day_m4", true, 6);
            LogCore03 = DockingBayContent.Log("log_core_03", SectorIds.Reactor, "speaker.rhee", "date.day_m5", false, 8);

            PatientTerminal = Terminal("terminal_med_patient", "med_patient", "0417", "terminal.med_patient.lock_hint",
                new List<TerminalEntry>
                {
                    Entry("med_patient", 1),
                    new()
                    {
                        titleKey = "terminal.med_patient.e2.title", bodyKey = "terminal.med_patient.e2.body",
                        onRead = { Effect.Flag(CabinetReleasedFlag) },
                    },
                    Entry("med_patient", 3),
                });
            OkaforNotes = Terminal("terminal_med_desk", "med_desk", null, null, new List<TerminalEntry> { Entry("med_desk", 1) });
            BoothTerminal = Terminal("terminal_res_booth", "res_booth", null, null, new List<TerminalEntry> { Entry("res_booth", 1) });
            CommsTerminal = Terminal("terminal_cmd_comms", "cmd_comms", null, null,
                new List<TerminalEntry> { Entry("cmd_comms", 1), Entry("cmd_comms", 2) });

            foreach (var so in new UnityEngine.ScriptableObject[]
                     {
                         KeycardResearch, KeycardCore, LogMed01, LogMed02, LogMed03, LogRes01, LogRes02, LogRes03, LogCore01, LogCore02, LogCore03,
                         PatientTerminal, OkaforNotes, BoothTerminal, CommsTerminal,
                     })
                registry.EditorRegister(so);
        }

        private static TerminalEntry Entry(string id, int n)
            => new() { titleKey = $"terminal.{id}.e{n}.title", bodyKey = $"terminal.{id}.e{n}.body" };

        private static TerminalDefinition Terminal(string asset, string id, string pin, string hint, List<TerminalEntry> entries)
        {
            var t = DockingBayContent.Asset<TerminalDefinition>(DockingBayContent.TerminalsFolder, asset);
            t.EditorInit(id, $"terminal.{id}.title", $"terminal.{id}.subtitle", pin, hint, entries);
            EditorUtility.SetDirty(t);
            return t;
        }
    }
}
