using System.Collections;
using LastSignal.Core;
using LastSignal.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Spec §16 "player movement works": WASD through a virtual keyboard moves the real player prefab; sprint is faster; menus freeze it.</summary>
    public class PlayerMovementTests
    {
        private Keyboard _keyboard;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GameServices.StartNewRun();
            SceneManager.LoadScene(SceneIds.DockingBay);
            yield return null;
            yield return null;
            _keyboard = InputSystem.AddDevice<Keyboard>("TestKeyboard");
        }

        [TearDown]
        public void RemoveKeyboard()
        {
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (GameServices.State.Current == GameState.InMenu) GameServices.State.Set(GameState.Playing);
        }

        private IEnumerator Hold(float seconds, params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        private static float Travelled(PlayerController p, Vector3 from)
            => Vector3.ProjectOnPlane(p.transform.position - from, Vector3.up).magnitude;

        [UnityTest]
        public IEnumerator Walk_Forward_ThenSprint_IsFaster()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            Vector3 start = player.transform.position;
            Vector3 forward = player.transform.forward;
            yield return Hold(0.6f, Key.W);
            float walked = Travelled(player, start);
            Assert.Greater(walked, 0.5f, "W moves the player.");
            Assert.Greater(Vector3.Dot(player.transform.position - start, forward), 0f, "…forwards.");
            Assert.Less(Mathf.Abs(player.transform.position.y - start.y), 0.2f, "Stays on the floor.");

            start = player.transform.position;
            yield return Hold(0.6f, Key.W, Key.LeftShift);
            Assert.Greater(Travelled(player, start), walked * 1.2f, "Sprint is faster than walking.");
        }

        [UnityTest]
        public IEnumerator MenuOpen_FreezesMovement()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            GameServices.State.Set(GameState.InMenu);
            Vector3 start = player.transform.position;
            yield return Hold(0.4f, Key.W);
            Assert.Less(Travelled(player, start), 0.01f, "No movement while a menu has focus.");
        }
    }
}
