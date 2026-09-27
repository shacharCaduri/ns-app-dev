using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizardArena.Pickups;
using WizardArena.Stage;
using Object = UnityEngine.Object;

namespace WizardArena.Tests.PlayMode.Pickups
{
    // Confirms StageRunner also cleans up a pickup that was never placed through
    // StageDefinition.PropSpawns -- e.g. one an EnemyDropper spawns at runtime. It learns
    // about it through PickupRegistry instead of a direct reference (Enemies never depends on
    // Stage).
    public sealed class PickupCleanupTests
    {
        private readonly List<Object> created = new List<Object>();
        private GameSession session;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("WizardMovement", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.2f);
            session = Object.FindFirstObjectByType<GameSession>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator DroppedPickup_IsRemoved_WhenTheStageRestarts()
        {
            GameObject pickup = new GameObject("Runtime Pickup"); // not Track()ed: cleanup is what this test checks
            pickup.AddComponent<CircleCollider2D>().isTrigger = true;
            pickup.AddComponent<Pickup>().Configure(Track(PickupConfig.Create(30f, 2f)));
            yield return null;
            Assert.IsFalse(pickup == null, "spawned");

            session.Retry();
            yield return null;

            Assert.IsTrue(pickup == null, "removed along with the rest of the restarted stage's content");
            LogAssert.NoUnexpectedReceived();
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
