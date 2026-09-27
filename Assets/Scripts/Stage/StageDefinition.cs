using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizardArena.Stage
{
    // Everything needed to run one stage: its name, where its enemies (and other scenery)
    // start, and where the player and the exit portal go. Assets live in
    // Assets/Config/Stage/Stages/. StageRunner is the only thing that reads this at runtime.
    [CreateAssetMenu(menuName = "Wizard Arena/Stage/Stage Definition", fileName = "StageDefinition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Stage";
        [SerializeField] private Vector3 playerSpawn;
        [SerializeField] private Vector3 portalPosition;
        [SerializeField] private EnemySpawn[] enemySpawns = Array.Empty<EnemySpawn>();
        [Tooltip("Hazards, pickups and other non-enemy scenery for this stage. Spawned the same " +
                 "way as enemies ([12] hazards; [13] pickups).")]
        [SerializeField] private PropSpawn[] propSpawns = Array.Empty<PropSpawn>();

        public string DisplayName => displayName;
        public Vector3 PlayerSpawn => playerSpawn;
        public Vector3 PortalPosition => portalPosition;
        public IReadOnlyList<EnemySpawn> EnemySpawns => enemySpawns;
        public IReadOnlyList<PropSpawn> PropSpawns => propSpawns;

        // For the scene builder and tests: build a stage without going through the AssetDatabase.
        public static StageDefinition Create(string displayName, Vector3 playerSpawn, Vector3 portalPosition,
            EnemySpawn[] enemySpawns, PropSpawn[] propSpawns = null)
        {
            StageDefinition stage = CreateInstance<StageDefinition>();
            stage.displayName = displayName;
            stage.playerSpawn = playerSpawn;
            stage.portalPosition = portalPosition;
            stage.enemySpawns = enemySpawns ?? Array.Empty<EnemySpawn>();
            stage.propSpawns = propSpawns ?? Array.Empty<PropSpawn>();
            return stage;
        }
    }

    // One enemy to spawn: a prefab that is already fully configured (Enemy, its EnemyConfig,
    // art, ContactDamage, ...) and where to put it. Adding an enemy to a stage is just adding
    // one of these -- see EnemySetup.LoadOrCreateBatPrefab for how a prefab like that is built.
    [Serializable]
    public sealed class EnemySpawn
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 position;

        public GameObject Prefab => prefab;
        public Vector3 Position => position;

        public static EnemySpawn At(GameObject prefab, Vector3 position)
        {
            return new EnemySpawn { prefab = prefab, position = position };
        }
    }

    // One piece of non-enemy stage scenery to spawn (hazard, pickup, decoration): a prefab
    // and where. Same shape as EnemySpawn, spawned the same way by StageRunner.
    [Serializable]
    public sealed class PropSpawn
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 position;

        public GameObject Prefab => prefab;
        public Vector3 Position => position;

        public static PropSpawn At(GameObject prefab, Vector3 position)
        {
            return new PropSpawn { prefab = prefab, position = position };
        }
    }
}
