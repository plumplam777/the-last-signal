using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>Named place where the player can appear in a sector.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId = "default";
        public string SpawnId => spawnId;

#if UNITY_EDITOR
        public void EditorSetId(string id) => spawnId = id;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.37f, 0.9f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward);
        }
#endif
    }
}
