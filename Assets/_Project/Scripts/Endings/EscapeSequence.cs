using System.Collections;
using LastSignal.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static LastSignal.UI.RuntimeUi;

namespace LastSignal.Endings
{
    /// <summary>
    /// Ending A gameplay (StoryBible §9.3): after "destroy" is confirmed the reactor overloads and the player has
    /// <see cref="Seconds"/> to get from Command back to the Tern (lift → reactor → service tunnel → Docking Bay).
    /// CAIRN's lines play live. At trust ≥ 50 CAIRN opens the tunnel; below 50 it is sealed and needs the manual crank.
    /// Runs across scene loads; re-arms itself from WorldState after a load (choice.final = destroy, not yet escaped).
    /// Running out of time reloads the last checkpoint.
    /// </summary>
    public sealed class EscapeSequence : MonoBehaviour
    {
        public const float Seconds = 120f;
        public static readonly string TunnelOpenFlag = WorldKeys.Story("escape_tunnel_open");
        public static readonly string EscapedFlag = WorldKeys.Story("escaped");
        public static readonly string ActiveFlag = WorldKeys.Story("escape_active");

        public static EscapeSequence Instance { get; private set; }

        private GameObject _overlay;
        private TMP_Text _timer, _speaker, _line, _failText;
        private Image _failFade;
        private float _remaining;
        private bool _running;
        private bool _failing;
        private Coroutine _lines;

        public bool IsRunning => _running;
        public float Remaining => _remaining;

        /// <summary>True when the run is in the escape phase (decided "destroy", not yet aboard the Tern).</summary>
        public static bool Pending
            => GameServices.World.GetString(EndingScript.ChoiceKey) == EndingScript.Id(EndingType.Destroy)
               && !GameServices.World.GetBool(EscapedFlag);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("[EscapeSequence]");
            DontDestroyOnLoad(go);
            go.AddComponent<EscapeSequence>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildOverlay();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        private static bool IsStationScene(string scene) => scene.StartsWith("Sector_");

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsStationScene(scene.name) || !Pending)
            {
                StopRun();
                return;
            }
            if (!_running) Begin(fresh: false);
        }

        /// <summary>Called when "destroy" is confirmed (and after a load during the escape).</summary>
        public void Begin(bool fresh = true)
        {
            _remaining = Seconds;
            _running = true;
            _failing = false;
            _overlay.SetActive(true);
            _failText.text = string.Empty;
            SetFail(0f);
            var world = GameServices.World;
            world.SetBool(ActiveFlag, true);
            bool high = EndingScript.HighTrust(world.GetInt(WorldKeys.AiTrust, WorldKeys.AiTrustDefault));
            if (high) world.SetBool(TunnelOpenFlag, true);
            GameServices.Audio.Sfx("sfx_alarm", 0.8f);
            if (_lines != null) StopCoroutine(_lines);
            _lines = StartCoroutine(PlayLines(high, fresh));
            Log.Info(nameof(EscapeSequence), $"Reactor overload countdown started ({(high ? "high" : "low")} trust).");
        }

        private IEnumerator PlayLines(bool high, bool fresh)
        {
            yield return new WaitForSeconds(1.2f);
            if (fresh) yield return Say("speaker.cairn", "end.a.01");
            yield return Say("speaker.cairn", high ? "end.a.02_high" : "end.a.02_low");
            yield return new WaitForSeconds(2f);
            // low trust: CAIRN plays the edited fragment of the captain's call
            yield return Say(high ? "speaker.cairn" : "speaker.dunn", high ? "end.a.03_high" : "end.a.03_low");
            _lines = null;
        }

        private IEnumerator Say(string speakerKey, string key)
        {
            _speaker.text = Loc.Get(speakerKey);
            _line.text = Loc.Get(key);
            float voice = GameServices.Audio.PlayVoice(key);
            yield return new WaitForSeconds(Mathf.Max(voice + 0.6f, Dialogue.DialogueManager.ReadingTime(_line.text) + 0.4f));
            _speaker.text = _line.text = string.Empty;
        }

        /// <summary>The player reached the Tern: last line, then the Ending scene.</summary>
        public void Board()
        {
            if (!_running || _failing) return;
            StartCoroutine(BoardRoutine());
        }

        private IEnumerator BoardRoutine()
        {
            _running = false;
            if (_lines != null) StopCoroutine(_lines);
            GameServices.World.SetBool(EscapedFlag, true);
            GameServices.World.SetBool(ActiveFlag, false);
            GameServices.Audio.Sfx("sfx_docking_clamp", 0.9f);
            yield return Say("speaker.cairn", "end.a.04");
            _overlay.SetActive(false);
            GameServices.State.Set(GameState.Ending);
            GameServices.Scenes.Load(SceneIds.Ending);
        }

        /// <summary>Stops the countdown without an outcome (new run, tests).</summary>
        public void Cancel() => StopRun();

        /// <summary>Test/debug hook: jump the countdown.</summary>
        public void SetRemaining(float seconds) => _remaining = seconds;

        private void StopRun()
        {
            _running = false;
            _failing = false;
            if (_lines != null) StopCoroutine(_lines);
            _lines = null;
            if (_overlay != null) _overlay.SetActive(false);
        }

        private void Update()
        {
            if (!_running || _failing) return;
            // the overload does not wait for terminals or the inventory; only the pause menu stops it
            if (GameServices.State.Current is GameState.Playing or GameState.InMenu) _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                StartCoroutine(Fail());
            }
            int s = Mathf.CeilToInt(_remaining);
            _timer.text = Loc.Get("escape.timer", s / 60, (s % 60).ToString("00"));
            _timer.color = _remaining < 30f && Mathf.Repeat(Time.time, 1f) < 0.5f ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.72f, 0.42f);
            if (_remaining < 30f && Mathf.Repeat(_remaining, 5f) > Mathf.Repeat(_remaining + Time.deltaTime, 5f))
                GameServices.Audio.Sfx("sfx_alarm", 0.35f);
        }

        private IEnumerator Fail()
        {
            _failing = true;
            if (_lines != null) StopCoroutine(_lines);
            _speaker.text = _line.text = string.Empty;
            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                SetFail(t / 1.5f);
                yield return null;
            }
            SetFail(1f);
            _failText.text = Loc.Get("escape.failed");
            yield return new WaitForSecondsRealtime(4f);
            _failText.text = string.Empty;
            _running = false;
            _overlay.SetActive(false);
            SetFail(0f);
            if (!GameServices.Saves.Continue())
                GameServices.Scenes.Load(SceneIds.ReactorCommand, "command");
        }

        private void SetFail(float a)
        {
            var c = _failFade.color;
            c.a = a;
            _failFade.color = c;
        }

        private void BuildOverlay()
        {
            var canvas = Canvas("EscapeOverlay", transform, 40).transform;
            _overlay = canvas.gameObject;
            _failFade = Image("FailFade", canvas, Vector2.zero, new Vector2(4000, 3000), new Color(0f, 0f, 0f, 0f));
            _failFade.raycastTarget = false;
            var top = new Vector2(0.5f, 1f);
            Text("Label", canvas, new Vector2(0, -40), new Vector2(900, 34), 22, new Color(1f, 0.45f, 0.38f), "escape.label", TextAlignmentOptions.Center, top).characterSpacing = 6;
            _timer = Text("Timer", canvas, new Vector2(0, -90), new Vector2(600, 70), 60, new Color(1f, 0.72f, 0.42f), null, TextAlignmentOptions.Center, top);
            var bottom = new Vector2(0.5f, 0f);
            _speaker = Text("Speaker", canvas, new Vector2(0, 190), new Vector2(1400, 34), 24, new Color(0.37f, 0.9f, 1f), null, TextAlignmentOptions.Center, bottom);
            _line = Text("Line", canvas, new Vector2(0, 135), new Vector2(1400, 80), 28, Color.white, null, TextAlignmentOptions.Center, bottom);
            _failText = Text("Failed", canvas, Vector2.zero, new Vector2(1400, 120), 40, new Color(1f, 0.72f, 0.42f));
            _overlay.SetActive(false);
        }
    }
}
