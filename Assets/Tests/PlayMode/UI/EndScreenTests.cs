using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using WizardArena.Combat;
using WizardArena.Player;
using WizardArena.UI;

namespace WizardArena.Tests.PlayMode.UI
{
    // Drives the real scene's end screen, built by UISetup.
    public sealed class EndScreenTests
    {
        private WizardController wizard;
        private VisualElement overlay;
        private Label heading;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            wizard = Object.FindFirstObjectByType<WizardController>();
            // Health is disabled until the wizard finishes appearing; wait it out before hitting it.
            yield return new WaitForSeconds(1.2f);
            VisualElement root = Object.FindFirstObjectByType<EndScreenController>().GetComponent<UIDocument>().rootVisualElement;
            overlay = root.Q<VisualElement>("end-overlay");
            heading = root.Q<Label>("end-heading");
        }

        [UnityTest]
        public IEnumerator GameOverScreen_BecomesVisible_WhenTheWizardIsDefeated()
        {
            Assert.AreEqual(DisplayStyle.None, overlay.style.display.value, "hidden while playing");

            wizard.GetComponent<Health>().TakeDamage(new DamageInfo(100, wizard.transform.position, null, Team.Neutral));
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, overlay.style.display.value);
            Assert.AreEqual("GAME OVER", heading.text);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
