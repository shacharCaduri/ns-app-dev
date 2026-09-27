using UnityEngine;

namespace WizardArena.Pickups
{
    [CreateAssetMenu(menuName = "Wizard Arena/Pickups/Pickup Config", fileName = "PickupConfig")]
    public sealed class PickupConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float lifetimeSeconds = 12f;
        [Tooltip("Seconds before despawn that the pickup starts blinking as a warning.")]
        [SerializeField, Min(0f)] private float blinkSeconds = 2f;
        [SerializeField, Min(0f)] private float bobHeight = 0.08f;
        [SerializeField, Min(0.1f)] private float bobSpeed = 2.5f;

        public float LifetimeSeconds => lifetimeSeconds;
        public float BlinkSeconds => blinkSeconds;
        public float BobHeight => bobHeight;
        public float BobSpeed => bobSpeed;

        public static PickupConfig Create(float lifetimeSeconds, float blinkSeconds, float bobHeight = 0.08f, float bobSpeed = 2.5f)
        {
            PickupConfig config = CreateInstance<PickupConfig>();
            config.lifetimeSeconds = Mathf.Max(0.1f, lifetimeSeconds);
            config.blinkSeconds = Mathf.Max(0f, blinkSeconds);
            config.bobHeight = Mathf.Max(0f, bobHeight);
            config.bobSpeed = Mathf.Max(0.1f, bobSpeed);
            return config;
        }
    }
}
