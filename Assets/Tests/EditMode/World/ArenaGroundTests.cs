using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WizardArena.World;

namespace WizardArena.Tests.EditMode.World
{
    public sealed class ArenaGroundTests
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private List<ArenaSurface> previouslyActive;

        [SetUp]
        public void SetUp()
        {
            previouslyActive = new List<ArenaSurface>(ArenaSurface.Active);
            ArenaSurface.Active.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in created) Object.DestroyImmediate(item);
            created.Clear();
            ArenaSurface.Active.Clear();
            ArenaSurface.Active.AddRange(previouslyActive);
        }

        [Test]
        public void GroundBelow_ReturnsHighestSurfaceTop_UnderThePoint()
        {
            CreateSurface(new Vector2(0f, -3f), new Vector2(10f, 1f));  // top -2.5
            CreateSurface(new Vector2(1f, 0f), new Vector2(2f, 0.4f));  // top 0.2

            Assert.IsTrue(ArenaSurface.TryGetGroundBelow(new Vector2(1f, 2f), out float onPlatform));
            Assert.AreEqual(0.2f, onPlatform, 1e-4f);
            Assert.IsTrue(ArenaSurface.TryGetGroundBelow(new Vector2(-3f, 2f), out float onFloor));
            Assert.AreEqual(-2.5f, onFloor, 1e-4f);
        }

        [Test]
        public void GroundBelow_IgnoresSurfacesAboveThePoint()
        {
            CreateSurface(new Vector2(0f, -3f), new Vector2(10f, 1f));
            CreateSurface(new Vector2(0f, 2f), new Vector2(10f, 1f));

            Assert.IsTrue(ArenaSurface.TryGetGroundBelow(new Vector2(0f, 0f), out float ground));
            Assert.AreEqual(-2.5f, ground, 1e-4f);
        }

        [Test]
        public void GroundBelow_ReturnsFalse_WhenNothingIsUnderThePoint()
        {
            CreateSurface(new Vector2(0f, -3f), new Vector2(2f, 1f));

            Assert.IsFalse(ArenaSurface.TryGetGroundBelow(new Vector2(5f, 0f), out _));
        }

        private void CreateSurface(Vector2 center, Vector2 size)
        {
            GameObject item = new GameObject("Test Surface");
            created.Add(item);
            item.transform.position = center;
            item.AddComponent<BoxCollider2D>().size = size;
            ArenaSurface surface = item.AddComponent<ArenaSurface>();
            // OnEnable does not run in Edit Mode, so register the surface by hand.
            if (!ArenaSurface.Active.Contains(surface)) ArenaSurface.Active.Add(surface);
            Physics2D.SyncTransforms();
        }
    }
}
