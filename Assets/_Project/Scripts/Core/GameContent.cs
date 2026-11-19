using System.Collections.Generic;
using System.Linq;
using LastSignal.Audio;
using LastSignal.Inventory;
using LastSignal.Puzzles;
using LastSignal.Quests;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Registry of every content definition (Resources/GameContent.asset). Saves store ids; this resolves
    /// them back to assets. The database seeds its catalogs from here. Maintained by the content builders.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Signal/Game Content Registry", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject
    {
        public const string ResourcePath = "GameContent";

        [SerializeField] private List<ItemDefinition> items = new();
        [SerializeField] private List<AudioLogDefinition> audioLogs = new();
        [SerializeField] private List<QuestDefinition> quests = new();
        [SerializeField] private List<TerminalDefinition> terminals = new();

        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<AudioLogDefinition> AudioLogs => audioLogs;
        public IReadOnlyList<QuestDefinition> Quests => quests;
        public IReadOnlyList<TerminalDefinition> Terminals => terminals;

        public ItemDefinition FindItem(string id) => items.FirstOrDefault(i => i != null && i.Id == id);
        public AudioLogDefinition FindLog(string id) => audioLogs.FirstOrDefault(l => l != null && l.Id == id);

        public static GameContent Load()
        {
            var content = Resources.Load<GameContent>(ResourcePath);
            if (content == null)
            {
                Log.Warn(nameof(GameContent), $"Resources/{ResourcePath}.asset not found; using an empty registry.");
                content = CreateInstance<GameContent>();
            }
            return content;
        }

#if UNITY_EDITOR
        public void EditorRegister(ScriptableObject definition)
        {
            switch (definition)
            {
                case ItemDefinition i when !items.Contains(i): items.Add(i); break;
                case AudioLogDefinition l when !audioLogs.Contains(l): audioLogs.Add(l); break;
                case QuestDefinition q when !quests.Contains(q): quests.Add(q); break;
                case TerminalDefinition t when !terminals.Contains(t): terminals.Add(t); break;
            }
            items.RemoveAll(x => x == null);
            audioLogs.RemoveAll(x => x == null);
            quests.RemoveAll(x => x == null);
            terminals.RemoveAll(x => x == null);
        }
#endif
    }
}
