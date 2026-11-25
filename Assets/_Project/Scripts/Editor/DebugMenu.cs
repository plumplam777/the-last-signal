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
        [MenuItem(Root + "Open Decision Screen", true)]
        [MenuItem(Root + "Toggle Flashlight", true)]
        [MenuItem(Root + "Look At Hidden Message", true)]
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

        [MenuItem(Root + "Toggle Flashlight")]
        private static void ToggleFlashlight()
        {
            GameServices.World.SetBool(Player.FlashlightController.OwnedFlag, true);
            var f = Player.FlashlightController.Current;
            if (f != null) f.SetOn(!f.IsOn);
        }

        /// <summary>Stands the player 2.2 m in front of the next hidden (beam-reveal) message with the flashlight on.</summary>
        [MenuItem(Root + "Look At Hidden Message")]
        private static void LookAtHiddenMessage()
        {
            var all = Object.FindObjectsByType<Player.BeamRevealText>(FindObjectsSortMode.InstanceID);
            var player = Object.FindAnyObjectByType<Player.PlayerController>();
            if (all.Length == 0 || player == null) return;
            _hiddenMessageIndex = (_hiddenMessageIndex + 1) % all.Length;
            var text = all[_hiddenMessageIndex].transform;
            var stand = text.position - text.forward * 2.2f;
            var eye = new Vector3(stand.x, 1.67f, stand.z);
            var look = text.position - eye;
            float yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(look.y, new Vector2(look.x, look.z).magnitude) * Mathf.Rad2Deg;
            player.Teleport(new Vector3(stand.x, 0.05f, stand.z), yaw, pitch);
            GameServices.World.SetBool(Player.FlashlightController.OwnedFlag, true);
            Player.FlashlightController.Current?.SetOn(true);
        }

        private static int _hiddenMessageIndex = -1;

        [MenuItem(Root + "Open Decision Screen")]
        private static void OpenDecision() => LastSignal.UI.DecisionScreen.RequestOpen();

        [MenuItem(Root + "Restore Sector Power")]
        private static void RestorePower()
        {
            var sector = Object.FindAnyObjectByType<Player.SectorRoot>();
            if (sector != null) GameServices.World.SetBool(WorldKeys.Power(sector.SectorId), true);
        }
    }
}
