using UnityEngine;

namespace WizardArena.Combat
{
    // Everything about one kind of projectile: numbers and looks.
    [CreateAssetMenu(menuName = "Wizard Arena/Combat/Projectile Config", fileName = "ProjectileConfig")]
    public sealed class ProjectileConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0f)] private float speed = 11f;
        [SerializeField, Min(0.01f)] private float lifetime = 2f;
        [Tooltip("Radius of the swept circle used for hits, in world units.")]
        [SerializeField, Min(0f)] private float hitRadius = 0.12f;
        [Tooltip("Rendered width in world units; the sprite is scaled to match.")]
        [SerializeField, Min(0.01f)] private float visualWidth = 0.7f;
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private Sprite spriteRight;
        [SerializeField] private Sprite spriteLeft;

        public int Damage => damage;
        public float Speed => speed;
        public float Lifetime => lifetime;
        public float HitRadius => hitRadius;
        public float VisualWidth => visualWidth;
        public int SortingOrder => sortingOrder;

        public Sprite SpriteFor(Vector2 direction)
        {
            return direction.x < 0f && spriteLeft != null ? spriteLeft : spriteRight;
        }

        public static ProjectileConfig Create(int damage, float speed, float lifetime, float hitRadius)
        {
            ProjectileConfig config = CreateInstance<ProjectileConfig>();
            config.damage = Mathf.Max(1, damage);
            config.speed = Mathf.Max(0f, speed);
            config.lifetime = Mathf.Max(0.01f, lifetime);
            config.hitRadius = Mathf.Max(0f, hitRadius);
            return config;
        }
    }
}
