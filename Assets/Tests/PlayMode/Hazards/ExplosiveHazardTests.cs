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
    // A hand-built explosive hazard far below the arena, same rig pattern as ProjectileTests.
    public sealed class ExplosiveHazardTests
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
        public IEnumerator Explosive_Dies_FromOneHit()
        {
            Health crystal = CreateExplosive(RigOrigin, damage: 3, radius: 2f);
            yield return null;

            Assert.IsTrue(crystal.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Player)));
            Assert.IsTrue(crystal.IsDead);
        }

        [UnityTest]
        public IEnumerator Explosion_DamagesEveryHealth_InRadius_ExactlyOnce_EnemiesIncluded()
        {
            Health crystal = CreateExplosive(RigOrigin, damage: 3, radius: 2f);
            Health player = CreateTarget(Team.Player, RigOrigin + new Vector3(1f, 0f, 0f));
            Health enemy = CreateTarget(Team.Enemy, RigOrigin + new Vector3(-1.5f, 0f, 0f));
            int playerHits = 0, enemyHits = 0;
            player.Damaged += _ => playerHits++;
            enemy.Damaged += _ => enemyHits++;
            yield return null;

            crystal.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Player));

            Assert.AreEqual(7, player.Current);
            Assert.AreEqual(7, enemy.Current);
            Assert.AreEqual(1, playerHits, "hit exactly once, not once per collider/frame");
            Assert.AreEqual(1, enemyHits, "the blast hurts enemies too -- that's the point");
        }

        [UnityTest]
        public IEnumerator Explosion_DoesNotDamage_AnythingOutsideItsRadius()
        {
            Health crystal = CreateExplosive(RigOrigin, damage: 3, radius: 1f);
            Health farAway = CreateTarget(Team.Player, RigOrigin + new Vector3(5f, 0f, 0f));
            yield return null;

            crystal.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Player));

            Assert.AreEqual(10, farAway.Current);
        }

        [UnityTest]
        public IEnumerator Explosive_Disappears_ShortlyAfterExploding()
        {
            Health crystal = CreateExplosive(RigOrigin, damage: 3, radius: 1f);
            GameObject go = crystal.gameObject;
            yield return null;

            crystal.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Player));
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(go == null, "the hazard removes itself after its destroy flash");
            LogAssert.NoUnexpectedReceived();
        }

        private Health CreateExplosive(Vector3 position, int damage, float radius)
        {
            GameObject go = Track(new GameObject("Test Explosive"));
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            Health health = go.AddComponent<Health>();
            health.Configure(Track(HealthConfig.Create(1, 0f)), Team.Neutral);
            ExplosiveHazard hazard = go.AddComponent<ExplosiveHazard>();
            hazard.Configure(Track(ExplosiveHazardConfig.Create(damage, radius, 0.15f)));
            return health;
        }

        private Health CreateTarget(Team team, Vector3 position)
        {
            GameObject item = Track(new GameObject("Test Target"));
            item.transform.position = position;
            item.AddComponent<CircleCollider2D>().isTrigger = true;
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
