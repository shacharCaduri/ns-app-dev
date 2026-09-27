using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WizardArena.World;

namespace WizardArena.Tests.EditMode.World
{
    // Pattern for EditMode tests: build the few objects you need in SetUp/helpers,
    // destroy them in TearDown, and leave shared static state as you found it.
    public sealed class ArenaSurfaceTests
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
        public void Support_ReturnsSurface_WhenFeetRestOnTop()
        {
            ArenaSurface floor = CreateSurface(new Vector2(0f, 0f), new Vector2(4f, 1f), jumpThrough: false);

            Assert.AreSame(floor, ArenaSurface.Support(new Vector3(1f, 0.5f, 0f)));
        }

        [Test]
        public void Support_ReturnsNull_WhenFeetAreAboveOrBesideTheSurface()
        {
            CreateSurface(new Vector2(0f, 0f), new Vector2(4f, 1f), jumpThrough: false);

            Assert.IsNull(ArenaSurface.Support(new Vector3(1f, 0.7f, 0f)), "above");
            Assert.IsNull(ArenaSurface.Support(new Vector3(2.5f, 0.5f, 0f)), "beside");
        }

        [Test]
        public void Support_IgnoresSurfacesThatAreNotWalkable()
        {
            ArenaSurface floor = CreateSurface(new Vector2(0f, 0f), new Vector2(4f, 1f), jumpThrough: false);
            floor.walkable = false;

            Assert.IsNull(ArenaSurface.Support(new Vector3(0f, 0.5f, 0f)));
        }

        [Test]
        public void Move_LandsOnTop_WhenFallingThroughTheSurface()
        {
            CreateSurface(new Vector2(0f, 0f), new Vector2(4f, 1f), jumpThrough: false);
            float verticalSpeed = -5f;

            Vector3 result = ArenaSurface.Move(new Vector3(0f, 1f, 0f), new Vector3(0f, 0.2f, 0f), ref verticalSpeed, false);

            Assert.AreEqual(0.5f, result.y, 1e-4f);
            Assert.AreEqual(0f, verticalSpeed);
        }

        [Test]
        public void Move_PassesUpThroughJumpThroughPlatform()
        {
            CreateSurface(new Vector2(0f, 0f), new Vector2(4f, 0.4f), jumpThrough: true);
            float verticalSpeed = 5f;
            Vector3 target = new Vector3(0f, 0.1f, 0f);

            Vector3 result = ArenaSurface.Move(new Vector3(0f, -1f, 0f), target, ref verticalSpeed, false);

            Assert.AreEqual(target, result);
            Assert.AreEqual(5f, verticalSpeed);
        }

        [Test]
        public void Move_StopsAtTheSideOfASolidWall()
        {
            // Wall spans x 1.5..2.5; the wizard body is 0.22 wide on each side of its feet.
            CreateSurface(new Vector2(2f, 0f), new Vector2(1f, 4f), jumpThrough: false);
            float verticalSpeed = 0f;

            Vector3 result = ArenaSurface.Move(new Vector3(0f, -1f, 0f), new Vector3(1.5f, -1f, 0f), ref verticalSpeed, false);

            Assert.AreEqual(1.28f, result.x, 1e-4f);
        }

        private ArenaSurface CreateSurface(Vector2 center, Vector2 size, bool jumpThrough)
        {
            GameObject item = new GameObject("Test Surface");
            created.Add(item);
            item.transform.position = center;
            item.AddComponent<BoxCollider2D>().size = size;
            ArenaSurface surface = item.AddComponent<ArenaSurface>();
            surface.jumpThrough = jumpThrough;
            // OnEnable does not run in Edit Mode, so register the surface by hand.
            if (!ArenaSurface.Active.Contains(surface)) ArenaSurface.Active.Add(surface);
            Physics2D.SyncTransforms();
            return surface;
        }
    }
}
