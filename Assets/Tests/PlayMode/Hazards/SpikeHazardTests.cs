using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Hazards;
using Object = UnityEngine.Object;

namespace WizardArena.Tests.PlayMode.Hazards
{
    // A hand-built spike far below the arena, same rig pattern as ProjectileTests.
    public sealed class SpikeHazardTests
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
        public IEnumerator Spike_DamagesPlayerTeam_WithKnockbackFromItsOwnPosition()
        {
            CreateSpike(RigOrigin, damage: 2, cooldown: 0.5f);
            Health target = CreateTarget(Team.Player, RigOrigin);
            DamageInfo? received = null;
            target.Damaged += info => received = info;

            yield return null;

            Assert.AreEqual(8, target.Current);
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual((Vector2)RigOrigin, received.Value.SourcePosition);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Spike_DamagesEnemyTeam_Too()
        {
            CreateSpike(RigOrigin, damage: 1, cooldown: 0.5f);
            Health target = CreateTarget(Team.Enemy, RigOrigin);

            yield return null;

            Assert.AreEqual(9, target.Current);
        }

        [UnityTest]
        public IEnumerator Spike_DoesNotHitAgain_UntilItsCooldownElapses()
        {
            CreateSpike(RigOrigin, damage: 1, cooldown: 0.3f);
            Health target = CreateTarget(Team.Player, RigOrigin);

            yield return null;
            Assert.AreEqual(9, target.Current, "first touch lands");

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(9, target.Current, "still within this target's cooldown");

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(8, target.Current, "hits again once its cooldown is over");
        }

        [UnityTest]
        public IEnumerator Spike_IgnoresATarget_OutOfRange()
        {
            CreateSpike(RigOrigin, damage: 1, cooldown: 0.1f);
            Health target = CreateTarget(Team.Player, RigOrigin + new Vector3(3f, 0f, 0f));

            yield return null;

            Assert.AreEqual(10, target.Current);
        }

        private void CreateSpike(Vector3 position, int damage, float cooldown)
        {
            GameObject spike = Track(new GameObject("Test Spike"));
            spike.transform.position = position;
            BoxCollider2D collider = spike.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1f, 1f);
            SpikeHazard hazard = spike.AddComponent<SpikeHazard>();
            hazard.Configure(Track(SpikeHazardConfig.Create(damage, cooldown)));
        }

        private Health CreateTarget(Team team, Vector3 position)
        {
            GameObject item = Track(new GameObject("Test Target"));
            item.transform.position = position;
            CircleCollider2D circle = item.AddComponent<CircleCollider2D>();
            circle.radius = 0.4f;
            circle.isTrigger = true;
            item.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Health health = item.AddComponent<Health>();
            health.Configure(Track(HealthConfig.Create(10, 0f)), team);
            return health;
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
