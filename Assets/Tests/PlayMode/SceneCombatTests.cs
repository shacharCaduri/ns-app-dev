using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.Player;

namespace WizardArena.Tests.PlayMode
{
    // Checks the generated scene is wired to the combat layer with the expected numbers.
    public sealed class SceneCombatTests
    {
        private WizardController wizard;
        private BatEnemyController bat;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            // Let the wizard finish appearing (damage is ignored until then).
            yield return new WaitForSeconds(1.2f);
            wizard = Object.FindFirstObjectByType<WizardController>();
            bat = Object.FindFirstObjectByType<BatEnemyController>();
        }

        [UnityTest]
        public IEnumerator WizardBolt_TakesOneHealthFromTheBat()
        {
            Health batHealth = bat.GetComponent<Health>();
            Assert.AreEqual(3, batHealth.Current);

            wizard.GetComponent<ProjectileLauncher>().Fire(bat.transform.position + Vector3.left, Vector2.right);
            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(2, batHealth.Current);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BatContact_TakesTwoHealthFromTheWizard()
        {
            Health wizardHealth = wizard.GetComponent<Health>();
            Assert.AreEqual(10, wizardHealth.Current);

            bat.transform.position = wizard.transform.TransformPoint(wizard.GetComponent<CircleCollider2D>().offset);
            yield return null;
            yield return null;

            Assert.AreEqual(8, wizardHealth.Current);
            Assert.IsTrue(wizardHealth.IsInvulnerable, "0.75 s damage cooldown");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
