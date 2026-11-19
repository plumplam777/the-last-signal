using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace LastSignal.Core
{
    public enum Language
    {
        English = 0,
        Bulgarian = 1,
    }

    /// <summary>
    /// EN/BG string tables loaded from Resources/Localization/{en,bg}.json (flat "key": "text" objects).
    /// Missing keys fall back to English, then to "#key#", and are reported once.
    /// </summary>
    public sealed class LocalizationService
    {
        private const string LogTag = nameof(LocalizationService);
        private const string LanguagePrefKey = "ls.language";

        private Dictionary<string, string> _current = new();
        private Dictionary<string, string> _fallback = new();
        private readonly HashSet<string> _reportedMissing = new();

        public Language Current { get; private set; } = Language.English;
        public CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;

        public event Action LanguageChanged;

        public void Initialize()
        {
            _fallback = LoadTable(Language.English);
            var defaultLanguage = Application.systemLanguage == SystemLanguage.Bulgarian
                ? Language.Bulgarian
                : Language.English;
            var saved = (Language)PlayerPrefs.GetInt(LanguagePrefKey, (int)defaultLanguage);
            if (!Enum.IsDefined(typeof(Language), saved)) saved = Language.English;
            SetLanguage(saved, persist: false, force: true);
        }

        public void SetLanguage(Language language, bool persist = true, bool force = false)
        {
            if (!force && language == Current) return;
            Current = language;
            Culture = new CultureInfo(Code(language));
            _current = language == Language.English ? _fallback : LoadTable(language);
            _reportedMissing.Clear();
            if (persist)
            {
                PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
                PlayerPrefs.Save();
            }
            LanguageChanged?.Invoke();
        }

        public void ToggleLanguage()
            => SetLanguage(Current == Language.English ? Language.Bulgarian : Language.English);

        public bool Has(string key) => !string.IsNullOrEmpty(key) && (_current.ContainsKey(key) || _fallback.ContainsKey(key));

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_current.TryGetValue(key, out var text)) return text;
            if (_fallback.TryGetValue(key, out text))
            {
                ReportMissing(key, $"missing in '{Code(Current)}', using English");
                return text;
            }
            ReportMissing(key, "missing in all tables");
            return $"#{key}#";
        }

        public string Get(string key, params object[] args)
        {
            var format = Get(key);
            if (args == null || args.Length == 0) return format;
            try
            {
                return string.Format(Culture, format, args);
            }
            catch (FormatException)
            {
                Log.Warn(LogTag, $"Bad format string for key '{key}'.");
                return format;
            }
        }

        public static string Code(Language language) => language switch
        {
            Language.Bulgarian => "bg",
            _ => "en",
        };

        private static Dictionary<string, string> LoadTable(Language language)
        {
            var path = $"Localization/{Code(language)}";
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Log.Error(LogTag, $"String table not found at Resources/{path}.json");
                return new Dictionary<string, string>();
            }
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(asset.text)
                       ?? new Dictionary<string, string>();
            }
            catch (JsonException e)
            {
                Log.Error(LogTag, $"String table '{path}' is not valid JSON: {e.Message}");
                return new Dictionary<string, string>();
            }
        }

        private void ReportMissing(string key, string detail)
        {
            if (_reportedMissing.Add(key)) Log.Warn(LogTag, $"Key '{key}' {detail}.");
        }
    }

    /// <summary>Short-hand for <c>GameServices.Localization.Get</c>.</summary>
    public static class Loc
    {
        public static string Get(string key) => GameServices.Localization.Get(key);
        public static string Get(string key, params object[] args) => GameServices.Localization.Get(key, args);
    }
}
