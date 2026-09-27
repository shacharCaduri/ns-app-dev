using UnityEngine;

namespace WizardArena.Feedback
{
    // Spawns a one-shot, self-destroying ParticleSystem entirely from code: a handful of
    // pixel-ish squares (a tiny point-filtered texture) that fly out and fade. No prefab or
    // art asset needed, so FeedbackConfig alone tunes every burst.
    internal static class ParticleBurstFactory
    {
        private static Material sharedMaterial;
        private static Texture2D sharedTexture;

        internal static void Spawn(Vector3 position, ParticleBurst burst, Color startColor, Color endColor)
        {
            if (burst == null || burst.Count <= 0) return;

            GameObject go = new GameObject("FeedbackBurst");
            go.transform.position = position;

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = burst.Lifetime;
            main.startSpeed = burst.Speed;
            main.startSize = burst.Size;
            main.startColor = startColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst.Count) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(endColor.a, 1f) });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = SharedMaterial();

            system.Play();
            Object.Destroy(go, burst.Lifetime + 0.25f);
        }

        private static Material SharedMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;
            // Always present in a Unity 2D project; respects vertex color, which is how the
            // ParticleSystemRenderer applies startColor/colorOverLifetime.
            sharedMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = SharedTexture() };
            return sharedMaterial;
        }

        private static Texture2D SharedTexture()
        {
            if (sharedTexture != null) return sharedTexture;
            sharedTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            sharedTexture.SetPixels32(pixels);
            sharedTexture.Apply();
            return sharedTexture;
        }
    }
}
