using System;
using System.Collections.Generic;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Enemies
{
    // Hurts any Health of another team that the enemy's circle touches, then waits out
    // the contact cooldown. No target reference is needed, so spawned enemies work too.
    // The circle is read from the current transform every frame, because enemies move by
    // transform and physics only syncs moved colliders on the next fixed step.
    [RequireComponent(typeof(Enemy), typeof(CircleCollider2D))]
    public sealed class ContactDamage : MonoBehaviour
    {
        private static readonly List<Collider2D> Touching = new List<Collider2D>();
        private static readonly ContactFilter2D AnyCollider = ContactFilter2D.noFilter;

        private Enemy enemy;
        private CircleCollider2D body;
        private float cooldownLeft;

        // Raised on every touch of a hostile target, even if it refused the damage
        // (invulnerable); the argument points from the target towards this enemy.
        public event Action<Vector2> Touched;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            body = GetComponent<CircleCollider2D>();
        }

        // After movement, so the check sees this frame's position.
        private void LateUpdate()
        {
            cooldownLeft -= Time.deltaTime;
            if (cooldownLeft > 0f || !body.enabled || enemy.Config == null) return;

            Vector2 center = transform.TransformPoint(body.offset);
            Vector3 scale = transform.lossyScale;
            float radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            int count = Physics2D.OverlapCircle(center, radius, AnyCollider, Touching);
            for (int i = 0; i < count; i++)
            {
                Health target = Touching[i].GetComponentInParent<Health>();
                if (!IsHostile(target)) continue;

                target.TakeDamage(new DamageInfo(enemy.Config.ContactDamage, transform.position, gameObject, enemy.Health.Team));
                cooldownLeft = enemy.Config.ContactCooldown;
                Touched?.Invoke((center - (Vector2)Touching[i].bounds.center).normalized);
                return;
            }
        }

        private bool IsHostile(Health target)
        {
            return target != null && target != enemy.Health && !target.IsDead && enemy.Health.Team.CanHurt(target.Team);
        }
    }
}
