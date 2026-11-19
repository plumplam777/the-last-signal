using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>Lives in the Bootstrap scene (build index 0): services are already up, go to the main menu.</summary>
    public sealed class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] private string firstScene = SceneIds.MainMenu;

        private void Start()
        {
            GameServices.State.Set(GameState.Boot);
            GameServices.Scenes.Load(firstScene);
        }
    }
}
