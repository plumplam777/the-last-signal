using System.Linq;
using LastSignal.Core;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// One per sector scene. Spawns the player prefab at the requested spawn point, marks the sector visited
    /// and switches the game to the Playing state.
    /// </summary>
    public sealed class SectorRoot : MonoBehaviour
    {
        [SerializeField] private string sectorId = SectorIds.Docking;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private string defaultSpawnId = "default";
        [Tooltip("Starts the sector already powered (debug / sectors without a power puzzle).")]
        [SerializeField] private bool startPowered;

        public string SectorId => sectorId;
        public PlayerController Player { get; private set; }

        private void Start()
        {
            if (startPowered) GameServices.World.SetBool(WorldKeys.Power(sectorId), true);
            GameServices.World.SetBool(WorldKeys.SectorVisited(sectorId), true);
            SpawnPlayer();
            GameServices.State.Set(GameState.Playing);
        }

        private void SpawnPlayer()
        {
            Player = FindAnyObjectByType<PlayerController>();
            if (Player == null)
            {
                if (playerPrefab == null)
                {
                    Log.Error(nameof(SectorRoot), $"Sector '{sectorId}' has no player prefab assigned.", this);
                    return;
                }
                Player = Instantiate(playerPrefab).GetComponent<PlayerController>();
            }

            var requested = GameServices.Scenes.PendingSpawnId;
            GameServices.Scenes.ConsumeSpawnId();
            var spawns = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            var spawn = spawns.FirstOrDefault(s => s.SpawnId == requested)
                        ?? spawns.FirstOrDefault(s => s.SpawnId == defaultSpawnId)
                        ?? spawns.FirstOrDefault();
            if (spawn == null)
            {
                Log.Warn(nameof(SectorRoot), $"No spawn point in sector '{sectorId}'; player stays at origin.", this);
                return;
            }
            Player.Teleport(spawn.transform.position, spawn.transform.eulerAngles.y);
        }

#if UNITY_EDITOR
        public void EditorConfigure(string sector, GameObject prefab)
        {
            sectorId = sector;
            playerPrefab = prefab;
        }
#endif
    }
}
