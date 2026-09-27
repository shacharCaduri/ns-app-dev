using UnityEngine;
using WizardArena.World;

namespace WizardArena.Enemies
{
    // Drops loot at the enemy's death position, on the ground below it if there is one (the
    // same lookup DeadState uses to land the corpse). Each entry in EnemyConfig.Drops is
    // rolled independently, so more than one item can drop from the same death. This lives in
    // Enemies, not the dropped item's own area (e.g. Pickups), because ARCHITECTURE.md lets
    // Enemies use any layer below it and this only ever Instantiates a generic prefab -- it
    // never references what that prefab actually is.
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyDropper : MonoBehaviour
    {
        private Enemy enemy;

        private void Awake() => enemy = GetComponent<Enemy>();
        private void OnEnable() => enemy.Health.Died += OnDied;
        private void OnDisable() => enemy.Health.Died -= OnDied;

        private void OnDied()
        {
            EnemyConfig config = enemy.Config;
            if (config == null) return;

            foreach (DropEntry drop in config.Drops)
            {
                if (drop.Prefab == null) continue;
                if (!ShouldDrop(drop.Chance, Random.value)) continue;
                Instantiate(drop.Prefab, DropPosition(), Quaternion.identity);
            }
        }

        private Vector3 DropPosition()
        {
            Vector2 position = transform.position;
            if (ArenaSurface.TryGetGroundBelow(position, out float groundY)) position.y = groundY;
            return position;
        }

        // A roll of exactly 0 or 1 is decided without touching Random, so a 100%/0% entry in
        // EnemyConfig is exact rather than "almost always/never" (Random.value can return 1).
        internal static bool ShouldDrop(float chance, float roll)
        {
            if (chance >= 1f) return true;
            if (chance <= 0f) return false;
            return roll < chance;
        }
    }
}
