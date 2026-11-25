using System.Collections;
using System.IO;
using LastSignal.Core;
using LastSignal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Start screen look (user feedback 2026-11-25): the menu column always stays on screen, and every other page
    /// slides out beside it on a glass panel; the item that opened it keeps its highlight frame.
    /// </summary>
    public class StartScreenLookTests
    {
        private static void Click(string path) => GameObject.Find(path).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

        [UnityTest]
        public IEnumerator OptionsPopOutBesideTheMenu_OnAGlassPanel()
        {
            SceneManager.LoadScene(SceneIds.MainMenu);
            yield return null;
            yield return null;
            var frame = Object.FindAnyObjectByType<StartScreen>().GetComponentInChildren<PopOutPanel>(true).gameObject;
            Assert.IsNotNull(frame);
            Assert.IsFalse(frame.activeSelf, "No side panel on the main page.");

            Click("Main/Settings");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(frame.activeInHierarchy, "Settings pops out.");
            var pop = frame.GetComponent<PopOutPanel>();
            Assert.IsFalse(pop.IsAnimating, "The slide-out finishes.");
            Assert.AreEqual(1f, frame.GetComponent<CanvasGroup>().alpha, 0.001f);
            Assert.IsTrue(frame.GetComponent<CanvasGroup>().interactable);
            Assert.Less(frame.GetComponent<UnityEngine.UI.Image>().color.a, 0.7f, "The panel is see-through glass.");
            Assert.IsTrue(GameObject.Find("Main/NewGame").activeInHierarchy, "The menu column stays on screen.");
            Assert.IsTrue(GameObject.Find("Main/Settings").GetComponent<MenuItemSelect>().IsHighlighted, "Settings keeps its frame.");
            var menuRight = ((RectTransform)GameObject.Find("Main/Settings").transform).position.x;
            Assert.Greater(frame.transform.position.x, menuRight, "The panel sits to the side of the menu.");

            SaveScreenshot("lastsignal_menu_popout.png");

            Click("Settings/Back");
            yield return null;
            Assert.IsFalse(frame.activeSelf, "Back closes the side panel.");
            Assert.IsFalse(GameObject.Find("Main/Settings").GetComponent<MenuItemSelect>().Pinned);
        }

        /// <summary>Renders the menu camera with the UI moved onto it (overlay canvases skip camera renders) into the temp folder.</summary>
        private static void SaveScreenshot(string file)
        {
            var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.05f;
            }
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            var path = Path.Combine(Path.GetTempPath(), file);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Debug.Log($"[StartScreenLookTests] screenshot: {path}");
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceCamera) c.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }
}
