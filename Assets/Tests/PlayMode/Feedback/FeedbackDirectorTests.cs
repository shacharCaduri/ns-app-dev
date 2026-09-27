using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardArena.Combat;
using WizardArena.Feedback;
using Object = UnityEngine.Object;

namespace WizardArena.Tests.PlayMode.Feedback
{
    // A hand-built director + player Health + camera far below the arena, same rig pattern as
    // ExplosiveHazardTests. Time.timeScale is restored in TearDown so a failed hit-stop can't
    // bleed into the next test.
    public sealed class FeedbackDirectorTests
    {
        private static readonly Vector3 RigOrigin = new Vector3(0f, -50f, 0f);
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator PlayerHurt_TriggersHitStop_ThenRestoresTimeScale()
        {
            FeedbackConfig config = Track(FeedbackConfig.Create(true));
            (FeedbackDirector director, Health player, Camera camera) = BuildRig(config);
            Assert.NotNull(director);
            Assert.NotNull(camera);
            yield return null;

            player.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Enemy));

            Assert.Less(Time.timeScale, 1f, "the dip should apply in the same frame as the hit");
            yield return new WaitForSecondsRealtime(0.12f);

            Assert.AreEqual(1f, Time.timeScale, 0.001f, "hit-stop restores timeScale once the dip has run its course");
        }

        [UnityTest]
        public IEnumerator PlayerHurt_TriggersShake_AndCameraReturnsToItsBasePosition()
        {
            FeedbackConfig config = Track(FeedbackConfig.Create(true));
            (FeedbackDirector director, Health player, Camera camera) = BuildRig(config);
            Vector3 basePosition = camera.transform.localPosition;
            yield return null;

            player.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Enemy));

            Assert.Greater(director.ShakeTrauma, 0f, "a hit should add shake trauma");
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.AreEqual(0f, director.ShakeTrauma, "trauma decays back to zero");
            Assert.AreEqual(basePosition, camera.transform.localPosition, "the camera lands back exactly on its base position");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DisabledEffects_DoNothing_OnPlayerHurt()
        {
            FeedbackConfig config = Track(FeedbackConfig.Create(false));
            (FeedbackDirector director, Health player, Camera camera) = BuildRig(config);
            Vector3 basePosition = camera.transform.localPosition;
            yield return null;

            player.TakeDamage(new DamageInfo(1, RigOrigin, null, Team.Enemy));
            yield return null;

            Assert.AreEqual(1f, Time.timeScale, "hit-stop is disabled");
            Assert.AreEqual(0f, director.ShakeTrauma, "shake is disabled");
            Assert.AreEqual(basePosition, camera.transform.localPosition, "the camera never moves");
        }

        private (FeedbackDirector, Health, Camera) BuildRig(FeedbackConfig config)
        {
            GameObject playerObject = Track(new GameObject("Test Player"));
            playerObject.transform.position = RigOrigin;
            Health player = playerObject.AddComponent<Health>();
            player.Configure(Track(HealthConfig.Create(10, 0f)), Team.Player);

            GameObject cameraObject = Track(new GameObject("Test Camera"));
            cameraObject.transform.position = RigOrigin + new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();

            GameObject directorObject = Track(new GameObject("Test FeedbackDirector"));
            FeedbackDirector director = directorObject.AddComponent<FeedbackDirector>();
            director.Configure(config, player, camera);

            return (director, player, camera);
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
