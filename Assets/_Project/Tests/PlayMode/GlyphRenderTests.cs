using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Regression (2026-11-20): a build wiped the dynamic fallback font that holds the Cyrillic glyphs, so the whole
    /// menu went blank in Bulgarian. Every visible character must resolve to a glyph with a real rect in a real atlas.
    /// </summary>
    public class GlyphRenderTests
    {
        private static readonly string[] Scenes = { SceneIds.MainMenu, SceneIds.DockingBay, SceneIds.Research };
        private static readonly Language[] Languages = { Language.Bulgarian, Language.English };

        [UnityTest]
        public IEnumerator EveryCharacterHasARealGlyph(
            [ValueSource(nameof(Scenes))] string scene, [ValueSource(nameof(Languages))] Language language)
        {
            GameServices.StartNewRun();
            GameServices.Localization.SetLanguage(language, persist: false);
            yield return Phase6.Load(scene);
            yield return null;

            var problems = new List<string>();
            int visible = 0;
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                t.ForceMeshUpdate(true);
                var info = t.textInfo;
                for (int i = 0; i < info.characterCount; i++)
                {
                    var ci = info.characterInfo[i];
                    if (!ci.isVisible) continue;
                    visible++;
                    if (ci.elementType != TMP_TextElementType.Character) continue;
                    var glyph = ci.textElement?.glyph;
                    var font = ci.fontAsset;
                    if (glyph == null || font == null || font.atlasTextures == null)
                    {
                        problems.Add($"'{ci.character}' has no glyph in {t.name}");
                        continue;
                    }
                    var atlas = glyph.atlasIndex < font.atlasTextures.Length ? font.atlasTextures[glyph.atlasIndex] : null;
                    if (atlas == null || atlas.width <= 1 || glyph.glyphRect.width <= 0 ||
                        glyph.glyphRect.x + glyph.glyphRect.width > atlas.width)
                        problems.Add($"'{ci.character}' (font {font.name}) has an empty/invalid atlas entry in {t.name}");
                }
            }
            GameServices.Localization.SetLanguage(Language.English, persist: false);
            Assert.Greater(visible, 0, "no visible text found");
            Assert.IsEmpty(problems, string.Join("\n", problems.Distinct().Take(30)));
        }
    }
}
