using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LastSignal.Audio;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Puzzles;
using LastSignal.Quests;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Creates/updates the data assets of the Docking Bay (items, audio logs, terminal, main quest objectives)
    /// and registers them in Resources/GameContent.asset. Text lives in the localization tables; these assets hold keys.
    /// Source: Documentation/Narrative/StoryBible.md §6.1, §8, §11.
    /// </summary>
    public static class DockingBayContent
    {
        public const string ContentPath = "Assets/_Project/Resources/GameContent.asset";
        private const string LogsFolder = "Assets/_Project/ScriptableObjects/AudioLogs";
        private const string QuestsFolder = "Assets/_Project/ScriptableObjects/Quests";
        internal const string TerminalsFolder = "Assets/_Project/ScriptableObjects/Terminals";
        private const string ItemsFolder = "Assets/_Project/ScriptableObjects/Items";

        public static readonly string FuseInstalledFlag = WorldKeys.Story("dock_fuse_installed");

        public static ItemDefinition Flashlight { get; private set; }
        public static ItemDefinition Fuse { get; private set; }
        public static AudioLogDefinition LogDock01 { get; private set; }
        public static AudioLogDefinition LogDock02 { get; private set; }
        public static TerminalDefinition BayTerminal { get; private set; }
        public static QuestDefinition MainQuest { get; private set; }

        public static GameContent Registry()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ContentPath)!);
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<GameContent>();
                AssetDatabase.CreateAsset(content, ContentPath);
            }
            return content;
        }

        public static void CreateAll()
        {
            var registry = Registry();

            Flashlight = Item("item_flashlight", ItemType.Tool, "item.flashlight");
            Fuse = Item("item_fuse_30a", ItemType.Component, "item.fuse");
            // remove the Phase 2 id if it exists (renamed to match the story bible: ITEM_FUSE_30A)
            AssetDatabase.DeleteAsset($"{ItemsFolder}/item_fuse.asset");

            LogDock01 = Log("log_dock_01", SectorIds.Docking, "speaker.jonah", "date.day_m61", critical: true, lineCount: 9);
            LogDock02 = Log("log_dock_02", SectorIds.Docking, "speaker.rhee", "date.day_m5", critical: false, lineCount: 8);

            BayTerminal = Asset<TerminalDefinition>(TerminalsFolder, "terminal_dock_bay");
            BayTerminal.EditorInit("dock_bay", "terminal.dock_bay.title", "terminal.dock_bay.subtitle", null, null,
                new List<TerminalEntry>
                {
                    new() { titleKey = "terminal.dock_bay.e1.title", bodyKey = "terminal.dock_bay.e1.body" },
                    new() { titleKey = "terminal.dock_bay.e2.title", bodyKey = "terminal.dock_bay.e2.body" },
                    new() { titleKey = "terminal.dock_bay.e3.title", bodyKey = "terminal.dock_bay.e3.body" },
                    new()
                    {
                        titleKey = "terminal.dock_bay.e4.title", bodyKey = "terminal.dock_bay.e4.body",
                        visibleWhen = new ConditionSet { conditions = { Condition.Powered(SectorIds.Docking) } },
                    },
                });
            EditorUtility.SetDirty(BayTerminal);

            StationContent.CreateAll(registry); // quest objectives below reference its assets
            MainQuest = Asset<QuestDefinition>(QuestsFolder, "quest_main");
            MainQuest.EditorInit("main", "quest.main.title", true, new List<ObjectiveDefinition>
            {
                Objective("obj_01", new ConditionSet { conditions = { Condition.Item(Flashlight) } }),
                Objective("obj_02", new ConditionSet
                {
                    requireAll = false,
                    conditions = { Condition.Item(Fuse), Condition.Flag(FuseInstalledFlag) },
                }),
                Objective("obj_03", new ConditionSet { conditions = { Condition.Powered(SectorIds.Docking) } }),
                Objective("obj_04", new ConditionSet { conditions = { Condition.Flag(WorldKeys.DoorUnlocked("dock_inner")) } }),
                Objective("obj_04b", new ConditionSet { conditions = { Condition.Flag(WorldKeys.SectorVisited(SectorIds.Engineering)) } }),
                Objective("obj_05", new ConditionSet { conditions = { Condition.Flag(EngineeringContent.CouplingInstalledFlag) } }),
                Objective("obj_06", new ConditionSet { conditions = { new Condition { type = ConditionType.PuzzleSolved, key = EngineeringContent.GeneratorPuzzleId } } }),
                Objective("obj_07", new ConditionSet { conditions = { new Condition { type = ConditionType.PuzzleSolved, key = EngineeringContent.RoutingPuzzleId } } }),
                Objective("obj_07b", new ConditionSet { conditions = { Condition.Flag(WorldKeys.SectorVisited(SectorIds.Medical)) } }),
                // Phase 6 (StoryBible §11)
                Objective("obj_08", Solved(StationContent.PatientTerminalPuzzleId)),
                Objective("obj_09", new ConditionSet { conditions = { Condition.Item(StationContent.KeycardResearch) } }),
                Objective("obj_10", new ConditionSet { conditions = { Condition.Flag(WorldKeys.SectorVisited(SectorIds.Research)) } }),
                Objective("obj_11", Solved(StationContent.DecodePuzzleId)),
                Objective("obj_12", new ConditionSet { conditions = { Condition.Item(StationContent.KeycardCore) } }),
                Objective("obj_13", new ConditionSet { conditions = { Condition.Flag(WorldKeys.SectorVisited(SectorIds.Reactor)) } }),
                Objective("obj_14", Solved(StationContent.ReactorPuzzleId)),
                Objective("obj_14b", new ConditionSet { conditions = { Condition.Flag(StationContent.CommandReachedFlag) } }),
                Objective("obj_15", new ConditionSet { conditions = { new Condition { type = ConditionType.LogCollected, key = "log_core_02" } } }),
                Objective("obj_16", new ConditionSet { conditions = { Condition.Flag(WorldKeys.Story("ending_reached")) } }),
                Objective("obj_17a", new ConditionSet { conditions = { Condition.Flag(WorldKeys.Story("escaped")) } }),
            });
            EditorUtility.SetDirty(MainQuest);

            EngineeringContent.CreateAll(registry);
            DialogueContent.CreateAll();
            foreach (var so in new ScriptableObject[] { Flashlight, Fuse, LogDock01, LogDock02, BayTerminal, MainQuest })
                registry.EditorRegister(so);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        public static void SetInspectPrefab(ItemDefinition item, GameObject prefab)
        {
            var so = new SerializedObject(item);
            so.FindProperty("inspectPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ConditionSet Solved(string puzzleId)
            => new() { conditions = { new Condition { type = ConditionType.PuzzleSolved, key = puzzleId } } };

        private static ObjectiveDefinition Objective(string id, ConditionSet completeWhen)
            => new() { id = id, textKey = $"obj.{id}", completeWhen = completeWhen };

        internal static ItemDefinition Item(string id, ItemType type, string keyPrefix)
        {
            var item = Asset<ItemDefinition>(ItemsFolder, id);
            item.EditorInit(id, type, $"{keyPrefix}.name", $"{keyPrefix}.desc");
            EditorUtility.SetDirty(item);
            return item;
        }

        /// <summary>
        /// Builds a log whose subtitle timings come from the English line lengths (≈2.6 words/s), so subtitles
        /// stay readable before voice audio exists. Keys: log.&lt;id&gt;.title, log.&lt;id&gt;.line.N.
        /// </summary>
        internal static AudioLogDefinition Log(string id, string sector, string speakerKey, string dateKey, bool critical, int lineCount)
        {
            var en = LocalizationTable("en");
            var lines = new List<SubtitleLine>();
            float t = 0.6f;
            for (int i = 1; i <= lineCount; i++)
            {
                var key = $"log.{id}.line.{i}";
                lines.Add(new SubtitleLine { start = t, textKey = key });
                int words = en.TryGetValue(key, out var text) ? Regex.Matches(text, @"\S+").Count : 8;
                t += Mathf.Max(1.8f, words / 2.6f) + 0.35f;
            }
            // generated voice recordings (Tools/generate_voices.py): use their exact per-line timings
            var manifest = VoiceManifest();
            AudioClip enClip = null, bgClip = null;
            if (manifest != null && manifest["logs"]?[id] is Newtonsoft.Json.Linq.JObject entry)
            {
                enClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/en/log_{id}.wav");
                bgClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/bg/log_{id}.wav");
                var enStarts = entry["en"]?["starts"]?.ToObject<float[]>();
                var bgStarts = entry["bg"]?["starts"]?.ToObject<float[]>();
                for (int i = 0; i < lines.Count; i++)
                {
                    if (enStarts != null && i < enStarts.Length) lines[i].start = enStarts[i];
                    if (bgStarts != null && i < bgStarts.Length) lines[i].startBg = bgStarts[i];
                }
                if (entry["en"]?["duration"] != null) t = (float)entry["en"]["duration"] - 0.8f;
            }
            var log = Asset<AudioLogDefinition>(LogsFolder, id);
            log.EditorInit(id, sector, $"log.{id}.title", speakerKey, dateKey, critical, t + 0.8f, lines);
            log.EditorSetClips(enClip, bgClip);
            EditorUtility.SetDirty(log);
            return log;
        }

        public const string VoiceFolder = "Assets/_Project/Audio/Resources/Voice";

        private static Newtonsoft.Json.Linq.JObject VoiceManifest()
        {
            var path = $"{VoiceFolder}/voice_manifest.json";
            return File.Exists(path) ? Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path)) : null;
        }

        public static Dictionary<string, string> LocalizationTable(string code)
        {
            var path = $"Assets/_Project/Localization/Resources/Localization/{code}.json";
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
        }

        internal static T Asset<T>(string folder, string name) where T : ScriptableObject
        {
            Directory.CreateDirectory(folder);
            var path = $"{folder}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
