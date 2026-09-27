using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Hazards
{
    // A small neutral Health so any hit (the wizard's bolt, a spike, another explosion...)
    // pops it. On death it deals one area hit to every Health nearby -- enemies included,
    // which is the point: shooting it into a crowd is worth it -- then flashes and shrinks
    // away. No particles here; [14] owns real hit feedback.
    [RequireComponent(typeof(Health))]
    public sealed class ExplosiveHazard : MonoBehaviour
    {
        [SerializeField] private ExplosiveHazardConfig config;

        private Health health;
        private SpriteRenderer sprite;

        // For hazards built from code (the builder, tests).
        public void Configure(ExplosiveHazardConfig hazardConfig) => config = hazardConfig;

        private void Awake()
        {
            health = GetComponent<Health>();
            sprite = GetComponent<SpriteRenderer>();
        }

        private void OnEnable() => health.Died += Explode;
        private void OnDisable() => health.Died -= Explode;

        private void Explode()
        {
            health.Died -= Explode;
            if (config != null) DealAreaDamage();
            StartCoroutine(FlashThenRemove());
        }

        private void DealAreaDamage()
        {
            List<Collider2D> hits = new List<Collider2D>();
            Physics2D.OverlapCircle(transform.position, config.Radius, ContactFilter2D.noFilter, hits);

            HashSet<Health> damaged = new HashSet<Health>();
            foreach (Collider2D hit in hits)
            {
                Health target = hit.GetComponentInParent<Health>();
                if (target == null || !damaged.Add(target)) continue;
                target.TakeDamage(new DamageInfo(config.Damage, transform.position, gameObject, Team.Neutral));
            }
        }

        private IEnumerator FlashThenRemove()
        {
            float flashSeconds = config != null ? config.FlashSeconds : 0.15f;
            Vector3 startScale = transform.localScale;
            if (sprite != null) sprite.color = Color.white;

            float elapsed = 0f;
            while (elapsed < flashSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flashSeconds);
                transform.localScale = Vector3.Lerp(startScale, startScale * 1.3f, t);
                if (sprite != null) sprite.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
