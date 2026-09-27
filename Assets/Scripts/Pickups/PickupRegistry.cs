using System;
using UnityEngine;

namespace WizardArena.Pickups
{
    // Lets other layers learn about a pickup the moment it spawns, without a lookup. Stage
    // uses this to track a pickup an EnemyDropper spawned at runtime (never through
    // StageDefinition.PropSpawns) so it can still be cleaned up on the next stage/retry, the
    // same way EnemyRegistry lets Stage learn about enemies.
    public static class PickupRegistry
    {
        public static event Action<GameObject> Spawned;

        internal static void NotifySpawned(GameObject pickup) => Spawned?.Invoke(pickup);

        // Static state survives play sessions when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Spawned = null;
    }
}
