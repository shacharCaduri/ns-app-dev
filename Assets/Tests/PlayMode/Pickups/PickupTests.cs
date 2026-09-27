using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Pickups;
using Object = UnityEngine.Object;

namespace WizardArena.Tests.PlayMode.Pickups
{
    // A hand-built pickup far below the arena, same rig pattern as SpikeHazardTests.
    public sealed class PickupTests
    {
        private static readonly Vector3 RigOrigin = new Vector3(0f, -50f, 0f);
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator HealEffect_Heals_ButNeverAboveMax()
        {
            // 10 max, 1 damage taken -> 9 current; a 5-point heal would overshoot to 14.
            Health target = CreateTarget();
            target.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Enemy));
            CreatePickup(RigOrigin, healAmount: 5, lifetime: 10f);

            yield return null;

            Assert.AreEqual(10, target.Current, "capped at Max, not 9 + 5");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator HealEffect_LeavesThePickupUncollected_AtFullHealth()
        {
            Health target = CreateTarget(); // starts at full HP
            GameObject pickup = CreatePickup(RigOrigin, healAmount: 3, lifetime: 10f);

            yield return null;
            yield return null;

            Assert.AreEqual(target.Max, target.Current);
            Assert.IsFalse(pickup == null, "nothing was healed, so it was not consumed");
        }

        [UnityTest]
        public IEnumerator HealEffect_Heals_WhenTargetIsHurt()
        {
            Health target = CreateTarget();
            target.TakeDamage(new DamageInfo(4, RigOrigin, null, Team.Enemy)); // 6/10
            GameObject pickup = CreatePickup(RigOrigin, healAmount: 3, lifetime: 10f);

            yield return null;

            Assert.AreEqual(9, target.Current);
            Assert.IsTrue(pickup == null, "consumed once it actually healed");
        }

        [UnityTest]
        public IEnumerator Pickup_Despawns_AfterItsLifetime()
        {
            // Off on its own, away from any target, so only the lifetime can remove it.
            GameObject pickup = CreatePickup(RigOrigin + new Vector3(20f, 0f, 0f), healAmount: 3, lifetime: 0.15f);

            yield return new WaitForSeconds(0.05f);
            Assert.IsFalse(pickup == null, "still within its lifetime");

            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(pickup == null, "despawned once its lifetime elapsed");
        }

        private GameObject CreatePickup(Vector3 position, int healAmount, float lifetime)
        {
            GameObject go = Track(new GameObject("Test Pickup"));
            go.transform.position = position;
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.3f;
            Pickup pickup = go.AddComponent<Pickup>();
            pickup.Configure(Track(PickupConfig.Create(lifetime, blinkSeconds: 0f)));
            go.AddComponent<HealEffect>().Configure(healAmount);
            return go;
        }

        private Health CreateTarget()
        {
            GameObject item = Track(new GameObject("Test Target"));
            item.transform.position = RigOrigin;
            item.AddComponent<CircleCollider2D>().radius = 0.4f;
            item.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Health health = item.AddComponent<Health>();
            health.Configure(Track(HealthConfig.Create(10, 0f)), Team.Player);
            return health;
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
