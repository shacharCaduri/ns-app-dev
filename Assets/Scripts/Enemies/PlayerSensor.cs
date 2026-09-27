using System.Collections.Generic;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Enemies
{
    // Finds the nearest Team.Player Health within range via a physics query, so a ground
    // enemy state can react to the wizard without ever referencing Player code
    // (ARCHITECTURE.md: Enemies and Player never reference each other).
    internal static class PlayerSensor
    {
        private static readonly List<Collider2D> Hits = new List<Collider2D>();
        private static readonly ContactFilter2D AnyCollider = ContactFilter2D.noFilter;

        internal static bool TryFind(Vector2 from, float radius, out Health target)
        {
            target = null;
            if (radius <= 0f) return false;

            int count = Physics2D.OverlapCircle(from, radius, AnyCollider, Hits);
            float closestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Health candidate = Hits[i].GetComponentInParent<Health>();
                if (candidate == null || candidate.Team != Team.Player || candidate.IsDead) continue;

                float distanceSqr = ((Vector2)candidate.transform.position - from).sqrMagnitude;
                if (distanceSqr >= closestSqr) continue;
                closestSqr = distanceSqr;
                target = candidate;
            }
            return target != null;
        }
    }
}
