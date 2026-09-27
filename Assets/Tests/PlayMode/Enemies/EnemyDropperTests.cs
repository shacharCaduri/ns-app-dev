using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Enemies;
using Object = UnityEngine.Object;

namespace WizardArena.Tests.PlayMode.Enemies
{
    // A hand-built enemy far below the arena, same rig pattern as BatEnemyTests. The "drop" is
    // a marker component so a chance of exactly 0 or 1 can be checked without depending on
    // Pickups.
    public sealed class EnemyDropperTests
    {
        private static readonly Vector2 RigOrigin = new Vector2(0f, -50f);
        private readonly List<Object> created = new List<Object>();

        private sealed class DropMarker : MonoBehaviour
        {
        }

        [TearDown]
        public void TearDown()
        {
            foreach (DropMarker marker in Object.FindObjectsByType<DropMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (marker != null) Object.Destroy(marker.gameObject);
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator EnemyDropper_AlwaysDrops_WhenChanceIsOne()
        {
            GameObject template = Track(new GameObject("Drop Template"));
            template.AddComponent<DropMarker>();
            int before = CountMarkers();

            Enemy enemy = CreateEnemy(RigOrigin, template, chance: 1f);
            Kill(enemy);
            yield return null;

            Assert.AreEqual(before + 1, CountMarkers(), "a 100% entry always drops");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EnemyDropper_NeverDrops_WhenChanceIsZero()
        {
            GameObject template = Track(new GameObject("Drop Template"));
            template.AddComponent<DropMarker>();
            int before = CountMarkers();

            Enemy enemy = CreateEnemy(RigOrigin, template, chance: 0f);
            Kill(enemy);
            yield return null;

            Assert.AreEqual(before, CountMarkers(), "a 0% entry never drops");
        }

        [UnityTest]
        public IEnumerator EnemyDropper_DropsAtTheDeathPosition()
        {
            GameObject template = Track(new GameObject("Drop Template"));
            template.AddComponent<DropMarker>();
            Vector2 deathSpot = RigOrigin + new Vector2(3f, 0f);

            Enemy enemy = CreateEnemy(deathSpot, template, chance: 1f);
            Kill(enemy);
            yield return null;

            DropMarker[] markers = Object.FindObjectsByType<DropMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool foundAtDeathSpot = false;
            foreach (DropMarker marker in markers)
                if (marker.gameObject != template && (Vector2)marker.transform.position == deathSpot)
                    foundAtDeathSpot = true;
            Assert.IsTrue(foundAtDeathSpot);
        }

        private static int CountMarkers()
        {
            return Object.FindObjectsByType<DropMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        private Enemy CreateEnemy(Vector2 position, GameObject dropPrefab, float chance)
        {
            GameObject body = Track(new GameObject("Test Enemy"));
            body.transform.position = position;
            body.AddComponent<SpriteRenderer>();
            Enemy enemy = body.AddComponent<Enemy>();
            body.AddComponent<EnemyDropper>();
            EnemyConfig config = Track(EnemyConfig.Create(Track(HealthConfig.Create(5, 0f)),
                new[] { DropEntry.At(dropPrefab, chance) }));
            enemy.Configure(config);
            return enemy;
        }

        private static void Kill(Enemy enemy)
        {
            enemy.Health.TakeDamage(new DamageInfo(1000, enemy.transform.position, null, Team.Player));
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
