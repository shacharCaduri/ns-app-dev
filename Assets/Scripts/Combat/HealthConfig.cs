using UnityEngine;

namespace WizardArena.Combat
{
    [CreateAssetMenu(menuName = "Wizard Arena/Combat/Health Config", fileName = "HealthConfig")]
    public sealed class HealthConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [Tooltip("Seconds after a hit during which further hits are ignored. 0 = none.")]
        [SerializeField, Min(0f)] private float invulnerabilitySeconds;

        public int MaxHealth => maxHealth;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;

        public static HealthConfig Create(int maxHealth, float invulnerabilitySeconds)
        {
            HealthConfig config = CreateInstance<HealthConfig>();
            config.maxHealth = Mathf.Max(1, maxHealth);
            config.invulnerabilitySeconds = Mathf.Max(0f, invulnerabilitySeconds);
            return config;
        }
    }
}
