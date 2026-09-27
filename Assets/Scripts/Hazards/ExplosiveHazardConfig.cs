using UnityEngine;

namespace WizardArena.Hazards
{
    [CreateAssetMenu(menuName = "Wizard Arena/Hazards/Explosive Hazard Config", fileName = "ExplosiveHazardConfig")]
    public sealed class ExplosiveHazardConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int damage = 3;
        [SerializeField, Min(0.1f)] private float radius = 1.5f;
        [Tooltip("How long the destroy flash/scale plays before the hazard is removed.")]
        [SerializeField, Min(0.05f)] private float flashSeconds = 0.15f;

        public int Damage => damage;
        public float Radius => radius;
        public float FlashSeconds => flashSeconds;

        public static ExplosiveHazardConfig Create(int damage, float radius, float flashSeconds)
        {
            ExplosiveHazardConfig config = CreateInstance<ExplosiveHazardConfig>();
            config.damage = Mathf.Max(1, damage);
            config.radius = Mathf.Max(0.1f, radius);
            config.flashSeconds = Mathf.Max(0.05f, flashSeconds);
            return config;
        }
    }
}
