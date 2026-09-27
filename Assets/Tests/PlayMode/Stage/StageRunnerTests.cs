using System.Collections;
using System.Collections.Generic;
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
    // Drives the generated scene's full three-stage sequence: StageRunner, built by StageSetup.
    public sealed class StageRunnerTests
    {
        private WizardController wizard;
        private GameSession session;
        private StagePortal portal;
        private StageRunner runner;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            // Let the wizard finish appearing (damage is ignored until then).
            yield return new WaitForSeconds(1.2f);
            wizard = Object.FindFirstObjectByType<WizardController>();
            session = Object.FindFirstObjectByType<GameSession>();
            portal = Object.FindFirstObjectByType<StagePortal>();
            runner = Object.FindFirstObjectByType<StageRunner>();
        }

        private static void KillAllEnemies()
        {
            foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!enemy.IsDead) enemy.Health.TakeDamage(new DamageInfo(1000, enemy.transform.position, null, Team.Player));
        }

        // Kills every enemy the current stage still has (so no live bat can touch the wizard
        // during the wait below), waits for the portal to fully open, then walks the wizard in
        // and waits for its vanish to finish -- the stage's outcome is set the moment that ends.
        private IEnumerator ClearStageAndEnterPortal()
        {
            KillAllEnemies();
            yield return null;
            Assert.AreEqual(0, EnemyRegistry.AliveCount);
            Assert.IsTrue(portal.IsOpen);
            // Covers both the portal's reveal (0.8 s) and, after a stage change, the wizard's
            // own appear transition (0.9 s) -- both start at roughly the same time.
            yield return new WaitForSeconds(1.0f);
            Assert.IsTrue(portal.IsReady);
            Assert.IsTrue(wizard.IsInPlay, "finished appearing by the time the portal is ready");

            wizard.transform.position = portal.transform.position;
            yield return null;
            Assert.IsFalse(wizard.IsInPlay, "started vanishing into the portal");
            yield return new WaitForSeconds(1.6f);
        }

        [UnityTest]
        public IEnumerator ClearingStage1_ThenContinue_MovesToStage2WithThreeEnemiesAlive()
        {
            Assert.AreEqual("Ruins Antechamber", runner.CurrentStage.DisplayName);
            yield return ClearStageAndEnterPortal();
            Assert.AreEqual(GameState.StageCleared, session.State);

            session.Continue();
            yield return null;

            Assert.AreEqual(GameState.Playing, session.State, "Continue hides the Stage Cleared screen");
            Assert.AreEqual("Mossy Bridge", runner.CurrentStage.DisplayName);
            Assert.AreEqual(3, EnemyRegistry.AliveCount);
            Assert.IsFalse(portal.IsOpen, "closed and moved for the new stage");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ClearingEveryStage_EndsInVictory_NotStageCleared()
        {
            yield return ClearStageAndEnterPortal(); // Ruins Antechamber (1 bat)
            Assert.AreEqual(GameState.StageCleared, session.State);
            session.Continue();
            yield return null;
            Assert.AreEqual(3, EnemyRegistry.AliveCount);
            Assert.IsFalse(runner.IsLastStage);

            yield return ClearStageAndEnterPortal(); // Mossy Bridge (3 bats)
            Assert.AreEqual(GameState.StageCleared, session.State);
            session.Continue();
            yield return null;
            Assert.AreEqual(5, EnemyRegistry.AliveCount);
            Assert.IsTrue(runner.IsLastStage);

            yield return ClearStageAndEnterPortal(); // Portal Sanctum (5 bats, the last stage)
            Assert.AreEqual(GameState.Victory, session.State, "the last stage ends the run in Victory");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Retry_RestartsTheCurrentStage_WithItsFullEnemyCount()
        {
            yield return ClearStageAndEnterPortal(); // move on to stage 2
            session.Continue();
            yield return null;
            Assert.AreEqual(3, EnemyRegistry.AliveCount, "stage 2 starts with 3 bats");

            // Kill one, so Retry giving the full count back below means a fresh spawn, not a
            // revive of whatever is left.
            List<Enemy> enemies = new List<Enemy>(Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            enemies[0].Health.TakeDamage(new DamageInfo(1000, enemies[0].transform.position, null, Team.Player));
            yield return null;
            Assert.AreEqual(2, EnemyRegistry.AliveCount);

            // The wizard is still mid-appear (Health is off) and the remaining bats could reach
            // it while we wait that out -- clear them first so the death below is deterministic.
            KillAllEnemies();
            yield return null;
            Assert.AreEqual(0, EnemyRegistry.AliveCount);
            yield return new WaitForSeconds(1.0f);
            Assert.IsTrue(wizard.IsInPlay, "finished appearing");

            wizard.GetComponent<Health>().TakeDamage(new DamageInfo(100, wizard.transform.position, null, Team.Enemy));
            Assert.AreEqual(GameState.Defeated, session.State);

            session.Retry();
            yield return null;

            Assert.AreEqual(GameState.Playing, session.State);
            Assert.AreEqual("Mossy Bridge", runner.CurrentStage.DisplayName, "restarted the SAME stage, not the first one");
            Assert.AreEqual(3, EnemyRegistry.AliveCount, "back to its full enemy count");
            Assert.IsFalse(portal.IsOpen);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RestartRun_FromVictory_GoesBackToStageOneWithItsEnemies()
        {
            yield return ClearStageAndEnterPortal(); // Ruins Antechamber (1 bat)
            session.Continue();
            yield return null;

            yield return ClearStageAndEnterPortal(); // Mossy Bridge (3 bats)
            session.Continue();
            yield return null;

            yield return ClearStageAndEnterPortal(); // Portal Sanctum (5 bats, the last stage)
            Assert.AreEqual(GameState.Victory, session.State);

            session.RestartRun();
            yield return null;

            Assert.AreEqual(GameState.Playing, session.State);
            Assert.AreEqual("Ruins Antechamber", runner.CurrentStage.DisplayName, "restarted the WHOLE run, not just the last stage");
            Assert.AreEqual(1, EnemyRegistry.AliveCount, "stage 1's one bat is back");
            Assert.IsFalse(portal.IsOpen);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
