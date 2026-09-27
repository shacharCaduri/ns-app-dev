using UnityEngine;

namespace WizardArena.Combat
{
    // Flies straight and damages the first IDamageable that accepts the hit.
    // Each frame it sweeps a small circle from its previous to its new position
    // (Physics2D.CircleCast, triggers included), so fast bolts cannot tunnel through
    // thin targets. Targets only need a Collider2D and an IDamageable on it or a parent.
    // Spawn through ProjectileLauncher.
    public sealed class Projectile : MonoBehaviour
    {
        private const int MaxHitsPerFrame = 8;
        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[MaxHitsPerFrame];
        private static readonly ContactFilter2D AnyCollider = new ContactFilter2D().NoFilter();

        private ProjectileConfig config;
        private Vector2 direction;
        private Team team;
        private GameObject owner;
        private float age;

        internal static Projectile Spawn(ProjectileConfig config, Vector3 origin, Vector2 direction, Team team, GameObject owner)
        {
            GameObject body = new GameObject(config.name + " Projectile");
            body.transform.position = origin;
            Projectile projectile = body.AddComponent<Projectile>();
            projectile.config = config;
            projectile.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            projectile.team = team;
            projectile.owner = owner;

            SpriteRenderer spriteRenderer = body.AddComponent<SpriteRenderer>();
            Sprite sprite = config.SpriteFor(projectile.direction);
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = config.SortingOrder;
            if (sprite != null) body.transform.localScale = Vector3.one * (config.VisualWidth / sprite.bounds.size.x);
            return projectile;
        }

        private void Update()
        {
            Vector3 start = transform.position;
            Vector2 step = direction * (config.Speed * Time.deltaTime);
            transform.position = start + (Vector3)step;
            if (HitSomethingAlong(start, step))
            {
                Destroy(gameObject);
                return;
            }

            age += Time.deltaTime;
            if (age >= config.Lifetime) Destroy(gameObject);
        }

        private bool HitSomethingAlong(Vector2 start, Vector2 step)
        {
            float distance = step.magnitude;
            Vector2 castDirection = distance > 0f ? step / distance : direction;
            int count = Physics2D.CircleCast(start, config.HitRadius, castDirection, AnyCollider, Hits, distance);
            for (int i = 0; i < count; i++)
            {
                Collider2D hitCollider = Hits[i].collider;
                if (owner != null && hitCollider.transform.IsChildOf(owner.transform)) continue;
                IDamageable target = hitCollider.GetComponentInParent<IDamageable>();
                // Same team, dead or invulnerable targets refuse the hit and the bolt flies on.
                if (target != null && target.TakeDamage(new DamageInfo(config.Damage, start, owner, team)))
                    return true;
            }
            return false;
        }
    }
}
