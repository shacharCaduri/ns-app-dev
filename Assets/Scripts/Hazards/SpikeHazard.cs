using System.Collections.Generic;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Hazards
{
    // Damages any Health that touches it. Team.Neutral, so it hurts the wizard and enemies
    // alike; a per-target cooldown stops it from re-hitting the same Health every frame while
    // it stands there (independent of that Health's own invulnerability window, which may be 0).
    [RequireComponent(typeof(Collider2D))]
    public sealed class SpikeHazard : MonoBehaviour
    {
        private static readonly List<Collider2D> Touching = new List<Collider2D>();

        [SerializeField] private SpikeHazardConfig config;

        private Collider2D area;
        private readonly Dictionary<Health, float> nextHitTime = new Dictionary<Health, float>();

        // For hazards built from code (the builder, tests).
        public void Configure(SpikeHazardConfig hazardConfig) => config = hazardConfig;

        private void Awake()
        {
            area = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (config == null) return;

            int count = area.Overlap(ContactFilter2D.noFilter, Touching);
            for (int i = 0; i < count; i++)
            {
                Health target = Touching[i].GetComponentInParent<Health>();
                if (target == null || target.IsDead) continue;
                if (nextHitTime.TryGetValue(target, out float readyAt) && Time.time < readyAt) continue;

                if (target.TakeDamage(new DamageInfo(config.Damage, transform.position, gameObject, Team.Neutral)))
                    nextHitTime[target] = Time.time + config.CooldownSeconds;
            }
        }
    }
}
