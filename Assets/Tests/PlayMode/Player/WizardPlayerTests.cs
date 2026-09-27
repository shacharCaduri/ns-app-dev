using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.Player;

namespace WizardArena.Tests.PlayMode.Player
{
    // Drives the real wizard in the generated scene with scripted input.
    public sealed class WizardPlayerTests
    {
        private sealed class FakeInput : IPlayerInput
        {
            public float Horizontal { get; set; }
            public bool Jump;
            public bool Cast;

            // Presses last one frame, like GetKeyDown.
            public bool JumpPressed { get { bool pressed = Jump; Jump = false; return pressed; } }
            public bool CastPressed { get { bool pressed = Cast; Cast = false; return pressed; } }
        }

        private WizardController wizard;
        private FakeInput input;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            // Park the bat: these tests are about the wizard alone. Deactivated (not destroyed)
            // so the stage keeps the exit portal, which sits next to the wizard, closed.
            Object.FindFirstObjectByType<BatEnemyController>().gameObject.SetActive(false);
            wizard = Object.FindFirstObjectByType<WizardController>();
            input = new FakeInput();
            wizard.UseInput(input);
            bool appeared = false;
            wizard.Appeared += () => appeared = true;
            float waited = 0f;
            while (!appeared && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(wizard.IsInPlay, "wizard finished appearing");
            // Settle onto the floor.
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTest]
        public IEnumerator Walking_MovesTheWizard_AndTurnsIt()
        {
            float startX = wizard.transform.position.x;
            input.Horizontal = 1f;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(wizard.transform.position.x, startX + 1f, "walks right about 4 units/s");
            Assert.AreEqual(1f, wizard.Facing);

            input.Horizontal = -1f;
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(-1f, wizard.Facing);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Jump_LeavesTheGround_AndLandsAgain()
        {
            Assert.IsTrue(wizard.IsGrounded, "starts grounded");
            float startY = wizard.transform.position.y;

            input.Jump = true;
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(wizard.IsGrounded, "in the air");
            Assert.Greater(wizard.transform.position.y, startY + 0.5f);

            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(wizard.IsGrounded, "landed");
            Assert.AreEqual(startY, wizard.transform.position.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator Cast_SpawnsAProjectile_InTheFacingDirection()
        {
            Assert.IsNull(Object.FindFirstObjectByType<Projectile>());

            input.Cast = true;
            yield return new WaitForSeconds(0.4f);

            Projectile bolt = Object.FindFirstObjectByType<Projectile>();
            Assert.IsNotNull(bolt, "released at 0.28 s");
            Assert.Greater(bolt.transform.position.x, wizard.transform.position.x);
        }

        [UnityTest]
        public IEnumerator Damage_KnocksTheWizardAwayFromTheSource()
        {
            Health health = wizard.GetComponent<Health>();
            Vector3 start = wizard.transform.position;

            health.TakeDamage(new DamageInfo(1, start + Vector3.left, null, Team.Enemy));
            Assert.IsTrue(wizard.GetComponent<PlayerHitFeedback>().IsPlaying, "hit flash");
            yield return new WaitForSeconds(0.15f);

            Assert.Greater(wizard.transform.position.x, start.x + 0.5f, "pushed right");
            Assert.Greater(wizard.transform.position.y, start.y, "bumped up");
            Assert.AreEqual(9, health.Current);
        }

        [UnityTest]
        public IEnumerator EnterPortal_VanishesIntoThePoint_AndRaisesVanished()
        {
            bool vanished = false;
            wizard.Vanished += () => vanished = true;
            Vector3 portal = wizard.transform.position + new Vector3(2f, 0f, 0f);

            wizard.EnterPortal(portal);
            Assert.IsFalse(wizard.IsInPlay);
            Assert.IsFalse(wizard.GetComponent<Health>().enabled, "no damage while leaving");
            yield return new WaitForSeconds(1.6f);

            Assert.IsTrue(vanished);
            Assert.AreEqual(portal.x, wizard.transform.position.x, 0.001f);
        }

        [UnityTest]
        public IEnumerator Death_VanishesInPlace()
        {
            bool vanished = false;
            wizard.Vanished += () => vanished = true;

            wizard.GetComponent<Health>().TakeDamage(new DamageInfo(100, Vector2.zero, null, Team.Neutral));
            Assert.IsTrue(wizard.IsDefeated);
            Assert.IsFalse(wizard.IsInPlay);
            yield return new WaitForSeconds(1.6f);

            Assert.IsTrue(vanished);
        }
    }
}
