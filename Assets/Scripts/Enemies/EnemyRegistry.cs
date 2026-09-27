using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizardArena.Enemies
{
    // Knows which enemies are alive. An enemy is added when it first wakes up (Awake) and
    // removed the moment its Health dies (Died is raised then, not when the corpse is gone).
    // A deactivated enemy still counts. One destroyed while alive (e.g. scene unload) is
    // removed without Died.
    public static class EnemyRegistry
    {
        private static readonly HashSet<Enemy> AliveEnemies = new HashSet<Enemy>();

        public static event Action<Enemy> Spawned;
        public static event Action<Enemy> Died;

        public static int AliveCount => AliveEnemies.Count;

        internal static void Add(Enemy enemy)
        {
            if (AliveEnemies.Add(enemy)) Spawned?.Invoke(enemy);
        }

        internal static void MarkDead(Enemy enemy)
        {
            if (AliveEnemies.Remove(enemy)) Died?.Invoke(enemy);
        }

        internal static void Remove(Enemy enemy)
        {
            AliveEnemies.Remove(enemy);
        }

        // Static state survives play sessions when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AliveEnemies.Clear();
            Spawned = null;
            Died = null;
        }
    }
}
