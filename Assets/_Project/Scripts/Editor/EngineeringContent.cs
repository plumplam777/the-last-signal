using System.Collections.Generic;
using LastSignal.Audio;
using LastSignal.Core;
using LastSignal.Inventory;
using LastSignal.Puzzles;
using UnityEditor;

namespace LastSignal.EditorTools
{
    /// <summary>Engineering data assets (StoryBible §6.2, §8): power coupling, LOG_ENG_01–03, Kaye's diagram, status terminal.</summary>
    public static class EngineeringContent
    {
        public const string GeneratorPuzzleId = "eng_generator_start";
        public const string RoutingPuzzleId = "eng_power_routing";
        public static readonly string CouplingInstalledFlag = WorldKeys.Story("eng_coupling_installed");
        public static readonly string GeneratorRunningFlag = WorldKeys.Story("eng_generator_running");
        public static readonly string FedArrayFlag = WorldKeys.Story("fed_array");

        public static ItemDefinition Coupling { get; private set; }
        public static AudioLogDefinition LogEng01 { get; private set; }
        public static AudioLogDefinition LogEng02 { get; private set; }
        public static AudioLogDefinition LogEng03 { get; private set; }
        public static TerminalDefinition DiagramNote { get; private set; }
        public static TerminalDefinition StatusTerminal { get; private set; }

        public static void CreateAll(GameContent registry)
        {
            Coupling = DockingBayContent.Item("item_power_coupling", ItemType.Component, "item.coupling");
            LogEng01 = DockingBayContent.Log("log_eng_01", SectorIds.Engineering, "speaker.kaye", "date.day_m33", false, 8);
            LogEng02 = DockingBayContent.Log("log_eng_02", SectorIds.Engineering, "speaker.kaye", "date.day_m15", false, 7);
            LogEng03 = DockingBayContent.Log("log_eng_03", SectorIds.Engineering, "speaker.kaye", "date.day_m6", true, 8);

            DiagramNote = DockingBayContent.Asset<TerminalDefinition>(DockingBayContent.TerminalsFolder, "terminal_eng_diagram");
            DiagramNote.EditorInit("eng_diagram", "terminal.eng_diagram.title", "terminal.eng_diagram.subtitle", null, null,
                new List<TerminalEntry> { new() { titleKey = "terminal.eng_diagram.e1.title", bodyKey = "terminal.eng_diagram.e1.body" } });
            EditorUtility.SetDirty(DiagramNote);

            StatusTerminal = DockingBayContent.Asset<TerminalDefinition>(DockingBayContent.TerminalsFolder, "terminal_eng_status");
            StatusTerminal.EditorInit("eng_status", "terminal.eng_status.title", "terminal.eng_status.subtitle", null, null,
                new List<TerminalEntry>
                {
                    new() { titleKey = "terminal.eng_status.e1.title", bodyKey = "terminal.eng_status.e1.body" },
                    new() { titleKey = "terminal.eng_status.e2.title", bodyKey = "terminal.eng_status.e2.body" },
                    new()
                    {
                        titleKey = "terminal.eng_status.e3.title", bodyKey = "terminal.eng_status.e3.body",
                        visibleWhen = new ConditionSet { conditions = { Condition.Flag(GeneratorRunningFlag) } },
                    },
                });
            EditorUtility.SetDirty(StatusTerminal);

            foreach (var so in new UnityEngine.ScriptableObject[] { Coupling, LogEng01, LogEng02, LogEng03, DiagramNote, StatusTerminal })
                registry.EditorRegister(so);
        }
    }
}
