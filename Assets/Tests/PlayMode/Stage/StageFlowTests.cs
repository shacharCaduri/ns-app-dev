using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.Player;
using WizardArena.Stage;

namespace WizardArena.Tests.PlayMode.Stage
{
    // Drives the generated scene's stage flow: the portal, the game session and the wizard.
    public sealed class StageFlowTests
    {
        private WizardController wizard;
        private BatEnemyController bat;
        private GameSession session;
        private StagePortal portal;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            // Let the wizard finish appearing (damage is ignored until then).
            yield return new WaitForSeconds(1.2f);
            wizard = Object.FindFirstObjectByType<WizardController>();
            bat = Object.FindFirstObjectByType<BatEnemyController>();
            session = Object.FindFirstObjectByType<GameSession>();
            portal = Object.FindFirstObjectByType<StagePortal>();
        }

        private static IEnumerator KillBat(BatEnemyController target)
        {
            target.GetComponent<Health>().TakeDamage(new DamageInfo(100, target.transform.position, null, Team.Player));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Portal_StaysClosed_WhileAnEnemyIsAlive()
        {
            Assert.IsFalse(portal.IsOpen, "bat is still alive");
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(portal.IsOpen);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Portal_Opens_OnceEveryEnemyIsDead()
        {
            Assert.IsFalse(portal.IsOpen);
            yield return KillBat(bat);

            Assert.AreEqual(0, EnemyRegistry.AliveCount);
            Assert.IsTrue(portal.IsOpen);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EnteringTheOpenPortal_ClearsTheStage_AfterTheWizardVanishes()
        {
            yield return KillBat(bat);
            Assert.IsTrue(portal.IsOpen);
            // Wait for the reveal to finish before the wizard is allowed to step in.
            yield return new WaitForSeconds(0.9f);
            Assert.IsTrue(portal.IsReady);

            wizard.transform.position = portal.transform.position;
            yield return null;
            Assert.IsFalse(wizard.IsInPlay, "started vanishing into the portal");
            Assert.AreEqual(GameState.Playing, session.State, "not cleared until the vanish finishes");

            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(GameState.StageCleared, session.State);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WizardHealthReachingZero_SetsDefeated()
        {
            Health wizardHealth = wizard.GetComponent<Health>();
            wizardHealth.TakeDamage(new DamageInfo(100, wizard.transform.position, null, Team.Enemy));

            Assert.IsTrue(wizardHealth.IsDead);
            Assert.AreEqual(GameState.Defeated, session.State);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Retry_RestartsTheStage_BackToPlaying_WithAFreshBat()
        {
            // [10]: Retry restarts the current stage in place (StageRunner), not a scene
            // reload -- the bat above died with it, so a fresh one confirms the restart really
            // re-spawned the stage's enemies rather than just flipping the state back.
            yield return KillBat(bat);
            Assert.AreEqual(0, EnemyRegistry.AliveCount);

            wizard.GetComponent<Health>().TakeDamage(new DamageInfo(100, wizard.transform.position, null, Team.Enemy));
            Assert.AreEqual(GameState.Defeated, session.State);

            session.Retry();
            yield return null;

            Assert.AreEqual(GameState.Playing, session.State);
            Assert.AreEqual(1, EnemyRegistry.AliveCount, "stage 1's one bat is back");
            Assert.IsFalse(portal.IsOpen);
        }
    }
}
