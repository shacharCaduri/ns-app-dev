using System;
using UnityEngine;

namespace WizardArena.Combat
{
    // Hit points and nothing else: owners react to the events with visuals and movement.
    // Disable the component to make it ignore damage (e.g. while spawning or leaving).
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private HealthConfig config;
        [SerializeField] private Team team;

        private int current;
        private float invulnerableUntil = float.NegativeInfinity;

        public event Action<DamageInfo> Damaged;
        public event Action<int> Healed;
        public event Action Died;

        public Team Team => team;
        public int Max => config != null ? config.MaxHealth : 1;
        public int Current => current;
        public bool IsDead => current <= 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        private void Awake()
        {
            RestoreFull();
        }

        // For objects built from code (spawners, tests); AddComponent runs Awake before this,
        // so a missing config is not an error until then. Also restores full health.
        public void Configure(HealthConfig healthConfig, Team healthTeam)
        {
            config = healthConfig;
            team = healthTeam;
            RestoreFull();
        }

        public bool TakeDamage(in DamageInfo damage)
        {
            if (!enabled || IsDead || damage.Amount <= 0 || !damage.Team.CanHurt(team) || IsInvulnerable)
                return false;

            current = Mathf.Max(0, current - damage.Amount);
            if (config != null && config.InvulnerabilitySeconds > 0f)
                invulnerableUntil = Time.time + config.InvulnerabilitySeconds;

            Damaged?.Invoke(damage);
            if (IsDead) Died?.Invoke();
            return true;
        }

        // Restores full health after death, e.g. when a stage restarts. The owner still
        // needs to reset its own visuals/position (see WizardController.Respawn).
        public void Revive()
        {
            RestoreFull();
        }

        // Dead things stay dead; healing never goes above Max.
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            int healed = Mathf.Min(Max, current + amount) - current;
            if (healed <= 0) return;
            current += healed;
            Healed?.Invoke(healed);
        }

        private void RestoreFull()
        {
            current = Max;
            invulnerableUntil = float.NegativeInfinity;
        }
    }
}
