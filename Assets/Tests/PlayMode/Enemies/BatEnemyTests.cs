using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.World;
using Object = UnityEngine.Object;

namespace WizardArena.Tests.PlayMode.Enemies
{
    // A hand-built bat far below the arena. Enemies and bounds left by other tests are
    // removed first so they cannot interfere.
    public sealed class BatEnemyTests
    {
        private static readonly Vector2 RigOrigin = new Vector2(0f, -50f);
        private readonly List<Object> created = new List<Object>();
        private Action<Enemy> diedHandler;
        private Sprite testSprite;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Remove enemies and bounds left by earlier tests (e.g. the loaded demo scene).
            foreach (Enemy leftover in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(leftover.gameObject);
            foreach (ArenaBounds leftover in Object.FindObjectsByType<ArenaBounds>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(leftover.gameObject);
            yield return null;

            Texture2D texture = Track(new Texture2D(16, 16));
            testSprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f));
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (diedHandler != null) EnemyRegistry.Died -= diedHandler;
            diedHandler = null;
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator Bat_DiesOnTheThirdHit_AndLeavesTheRegistryOnce()
        {
            int aliveBefore = EnemyRegistry.AliveCount;
            CreateBounds(new Vector2(10f, 10f));
            Enemy bat = CreateBat(RigOrigin);
            Assert.AreEqual(aliveBefore + 1, EnemyRegistry.AliveCount, "registered when it wakes up");
            int diedEvents = 0;
            diedHandler = enemy => { if (enemy == bat) diedEvents++; };
            EnemyRegistry.Died += diedHandler;
            yield return null;

            Assert.IsTrue(Hit(bat));
            Assert.IsTrue(Hit(bat));
            Assert.IsFalse(bat.IsDead);
            Assert.AreEqual(0, diedEvents);
            Assert.AreEqual(aliveBefore + 1, EnemyRegistry.AliveCount);

            Assert.IsTrue(Hit(bat));
            Assert.IsTrue(bat.IsDead);
            Assert.AreEqual(1, diedEvents, "Died is raised at the lethal hit");
            Assert.AreEqual(aliveBefore, EnemyRegistry.AliveCount, "a corpse no longer counts");

            Assert.IsFalse(Hit(bat), "corpses ignore hits");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, diedEvents, "Died is raised only once");
            Assert.IsTrue(bat != null, "the corpse is still there, falling or fading");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Bat_StillCounts_WhileDeactivated_AndLeavesWithoutDiedWhenDestroyed()
        {
            CreateBounds(new Vector2(10f, 10f));
            int aliveBefore = EnemyRegistry.AliveCount;
            Enemy bat = CreateBat(RigOrigin);
            int diedEvents = 0;
            diedHandler = enemy => diedEvents++;
            EnemyRegistry.Died += diedHandler;
            yield return null;

            bat.gameObject.SetActive(false);
            Assert.AreEqual(aliveBefore + 1, EnemyRegistry.AliveCount);

            Object.Destroy(bat.gameObject);
            yield return null;
            Assert.AreEqual(aliveBefore, EnemyRegistry.AliveCount);
            Assert.AreEqual(0, diedEvents);
        }

        [UnityTest]
        public IEnumerator Contact_DamagesPlayerTeam_OncePerCooldown_AndIgnoresAllies()
        {
            CreateBounds(new Vector2(10f, 10f));
            // Targets big enough that the wandering, bouncing bat always touches them.
            Health player = CreateTarget(Team.Player, 10f);
            Health ally = CreateTarget(Team.Enemy, 10f);
            List<float> hitTimes = new List<float>();
            player.Damaged += damage =>
            {
                Assert.AreEqual(2, damage.Amount);
                hitTimes.Add(Time.time);
            };
            Enemy bat = CreateBat(RigOrigin);

            yield return new WaitForSeconds(1.7f);

            Assert.GreaterOrEqual(hitTimes.Count, 3, "hits again after each cooldown");
            for (int i = 1; i < hitTimes.Count; i++)
                Assert.GreaterOrEqual(hitTimes[i] - hitTimes[i - 1], bat.Config.ContactCooldown - 0.001f, "cooldown between hits");
            Assert.AreEqual(ally.Max, ally.Current, "same team is never hurt");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Bat_StaysInsideTheArenaBounds_WhileWanderingAndKnockedBack()
        {
            ArenaBounds bounds = CreateBounds(new Vector2(3f, 1.5f));
            Enemy bat = CreateBat(RigOrigin);

            float end = Time.time + 3f;
            bool knocked = false;
            while (Time.time < end)
            {
                yield return null;
                Assert.IsTrue(bounds.Contains(bat.transform.position), "outside the bounds at " + bat.transform.position);
                if (!knocked && Time.time > end - 2f)
                {
                    knocked = true;
                    // From far left: the knockback pushes right, into the edge.
                    bat.Health.TakeDamage(new DamageInfo(1, bat.transform.position + Vector3.left * 10f, null, Team.Player));
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DeadBat_LandsOnTheGround_HoldsThenFadesAndIsDestroyed()
        {
            Time.timeScale = 4f;
            CreateBounds(new Vector2(10f, 10f));
            float floorTop = RigOrigin.y - 2.5f;
            CreateFloor(new Vector2(RigOrigin.x, floorTop - 0.5f), new Vector2(20f, 1f));
            Enemy bat = CreateBat(RigOrigin + Vector2.up);
            SpriteRenderer renderer = bat.GetComponent<SpriteRenderer>();
            yield return null;

            Hit(bat);
            Hit(bat);
            Hit(bat);

            float timeout = Time.time + 3f;
            while (Mathf.Abs(renderer.bounds.min.y - floorTop) > 0.01f && Time.time < timeout)
                yield return null;
            float landedAt = Time.time;
            Assert.AreEqual(floorTop, renderer.bounds.min.y, 0.01f, "lands on the floor");
            Assert.AreEqual(Quaternion.identity, bat.transform.rotation);

            EnemyConfig config = bat.Config;
            yield return new WaitForSeconds(config.CorpseHoldSeconds - 0.3f);
            Assert.IsTrue(bat != null, "still there during the hold");
            Assert.AreEqual(1f, renderer.color.a, 1e-4f, "not fading during the hold");
            Assert.AreEqual(floorTop, renderer.bounds.min.y, 0.01f, "stays on the floor");

            yield return new WaitForSeconds(config.CorpseFadeSeconds * 0.5f + 0.3f);
            Assert.IsTrue(bat != null, "still fading");
            Assert.Less(renderer.color.a, 1f, "fading");

            while (bat != null && Time.time < landedAt + config.CorpseHoldSeconds + config.CorpseFadeSeconds + 0.5f)
                yield return null;
            Assert.IsTrue(bat == null, "destroyed after the fade");
            LogAssert.NoUnexpectedReceived();
        }

        private static bool Hit(Enemy bat)
        {
            return bat.Health.TakeDamage(new DamageInfo(1, (Vector2)bat.transform.position + Vector2.left, null, Team.Player));
        }

        private Enemy CreateBat(Vector2 position)
        {
            GameObject body = Track(new GameObject("Test Bat"));
            body.transform.position = position;
            body.AddComponent<SpriteRenderer>().sprite = testSprite;
            CircleCollider2D circle = body.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.45f;
            body.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Enemy enemy = body.AddComponent<Enemy>();
            enemy.Configure(Track(EnemyConfig.Create(Track(HealthConfig.Create(3, 0f)))));
            body.AddComponent<ContactDamage>();
            body.AddComponent<BatEnemyController>();
            return enemy;
        }

        private ArenaBounds CreateBounds(Vector2 size)
        {
            ArenaBounds bounds = Track(new GameObject("Test Bounds")).AddComponent<ArenaBounds>();
            bounds.Configure(new Rect(RigOrigin - size * 0.5f, size));
            return bounds;
        }

        private void CreateFloor(Vector2 center, Vector2 size)
        {
            GameObject floor = Track(new GameObject("Test Floor"));
            floor.transform.position = center;
            floor.AddComponent<BoxCollider2D>().size = size;
            floor.AddComponent<ArenaSurface>();
            Physics2D.SyncTransforms();
        }

        private Health CreateTarget(Team team, float radius)
        {
            GameObject item = Track(new GameObject("Test Target " + team));
            item.transform.position = RigOrigin;
            item.AddComponent<CircleCollider2D>().radius = radius;
            Health health = item.AddComponent<Health>();
            health.Configure(Track(HealthConfig.Create(100, 0f)), team);
            return health;
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
