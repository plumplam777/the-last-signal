using System;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Per-machine player preferences (PlayerPrefs): mouse sensitivity, invert Y, volumes, graphics quality.
    /// Gameplay reads values from here; the settings UI writes them.
    /// </summary>
    public sealed class SettingsService
    {
        private const string SensitivityKey = "ls.settings.mouseSensitivity";
        private const string InvertYKey = "ls.settings.invertY";
        private const string MasterVolumeKey = "ls.settings.masterVolume";
        private const string QualityKey = "ls.settings.quality";

        /// <summary>Quality levels as built by the editor (Low, Medium, High); Medium is the default.</summary>
        public const int QualityDefault = 1;

        public const float SensitivityMin = 0.03f;
        public const float SensitivityMax = 0.6f;
        public const float SensitivityDefault = 0.2f;

        public float MouseSensitivity { get; private set; }
        public bool InvertY { get; private set; }
        public float MasterVolume { get; private set; }
        public int QualityLevel { get; private set; }

        public event Action Changed;

        public SettingsService()
        {
            MouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, SensitivityDefault), SensitivityMin, SensitivityMax);
            InvertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            AudioListener.volume = MasterVolume;
            QualityLevel = ClampQuality(PlayerPrefs.GetInt(QualityKey, QualityDefault));
            QualitySettings.SetQualityLevel(QualityLevel, true);
        }

        private static int ClampQuality(int level) => Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        public void SetQualityLevel(int level)
        {
            QualityLevel = ClampQuality(level);
            QualitySettings.SetQualityLevel(QualityLevel, true);
            PlayerPrefs.SetInt(QualityKey, QualityLevel);
            Save();
        }

        public void SetMouseSensitivity(float value)
        {
            MouseSensitivity = Mathf.Clamp(value, SensitivityMin, SensitivityMax);
            PlayerPrefs.SetFloat(SensitivityKey, MouseSensitivity);
            Save();
        }

        public void SetInvertY(bool value)
        {
            InvertY = value;
            PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
            Save();
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            Save();
        }

        private void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
