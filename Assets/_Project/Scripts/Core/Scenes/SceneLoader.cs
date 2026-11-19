using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>Scene names. Must match the scenes listed in Build Settings.</summary>
    public static class SceneIds
    {
        public const string Bootstrap = "Bootstrap";
        public const string MainMenu = "MainMenu";
        public const string CutsceneOpening = "Cutscene_Opening";
        public const string DockingBay = "Sector_DockingBay";
        public const string Engineering = "Sector_Engineering";
        public const string Medical = "Sector_Medical";
        public const string Research = "Sector_Research";
        public const string ReactorCommand = "Sector_ReactorCommand";
        public const string Ending = "Ending";
    }

    /// <summary>
    /// Loads one scene at a time behind a fade. Callers decide which scene; the player spawner
    /// in the new scene reads <see cref="PendingSpawnId"/>.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private const float FadeSeconds = 0.35f;

        private CanvasGroup _fade;
        private bool _busy;

        public bool IsLoading => _busy;

        /// <summary>Spawn point the next scene should place the player at (null = scene default).</summary>
        public string PendingSpawnId { get; private set; }

        public event Action<string> LoadStarted;
        public event Action<string> LoadCompleted;

        private void Awake() => CreateFadeOverlay();

        public void Load(string sceneName, string spawnId = null)
        {
            if (_busy)
            {
                Log.Warn(nameof(SceneLoader), $"Ignoring load of '{sceneName}' while another load is running.");
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Log.Error(nameof(SceneLoader), $"Scene '{sceneName}' is not in Build Settings.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName, spawnId));
        }

        public void ConsumeSpawnId() => PendingSpawnId = null;

        private IEnumerator LoadRoutine(string sceneName, string spawnId)
        {
            _busy = true;
            PendingSpawnId = spawnId;
            LoadStarted?.Invoke(sceneName);

            yield return Fade(1f);
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Log.Error(nameof(SceneLoader), $"LoadSceneAsync returned null for '{sceneName}'.");
                yield return Fade(0f);
                _busy = false;
                yield break;
            }
            while (!op.isDone) yield return null;

            LoadCompleted?.Invoke(sceneName);
            yield return Fade(0f);
            _busy = false;
        }

        private IEnumerator Fade(float target)
        {
            _fade.blocksRaycasts = true;
            float start = _fade.alpha;
            for (float t = 0; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                _fade.alpha = Mathf.Lerp(start, target, t / FadeSeconds);
                yield return null;
            }
            _fade.alpha = target;
            _fade.blocksRaycasts = target > 0.01f;
        }

        private void CreateFadeOverlay()
        {
            var go = new GameObject("FadeOverlay", typeof(Canvas), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            _fade = go.GetComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
            _fade.interactable = false;

            var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(go.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            image.GetComponent<Image>().color = Color.black;
        }
    }
}
