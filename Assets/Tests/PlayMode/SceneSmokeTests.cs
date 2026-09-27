using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizardArena.Enemies;
using WizardArena.Player;

namespace WizardArena.Tests.PlayMode
{
    // Pattern for PlayMode tests: load the generated scene (it is in Build Settings),
    // let it run, then assert on state. Any logged error or exception fails the test.
    public sealed class SceneSmokeTests
    {
        private const string SceneName = "WizardMovement";

        [UnityTest]
        public IEnumerator DemoScene_RunsForThreeSeconds_WithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            yield return new WaitForSeconds(3f);

            Assert.IsNotNull(Object.FindFirstObjectByType<WizardController>(), "wizard");
            Assert.IsNotNull(Object.FindFirstObjectByType<BatEnemyController>(), "bat");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
