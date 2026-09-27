using System;
using UnityEngine;

namespace WizardArena.Feedback
{
    // All tunable numbers for hit feel, plus an on/off switch per effect. One asset:
    // Assets/Config/Feedback/FeedbackConfig.asset.
    [CreateAssetMenu(menuName = "Wizard Arena/Feedback/Feedback Config", fileName = "FeedbackConfig")]
    public sealed class FeedbackConfig : ScriptableObject
    {
        [Header("Hit-stop (enemy hit, player hurt)")]
        [SerializeField] private bool hitStopEnabled = true;
        [Tooltip("How long the dip lasts, in real (unscaled) seconds.")]
        [SerializeField, Min(0.01f)] private float hitStopSeconds = 0.05f;
        [Tooltip("Time.timeScale during the dip. Not 0, so it reads as a stutter, not a freeze.")]
        [SerializeField, Range(0.01f, 1f)] private float hitStopTimeScale = 0.05f;

        [Header("Camera shake (player hurt, explosions)")]
        [SerializeField] private bool shakeEnabled = true;
        [SerializeField, Range(0f, 1f)] private float playerHurtShakeTrauma = 0.4f;
        [SerializeField, Range(0f, 1f)] private float explosionShakeTrauma = 0.7f;
        [Tooltip("How fast shake trauma decays back to zero, per real second.")]
        [SerializeField, Min(0.01f)] private float shakeDecayPerSecond = 2.2f;
        [SerializeField, Min(0f)] private float shakeMaxOffset = 0.35f;

        [Header("Hit flash (enemies)")]
        [SerializeField] private bool hitFlashEnabled = true;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField, Min(0.01f)] private float hitFlashSeconds = 0.08f;

        [Header("Particles -- pixel-square bursts, blue/teal palette")]
        [SerializeField] private bool particlesEnabled = true;
        [SerializeField] private Color particleStartColor = new Color(0.55f, 0.9f, 1f, 1f);
        [SerializeField] private Color particleEndColor = new Color(0.1f, 0.55f, 0.85f, 0f);
        [SerializeField] private ParticleBurst enemyHitBurst = new ParticleBurst(10, 2.5f, 0.3f, 0.07f);
        [SerializeField] private ParticleBurst enemyDeathBurst = new ParticleBurst(18, 3.5f, 0.45f, 0.09f);
        [SerializeField] private ParticleBurst explosionBurst = new ParticleBurst(30, 5f, 0.55f, 0.11f);

        public bool HitStopEnabled => hitStopEnabled;
        public float HitStopSeconds => hitStopSeconds;
        public float HitStopTimeScale => hitStopTimeScale;

        public bool ShakeEnabled => shakeEnabled;
        public float PlayerHurtShakeTrauma => playerHurtShakeTrauma;
        public float ExplosionShakeTrauma => explosionShakeTrauma;
        public float ShakeDecayPerSecond => shakeDecayPerSecond;
        public float ShakeMaxOffset => shakeMaxOffset;

        public bool HitFlashEnabled => hitFlashEnabled;
        public Color HitFlashColor => hitFlashColor;
        public float HitFlashSeconds => hitFlashSeconds;

        public bool ParticlesEnabled => particlesEnabled;
        public Color ParticleStartColor => particleStartColor;
        public Color ParticleEndColor => particleEndColor;
        public ParticleBurst EnemyHitBurst => enemyHitBurst;
        public ParticleBurst EnemyDeathBurst => enemyDeathBurst;
        public ParticleBurst ExplosionBurst => explosionBurst;

        // For code-built configs (tests): every effect on or off together, defaults kept small.
        public static FeedbackConfig Create(bool enabled)
        {
            FeedbackConfig config = CreateInstance<FeedbackConfig>();
            config.hitStopEnabled = enabled;
            config.shakeEnabled = enabled;
            config.hitFlashEnabled = enabled;
            config.particlesEnabled = enabled;
            return config;
        }
    }

    // Count/speed/lifetime/size for one particle burst tier (hit, death, explosion). Color is
    // shared (FeedbackConfig.ParticleStartColor/EndColor) since only the palette, not the tint,
    // needs to stay consistent across effects.
    [Serializable]
    public sealed class ParticleBurst
    {
        [SerializeField, Min(0)] private int count;
        [SerializeField, Min(0f)] private float speed;
        [SerializeField, Min(0f)] private float lifetime;
        [SerializeField, Min(0f)] private float size;

        public ParticleBurst(int count, float speed, float lifetime, float size)
        {
            this.count = count;
            this.speed = speed;
            this.lifetime = lifetime;
            this.size = size;
        }

        public int Count => count;
        public float Speed => speed;
        public float Lifetime => lifetime;
        public float Size => size;
    }
}
