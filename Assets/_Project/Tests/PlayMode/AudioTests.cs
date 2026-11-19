using System.Collections;
using LastSignal.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Generated voices, logs, effects and ambience are present and wired (Tools/generate_*.py).</summary>
    public class AudioTests
    {
        [UnityTest]
        public IEnumerator CairnVoice_ExistsInBothLanguages()
        {
            yield return null;
            foreach (var lang in new[] { Language.English, Language.Bulgarian })
            {
                GameServices.Localization.SetLanguage(lang, persist: false);
                Assert.Greater(GameServices.Audio.PlayVoice("dlg.ai_dock_01.l0"), 1f, $"CAIRN line missing in {lang}");
                Assert.Greater(GameServices.Audio.PlayVoice("cut.open.02"), 1f, $"Distress call missing in {lang}");
                Assert.Greater(GameServices.Audio.PlayVoice("end.a.01"), 0.5f, $"Ending line missing in {lang}");
                GameServices.Audio.StopVoice();
            }
            GameServices.Localization.SetLanguage(Language.English, persist: false);
        }

        [Test]
        public void AudioLogs_HaveRecordingsAndPerLanguageTimings()
        {
            foreach (var log in GameServices.Content.AudioLogs)
            {
                Assert.IsNotNull(log.ClipFor(Language.English), $"{log.Id}: English clip");
                Assert.IsNotNull(log.ClipFor(Language.Bulgarian), $"{log.Id}: Bulgarian clip");
                Assert.AreNotSame(log.ClipFor(Language.English), log.ClipFor(Language.Bulgarian));
                for (int i = 1; i < log.Subtitles.Count; i++)
                {
                    Assert.Greater(log.StartOf(i, Language.English), log.StartOf(i - 1, Language.English));
                    Assert.Greater(log.StartOf(i, Language.Bulgarian), log.StartOf(i - 1, Language.Bulgarian));
                }
                Assert.Less(log.StartOf(log.Subtitles.Count - 1, Language.Bulgarian), log.DurationFor(Language.Bulgarian));
            }
        }

        [Test]
        public void EffectsAndAmbience_Load()
        {
            foreach (var name in new[] { "sfx_footstep_1", "sfx_door_open", "sfx_breaker", "sfx_pickup", "sfx_terminal_open", "sfx_generator_start" })
                Assert.IsNotNull(Resources.Load<AudioClip>($"Sfx/{name}"), name);
            foreach (var name in new[] { "amb_station_hum", "amb_ventilation", "amb_generator", "amb_murmur" })
                Assert.IsNotNull(Resources.Load<AudioClip>($"Ambience/{name}"), name);
        }
    }
}
