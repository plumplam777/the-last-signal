using System.Linq;
using System.Reflection;
using LastSignal.Core;
using LastSignal.Interaction;
using LastSignal.Puzzles;
using LastSignal.UI;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>Play-mode shortcuts for testing and screenshots (Editor only; the admin panel replaces this in builds).</summary>
    public static class DebugMenu
    {
        private const string Root = "Last Signal/Debug/";

        [MenuItem(Root + "Open Inventory", true)]
        [MenuItem(Root + "Open First Terminal", true)]
        [MenuItem(Root + "Play First Audio Log", true)]
        [MenuItem(Root + "Give All Items", true)]
        [MenuItem(Root + "Restore Sector Power", true)]
        private static bool PlayingOnly() => Application.isPlaying;

        [MenuItem(Root + "Open Inventory")]
        private static void OpenInventory()
        {
            var screen = Object.FindAnyObjectByType<InventoryScreen>();
            typeof(UiScreen).GetMethod("TryOpen", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(screen, null);
        }

        [MenuItem(Root + "Open First Terminal")]
        private static void OpenTerminal() => Object.FindAnyObjectByType<TerminalInteractable>()?.Interact(null);

        [MenuItem(Root + "Play First Audio Log")]
        private static void PlayLog()
        {
            var log = GameServices.Content.AudioLogs.FirstOrDefault();
            if (log != null) GameServices.AudioLogs.Collect(log);
        }

        [MenuItem(Root + "Give All Items")]
        private static void GiveAll()
        {
            foreach (var item in GameServices.Content.Items) GameServices.Inventory.Add(item);
            GameServices.World.SetBool(Player.FlashlightController.OwnedFlag, true);
        }

        [MenuItem(Root + "Restore Sector Power")]
        private static void RestorePower()
        {
            var sector = Object.FindAnyObjectByType<Player.SectorRoot>();
            if (sector != null) GameServices.World.SetBool(WorldKeys.Power(sector.SectorId), true);
        }
    }
}
