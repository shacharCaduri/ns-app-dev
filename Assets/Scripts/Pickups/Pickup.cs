using System.Collections.Generic;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Pickups
{
    // Collected by touch, like a hazard's overlap check (SpikeHazard) but only for a
    // Team.Player Health. Bobs gently in place, blinks in its last BlinkSeconds, and despawns
    // on its own after LifetimeSeconds so drops don't litter a stage forever. Effects
    // (HealEffect, ...) live as sibling components; the pickup consumes itself only if at
    // least one effect actually applied -- e.g. a health potion touched at full HP is left in
    // place rather than wasted.
    [RequireComponent(typeof(Collider2D))]
    public sealed class Pickup : MonoBehaviour
    {
        private const float BlinkPeriodSeconds = 0.15f;
        private static readonly List<Collider2D> Touching = new List<Collider2D>();

        [SerializeField] private PickupConfig config;

        private Collider2D area;
        private SpriteRenderer sprite;
        private Vector3 basePosition;
        private float age;
        private bool gone;

        // For pickups built from code (the builder, tests).
        public void Configure(PickupConfig pickupConfig) => config = pickupConfig;

        private void Awake()
        {
            area = GetComponent<Collider2D>();
            sprite = GetComponent<SpriteRenderer>();
            basePosition = transform.position;
            PickupRegistry.NotifySpawned(gameObject);
        }

        private void Update()
        {
            if (gone) return;

            Bob();
            if (TickLifetime()) return;
            CheckCollection();
        }

        private void Bob()
        {
            if (config == null || config.BobHeight <= 0f) return;
            float offset = Mathf.Sin(Time.time * config.BobSpeed) * config.BobHeight;
            transform.position = basePosition + Vector3.up * offset;
        }

        // Returns true once the pickup has despawned this frame -- nothing else to do.
        private bool TickLifetime()
        {
            if (config == null) return false;
            age += Time.deltaTime;
            float remaining = config.LifetimeSeconds - age;

            if (sprite != null && config.BlinkSeconds > 0f && remaining <= config.BlinkSeconds)
                sprite.enabled = remaining > 0f && Mathf.FloorToInt(remaining / BlinkPeriodSeconds) % 2 == 0;

            if (remaining > 0f) return false;

            gone = true;
            Destroy(gameObject);
            return true;
        }

        private void CheckCollection()
        {
            int count = area.Overlap(ContactFilter2D.noFilter, Touching);
            for (int i = 0; i < count; i++)
            {
                Health target = Touching[i].GetComponentInParent<Health>();
                if (target == null || target.Team != Team.Player || target.IsDead) continue;
                if (TryCollect(target.gameObject)) return;
            }
        }

        private bool TryCollect(GameObject collector)
        {
            // Queried fresh rather than cached in Awake: sibling effects (HealEffect, ...) may
            // be added right after Pickup itself (the builder, tests), not necessarily before it.
            bool applied = false;
            foreach (IPickupEffect effect in GetComponents<IPickupEffect>())
                if (effect.Apply(collector)) applied = true;
            if (!applied) return false;

            gone = true;
            Destroy(gameObject);
            return true;
        }
    }
}
