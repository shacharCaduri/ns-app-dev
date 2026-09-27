using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizardArena.Stage;
using WizardArena.UI;

namespace WizardArena.Tests.PlayMode.UI
{
    // Drives the generated main menu scene, built by MenuSetup.
    public sealed class MainMenuTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator Play_LoadsTheArena_AtStageOne()
        {
            Object.FindFirstObjectByType<MainMenuController>().Play();
            yield return null;
            yield return null;

            StageRunner runner = Object.FindFirstObjectByType<StageRunner>();
            Assert.IsNotNull(runner, "the arena scene loaded");
            Assert.AreEqual("Ruins Antechamber", runner.CurrentStage.DisplayName, "starts at stage 1");
            Assert.AreEqual(1, WizardArena.Enemies.EnemyRegistry.AliveCount, "stage 1's one bat");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
