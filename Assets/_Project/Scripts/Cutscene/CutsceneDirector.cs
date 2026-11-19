using System.Collections;
using System.Collections.Generic;
using LastSignal.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Cutscene
{
    /// <summary>
    /// Plays a list of <see cref="CutsceneShot"/>s with one camera, fades, subtitles, title cards and a cockpit display,
    /// then loads the next scene. Hold Space (or Esc) to skip. Sets GameState.Cutscene so gameplay input is off.
    /// </summary>
    public sealed class CutsceneDirector : MonoBehaviour
    {
        [SerializeField] private Camera shotCamera;
        [SerializeField] private List<CutsceneShot> shots = new();
        [SerializeField] private string nextScene = SceneIds.DockingBay;
        [SerializeField] private string nextSpawnId = "default";
        [Header("UI")]
        [SerializeField] private Image fade;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text card;
        [SerializeField] private TMP_Text skipHint;
        [SerializeField] private Image skipProgress;
        [SerializeField] private TMP_Text display;
        [SerializeField] private float holdToSkipSeconds = 1.2f;

        private float _skipHold;
        private bool _finished;
        private string _subtitleKey, _cardKey, _displayKey;

        public bool IsFinished => _finished;
        public int CurrentShot { get; private set; } = -1;

        private void Start()
        {
            GameServices.State.Set(GameState.Cutscene);
            GameServices.Localization.LanguageChanged += RefreshTexts;
            StartCoroutine(Play());
        }

        private void OnDestroy()
        {
            if (GameServices.IsReady) GameServices.Localization.LanguageChanged -= RefreshTexts;
        }

        private IEnumerator Play()
        {
            SetFade(1f);
            for (int i = 0; i < shots.Count && !_finished; i++)
            {
                CurrentShot = i;
                var shot = shots[i];
                foreach (var go in shot.activate) if (go != null) go.SetActive(true);
                _subtitleKey = shot.subtitleKey;
                _cardKey = shot.cardKey;
                if (!string.IsNullOrEmpty(shot.displayKey)) _displayKey = shot.displayKey;
                RefreshTexts();
                shotCamera.fieldOfView = shot.fieldOfView;

                float t = 0f;
                while (t < shot.duration && !_finished)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / shot.duration);
                    shotCamera.transform.SetPositionAndRotation(
                        Vector3.Lerp(shot.fromPosition, shot.toPosition, k),
                        Quaternion.Slerp(Quaternion.Euler(shot.fromEuler), Quaternion.Euler(shot.toEuler), k));
                    float alpha = 0f;
                    if (shot.black) alpha = 1f;
                    else
                    {
                        if (shot.fadeIn > 0f && t < shot.fadeIn) alpha = 1f - t / shot.fadeIn;
                        if (shot.fadeOut > 0f && t > shot.duration - shot.fadeOut) alpha = Mathf.Max(alpha, (t - (shot.duration - shot.fadeOut)) / shot.fadeOut);
                    }
                    SetFade(alpha);
                    t += Time.deltaTime;
                    yield return null;
                }
            }
            Finish();
        }

        private void Update()
        {
            if (_finished) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool holding = kb != null && (kb.spaceKey.isPressed || kb.escapeKey.isPressed);
            _skipHold = holding ? _skipHold + Time.unscaledDeltaTime : Mathf.Max(0f, _skipHold - Time.unscaledDeltaTime * 2f);
            if (skipProgress != null) skipProgress.fillAmount = _skipHold / holdToSkipSeconds;
            if (_skipHold >= holdToSkipSeconds) Finish();
        }

        /// <summary>Ends the cutscene and loads the next scene (also used by tests).</summary>
        public void Finish()
        {
            if (_finished) return;
            _finished = true;
            StopAllCoroutines();
            Log.Info(nameof(CutsceneDirector), $"Cutscene finished at shot {CurrentShot + 1}/{shots.Count}.");
            GameServices.Scenes.Load(nextScene, nextSpawnId);
        }

        private void RefreshTexts()
        {
            subtitle.text = string.IsNullOrEmpty(_subtitleKey) ? string.Empty : Loc.Get(_subtitleKey);
            card.text = string.IsNullOrEmpty(_cardKey) ? string.Empty : Loc.Get(_cardKey);
            if (display != null) display.text = string.IsNullOrEmpty(_displayKey) ? string.Empty : Loc.Get(_displayKey);
            if (skipHint != null) skipHint.text = Loc.Get("cut.skip");
        }

        private void SetFade(float a)
        {
            var c = fade.color;
            c.a = Mathf.Clamp01(a);
            fade.color = c;
        }

#if UNITY_EDITOR
        public void EditorBind(Camera cam, List<CutsceneShot> list, Image f, TMP_Text sub, TMP_Text c, TMP_Text skip, Image skipFill, TMP_Text disp)
        {
            shotCamera = cam; shots = list; fade = f; subtitle = sub; card = c; skipHint = skip; skipProgress = skipFill; display = disp;
        }
#endif
    }
}
