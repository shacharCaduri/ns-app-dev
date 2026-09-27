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
    // Hand-built slimes and skeletons far below the arena, same rig pattern as
    // BatEnemyTests: leftovers from other tests are removed first so they cannot interfere.
    public sealed class GroundEnemyTests
    {
        private static readonly Vector2 RigOrigin = new Vector2(0f, -50f);
        private readonly List<Object> created = new List<Object>();
        private Sprite testSprite;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
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
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator Slime_PatrolsBackAndForth_NeverWalksOffItsPlatform()
        {
            float floorTop = RigOrigin.y - 0.5f;
            CreateFloor(new Vector2(RigOrigin.x, floorTop - 0.5f), new Vector2(3f, 1f));
            Enemy slime = CreateSlime(RigOrigin);
            yield return null;

            float end = Time.time + 4f;
            while (Time.time < end)
            {
                yield return null;
                Vector3 position = slime.transform.position;
                Assert.GreaterOrEqual(position.x, RigOrigin.x - 1.5f - 0.3f, "walked off the left edge");
                Assert.LessOrEqual(position.x, RigOrigin.x + 1.5f + 0.3f, "walked off the right edge");
                Assert.GreaterOrEqual(position.y, floorTop - 0.1f, "fell off the platform");
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Slime_HopsTowardThePlayer_ButStopsAtTheEdgeInsteadOfFallingAcrossAGap()
        {
            float floorTop = RigOrigin.y - 0.5f;
            CreateFloor(new Vector2(RigOrigin.x, floorTop - 0.5f), new Vector2(2f, 1f));
            Health player = CreateTarget(Team.Player, 0.3f, new Vector2(RigOrigin.x + 2.5f, RigOrigin.y));
            Enemy slime = CreateSlime(RigOrigin);
            yield return null;

            float end = Time.time + 3f;
            bool sawHopTowardTheEdge = false;
            while (Time.time < end)
            {
                yield return null;
                Vector3 position = slime.transform.position;
                if (position.x > RigOrigin.x + 0.3f) sawHopTowardTheEdge = true;
                Assert.LessOrEqual(position.x, RigOrigin.x + 1f + 0.25f, "hopped past the platform edge while chasing");
                Assert.GreaterOrEqual(position.y, floorTop - 0.1f, "fell into the gap while chasing");
            }
            Assert.IsTrue(sawHopTowardTheEdge, "should hop toward the player it noticed");
            Assert.IsTrue(player.IsDead == false);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Skeleton_ApproachesAndSwings_ButOnlyAfterTheWindUpCompletes()
        {
            float floorTop = RigOrigin.y - 0.5f;
            CreateFloor(new Vector2(RigOrigin.x, floorTop - 0.5f), new Vector2(20f, 1f));
            Health player = CreateTarget(Team.Player, 0.3f, new Vector2(RigOrigin.x + 1.6f, RigOrigin.y));
            int hits = 0;
            player.Damaged += _ => hits++;
            Enemy skeleton = CreateSkeleton(RigOrigin);
            yield return null;

            // The wind-up alone takes 0.5s (EnemyConfig default), so nothing can land before that,
            // no matter how quickly the skeleton closes the distance.
            float noHitUntil = Time.time + 0.45f;
            while (Time.time < noHitUntil)
            {
                yield return null;
                Assert.AreEqual(0, hits, "swung before the wind-up could have finished");
            }

            float deadline = Time.time + 3f;
            while (hits == 0 && Time.time < deadline) yield return null;
            Assert.GreaterOrEqual(hits, 1, "should reach melee range, wind up and land the swing");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Skeleton_MissesTheSwing_WhenTheTargetLeavesReachDuringTheWindUp()
        {
            float floorTop = RigOrigin.y - 0.5f;
            CreateFloor(new Vector2(RigOrigin.x, floorTop - 0.5f), new Vector2(20f, 1f));
            // Already inside melee range (but outside contact-touch distance, so only the
            // swing -- not passive ContactDamage -- could be the source of a hit here):
            // the skeleton starts winding up on the first tick.
            Health player = CreateTarget(Team.Player, 0.3f, new Vector2(RigOrigin.x + 0.95f, RigOrigin.y));
            int hits = 0;
            player.Damaged += _ => hits++;
            CreateSkeleton(RigOrigin);
            yield return null;

            yield return new WaitForSeconds(0.1f);
            player.transform.position = new Vector3(RigOrigin.x + 30f, RigOrigin.y, 0f);
            Physics2D.SyncTransforms();

            yield return new WaitForSeconds(0.7f); // past when the wind-up would have landed
            Assert.AreEqual(0, hits, "the target left reach before the swing, so it should not be hit");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Slime_And_Skeleton_DieAndLeaveTheRegistry()
        {
            CreateFloor(new Vector2(RigOrigin.x, RigOrigin.y - 1f), new Vector2(20f, 1f));
            int aliveBefore = EnemyRegistry.AliveCount;
            Enemy slime = CreateSlime(RigOrigin + Vector2.left * 3f);
            Enemy skeleton = CreateSkeleton(RigOrigin + Vector2.right * 3f);
            Assert.AreEqual(aliveBefore + 2, EnemyRegistry.AliveCount, "both register when they wake up");
            yield return null;

            Assert.IsTrue(Hit(slime));
            Assert.IsTrue(Hit(slime));
            Assert.IsTrue(slime.IsDead);

            Assert.IsTrue(Hit(skeleton));
            Assert.IsTrue(Hit(skeleton));
            Assert.IsTrue(Hit(skeleton));
            Assert.IsTrue(Hit(skeleton));
            Assert.IsTrue(skeleton.IsDead);

            Assert.AreEqual(aliveBefore, EnemyRegistry.AliveCount, "neither corpse counts any more");
            LogAssert.NoUnexpectedReceived();
        }

        private static bool Hit(Enemy enemy)
        {
            return enemy.Health.TakeDamage(new DamageInfo(1, (Vector2)enemy.transform.position + Vector2.left, null, Team.Player));
        }

        private Enemy CreateSlime(Vector2 position)
        {
            GameObject body = Track(new GameObject("Test Slime"));
            body.transform.position = position;
            body.AddComponent<SpriteRenderer>().sprite = testSprite;
            CircleCollider2D circle = body.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.35f;
            body.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Enemy enemy = body.AddComponent<Enemy>();
            enemy.Configure(Track(EnemyConfig.Create(Track(HealthConfig.Create(2, 0f)))));
            body.AddComponent<ContactDamage>();
            body.AddComponent<BlueSlimeController>();
            return enemy;
        }

        private Enemy CreateSkeleton(Vector2 position)
        {
            GameObject body = Track(new GameObject("Test Skeleton"));
            body.transform.position = position;
            body.AddComponent<SpriteRenderer>().sprite = testSprite;
            CircleCollider2D circle = body.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.4f;
            body.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Enemy enemy = body.AddComponent<Enemy>();
            enemy.Configure(Track(EnemyConfig.Create(Track(HealthConfig.Create(4, 0f)))));
            body.AddComponent<ContactDamage>();
            body.AddComponent<SkeletonWarriorController>();
            return enemy;
        }

        private void CreateFloor(Vector2 center, Vector2 size)
        {
            GameObject floor = Track(new GameObject("Test Floor"));
            floor.transform.position = center;
            floor.AddComponent<BoxCollider2D>().size = size;
            floor.AddComponent<ArenaSurface>();
            Physics2D.SyncTransforms();
        }

        private Health CreateTarget(Team team, float radius, Vector2 position)
        {
            GameObject item = Track(new GameObject("Test Target " + team));
            item.transform.position = position;
            item.AddComponent<CircleCollider2D>().radius = radius;
            Health health = item.AddComponent<Health>();
            health.Configure(Track(HealthConfig.Create(100, 0f)), team);
            Physics2D.SyncTransforms();
            return health;
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
