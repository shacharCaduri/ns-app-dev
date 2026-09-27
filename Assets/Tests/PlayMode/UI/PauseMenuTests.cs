using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizardArena.UI;

namespace WizardArena.Tests.PlayMode.UI
{
    // Drives the real scene's pause menu, built by UISetup.
    public sealed class PauseMenuTests
    {
        private PauseMenuController pause;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            pause = Object.FindFirstObjectByType<PauseMenuController>();
        }

        [UnityTearDown]
        public IEnumerator ResetTimescale()
        {
            // In case a test leaves it paused: later tests/scenes must not inherit a frozen clock.
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TogglePause_FreezesTime_AndResumeRestoresIt()
        {
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(pause.IsPaused);

            pause.TogglePause();
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            pause.Resume();
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
