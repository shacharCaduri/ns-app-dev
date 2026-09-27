using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Pickups
{
    // Heals through Health.Heal. Does not consume the pickup if the collector has no Health,
    // is dead, or is already at full HP -- it is left in place for the player to grab later.
    public sealed class HealEffect : MonoBehaviour, IPickupEffect
    {
        [SerializeField, Min(1)] private int amount = 4;

        public int Amount => amount;

        // For pickups built from code (the builder, tests).
        public void Configure(int healAmount) => amount = Mathf.Max(1, healAmount);

        public bool Apply(GameObject collector)
        {
            Health health = collector.GetComponentInParent<Health>();
            if (health == null || health.IsDead || health.Current >= health.Max) return false;

            health.Heal(amount);
            return true;
        }
    }
}
