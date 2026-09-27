using UnityEngine;

namespace WizardArena.Combat
{
    // Put this on whatever shoots. It fires its configured projectile for its team;
    // the shooter's own colliders are never hit.
    public sealed class ProjectileLauncher : MonoBehaviour
    {
        [SerializeField] private ProjectileConfig projectile;
        [SerializeField] private Team team;

        public Team Team => team;

        // For launchers built from code (spawners, tests).
        public void Configure(ProjectileConfig projectileConfig, Team launcherTeam)
        {
            projectile = projectileConfig;
            team = launcherTeam;
        }

        // Returns null when no projectile is configured.
        public Projectile Fire(Vector3 origin, Vector2 direction)
        {
            if (projectile == null) return null;
            return Projectile.Spawn(projectile, origin, direction, team, gameObject);
        }
    }
}
