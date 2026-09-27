using System.Collections;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Feedback
{
    // Added by FeedbackDirector to every enemy the moment it spawns (EnemyRegistry.Spawned),
    // so the enemy itself never needs to know Feedback exists. Subscribes to its own Health in
    // OnEnable and unsubscribes in OnDisable -- which Unity also calls just before the enemy's
    // GameObject is destroyed, so death and a plain Destroy (e.g. a stage transition) both clean
    // up the same way.
    internal sealed class EnemyFeedbackLink : MonoBehaviour
    {
        private FeedbackDirector director;
        private Health health;
        private SpriteRenderer sprite;
        private Coroutine flashRoutine;

        internal void Bind(FeedbackDirector owner) => director = owner;

        private void Awake()
        {
            health = GetComponent<Health>();
            sprite = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo damage)
        {
            if (director == null) return;
            FeedbackConfig config = director.Config;
            if (config == null) return;

            if (config.HitStopEnabled) director.TriggerHitStop();
            if (config.HitFlashEnabled)
            {
                if (flashRoutine != null) StopCoroutine(flashRoutine);
                flashRoutine = StartCoroutine(Flash(config));
            }
            if (config.ParticlesEnabled)
                ParticleBurstFactory.Spawn(transform.position, config.EnemyHitBurst, config.ParticleStartColor, config.ParticleEndColor);
        }

        private void OnDied()
        {
            if (director == null) return;
            FeedbackConfig config = director.Config;
            if (config != null && config.ParticlesEnabled)
                ParticleBurstFactory.Spawn(transform.position, config.EnemyDeathBurst, config.ParticleStartColor, config.ParticleEndColor);
        }

        private IEnumerator Flash(FeedbackConfig config)
        {
            if (sprite == null) yield break;
            Color original = sprite.color;
            float elapsed = 0f;
            while (elapsed < config.HitFlashSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                sprite.color = Color.Lerp(config.HitFlashColor, original, elapsed / config.HitFlashSeconds);
                yield return null;
            }
            sprite.color = original;
            flashRoutine = null;
        }
    }
}
