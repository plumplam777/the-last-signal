using System;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Per-machine player preferences (PlayerPrefs): mouse sensitivity, invert Y, volumes.
    /// Gameplay reads values from here; the settings UI writes them.
    /// </summary>
    public sealed class SettingsService
    {
        private const string SensitivityKey = "ls.settings.mouseSensitivity";
        private const string InvertYKey = "ls.settings.invertY";
        private const string MasterVolumeKey = "ls.settings.masterVolume";

        public const float SensitivityMin = 0.03f;
        public const float SensitivityMax = 0.6f;
        public const float SensitivityDefault = 0.2f;

        public float MouseSensitivity { get; private set; }
        public bool InvertY { get; private set; }
        public float MasterVolume { get; private set; }

        public event Action Changed;

        public SettingsService()
        {
            MouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, SensitivityDefault), SensitivityMin, SensitivityMax);
            InvertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            AudioListener.volume = MasterVolume;
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
