using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.Player;
using WizardArena.UI;

namespace WizardArena.Tests.PlayMode.UI
{
    // Drives the real scene's HUD, built by UISetup.
    public sealed class HudTests
    {
        private WizardController wizard;
        private Health wizardHealth;
        private Label hpText;
        private VisualElement hpFill;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            // These tests are about the HUD, not the bat.
            Object.FindFirstObjectByType<BatEnemyController>().gameObject.SetActive(false);
            wizard = Object.FindFirstObjectByType<WizardController>();
            wizardHealth = wizard.GetComponent<Health>();

            bool appeared = false;
            wizard.Appeared += () => appeared = true;
            float waited = 0f;
            while (!appeared && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            VisualElement root = Object.FindFirstObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
            hpText = root.Q<Label>("hp-text");
            hpFill = root.Q<VisualElement>("hp-bar-fill");
        }

        [UnityTest]
        public IEnumerator HpBarAndText_Update_WhenTheWizardTakesDamage()
        {
            Assert.AreEqual("10 / 10", hpText.text);
            Assert.AreEqual(100f, hpFill.style.width.value.value, 0.01f);

            wizardHealth.TakeDamage(new DamageInfo(4, wizard.transform.position, null, Team.Enemy));
            yield return null;

            Assert.AreEqual("6 / 10", hpText.text);
            Assert.AreEqual(60f, hpFill.style.width.value.value, 0.01f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
