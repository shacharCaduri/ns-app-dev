using UnityEngine;

namespace WizardArena.Hazards
{
    [CreateAssetMenu(menuName = "Wizard Arena/Hazards/Spike Hazard Config", fileName = "SpikeHazardConfig")]
    public sealed class SpikeHazardConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int damage = 2;
        [Tooltip("Seconds a given target is ignored after being hit, so standing on the spikes doesn't shred it every frame.")]
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.5f;

        public int Damage => damage;
        public float CooldownSeconds => cooldownSeconds;

        public static SpikeHazardConfig Create(int damage, float cooldownSeconds)
        {
            SpikeHazardConfig config = CreateInstance<SpikeHazardConfig>();
            config.damage = Mathf.Max(1, damage);
            config.cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            return config;
        }
    }
}
