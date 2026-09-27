using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardArena.Combat;

namespace WizardArena.Tests.PlayMode.Combat
{
    // Builds a tiny rig far below the arena so it never meets scene objects from other tests.
    public sealed class ProjectileTests
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
        public IEnumerator Projectile_DamagesEnemy_WithTriggerAndKinematicBody()
        {
            Health target = CreateTarget(Team.Enemy, trigger: true, kinematicBody: true);
            Projectile bolt = CreateLauncher(Team.Player).Fire(RigOrigin, Vector2.right);

            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(2, target.Current);
            Assert.IsTrue(bolt == null, "the bolt is destroyed on hit");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Projectile_DamagesTarget_WithPlainSolidCollider()
        {
            Health target = CreateTarget(Team.Player, trigger: false, kinematicBody: false);
            Projectile bolt = CreateLauncher(Team.Enemy).Fire(RigOrigin, Vector2.right);

            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(2, target.Current);
            Assert.IsTrue(bolt == null, "the bolt is destroyed on hit");
        }

        [UnityTest]
        public IEnumerator Projectile_PassesThrough_SameTeamTarget()
        {
            Health target = CreateTarget(Team.Player, trigger: true, kinematicBody: true);
            Projectile bolt = CreateLauncher(Team.Player).Fire(RigOrigin, Vector2.right);
            Track(bolt.gameObject);

            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(3, target.Current);
            Assert.IsTrue(bolt != null, "the bolt flies on");
            Assert.Greater(bolt.transform.position.x, target.transform.position.x);
        }

        [UnityTest]
        public IEnumerator Projectile_IsDestroyed_AfterItsLifetime()
        {
            Projectile bolt = CreateLauncher(Team.Player).Fire(RigOrigin, Vector2.left);

            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(bolt != null, "still flying");
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(bolt == null, "gone after 1 s lifetime");
        }

        private ProjectileLauncher CreateLauncher(Team team)
        {
            GameObject shooter = Track(new GameObject("Test Shooter"));
            shooter.transform.position = RigOrigin;
            // The shooter's own collider must never be hit.
            shooter.AddComponent<CircleCollider2D>().radius = 0.5f;
            ProjectileLauncher launcher = shooter.AddComponent<ProjectileLauncher>();
            launcher.Configure(Track(ProjectileConfig.Create(1, 11f, 1f, 0.12f)), team);
            return launcher;
        }

        private Health CreateTarget(Team team, bool trigger, bool kinematicBody)
        {
            GameObject item = Track(new GameObject("Test Target"));
            item.transform.position = RigOrigin + new Vector3(3f, 0f, 0f);
            CircleCollider2D circle = item.AddComponent<CircleCollider2D>();
            circle.radius = 0.45f;
            circle.isTrigger = trigger;
            if (kinematicBody) item.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Health health = item.AddComponent<Health>();
            health.Configure(Track(HealthConfig.Create(3, 0f)), team);
            return health;
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
