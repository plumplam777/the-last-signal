using LastSignal.Inventory;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// The one access point to the persistent game systems. Created automatically before the first
    /// scene loads (so any scene can be played directly in the Editor) and kept across scene loads.
    /// It wires systems together but contains no gameplay rules itself.
    /// New managers are added here as they are implemented (see Documentation/TechnicalArchitecture.md §3).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameServices : MonoBehaviour
    {
        private static GameServices _instance;

        private GameStateMachine _state;
        private WorldState _world;
        private LocalizationService _localization;
        private InputService _input;
        private SceneLoader _scenes;
        private InventoryManager _inventory;

        public static bool IsReady => _instance != null;

        public static GameStateMachine State => Require()._state;
        public static WorldState World => Require()._world;
        public static LocalizationService Localization => Require()._localization;
        public static InputService Input => Require()._input;
        public static SceneLoader Scenes => Require()._scenes;
        public static InventoryManager Inventory => Require()._inventory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStartup()
        {
            if (_instance != null) return;
            var go = new GameObject("[GameServices]");
            go.AddComponent<GameServices>();
        }

        private static GameServices Require()
        {
            if (_instance == null)
            {
                // Can happen if something is accessed from an editor script or during shutdown.
                CreateOnStartup();
            }
            return _instance;
        }

        /// <summary>Clears all run state (world facts, inventory) for a brand-new game.</summary>
        public static void StartNewRun()
        {
            var s = Require();
            s._world.Clear();
            s._inventory.Clear();
            s._world.SetInt(WorldKeys.AiTrust, WorldKeys.AiTrustDefault);
            Log.Info(nameof(GameServices), "New run started.");
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _state = new GameStateMachine();
            _world = new WorldState();
            _localization = new LocalizationService();
            _localization.Initialize();
            _input = new InputService();
            _scenes = gameObject.AddComponent<SceneLoader>();
            _inventory = new InventoryManager();

            _state.Changed += (_, next) => _input.ApplyState(next);
            Log.Info(nameof(GameServices), "Services initialised.");
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            _input?.Dispose();
            _instance = null;
        }
    }
}
