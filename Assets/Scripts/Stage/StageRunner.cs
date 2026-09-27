using System;
using System.Collections.Generic;
using UnityEngine;
using WizardArena.Pickups;
using WizardArena.Player;

namespace WizardArena.Stage
{
    // Plays a StageSequence's stages one after another inside this one scene (no scene
    // reload for progression): on every stage start it clears whatever the last stage left
    // behind (enemies, corpses, props, dropped pickups), spawns the new stage's enemies from
    // their prefabs, puts the wizard back at its spawn and moves the portal into place.
    // Registers itself with GameSession as what Retry()/Continue() actually do.
    public sealed class StageRunner : MonoBehaviour, IStageProgression
    {
        [SerializeField] private StageSequence sequence;
        [SerializeField] private WizardController player;
        [SerializeField] private StagePortal portal;
        [SerializeField] private GameSession session;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private int currentIndex;

        // Raised whenever a stage starts (including the first, from Awake): what the HUD (or
        // anything else) should show for it. A late subscriber should also read CurrentStage
        // directly once (e.g. from its own Start), the same way HudController reads Health.
        public event Action<StageDefinition> StageStarted;

        public StageSequence Sequence => sequence;
        public StageDefinition CurrentStage => sequence.Stages[currentIndex];
        public bool IsLastStage => currentIndex == sequence.Stages.Count - 1;

        private void Awake()
        {
            session.Progression = this;
            currentIndex = 0;
            StageDefinition stage = CurrentStage;
            PlacePortal(stage);
            SpawnStageContent(stage);
            // Not player.Respawn here: the wizard's own components run their Awake around the
            // same time as this one, in no guaranteed order, and the scene builder already
            // places it at the first stage's spawn point.
            StageStarted?.Invoke(stage);
        }

        private void OnEnable() => PickupRegistry.Spawned += RegisterSpawned;
        private void OnDisable() => PickupRegistry.Spawned -= RegisterSpawned;

        private void OnDestroy()
        {
            if (session != null && ReferenceEquals(session.Progression, this)) session.Progression = null;
        }

        // A pickup an EnemyDropper spawned at runtime (never through PropSpawns) still needs
        // to be cleaned up like anything else the current stage put in the scene. Registering
        // it here (rather than the dropper knowing about StageRunner) keeps Enemies from
        // depending on Stage. A PropSpawn-placed pickup notifies this too, once this is
        // subscribed (harmless: SpawnStageContent already tracks it directly).
        private void RegisterSpawned(GameObject pickup)
        {
            if (!spawned.Contains(pickup)) spawned.Add(pickup);
        }

        void IStageProgression.RestartCurrentStage() => BeginStage(currentIndex);

        void IStageProgression.AdvanceToNextStage() => BeginStage(Mathf.Min(currentIndex + 1, sequence.Stages.Count - 1));

        private void BeginStage(int index)
        {
            currentIndex = index;
            ClearSpawnedContent();
            StageDefinition stage = CurrentStage;
            portal.Close();
            PlacePortal(stage);
            SpawnStageContent(stage);
            player.Respawn(stage.PlayerSpawn);
            session.BeginStage();
            StageStarted?.Invoke(stage);
        }

        private void PlacePortal(StageDefinition stage)
        {
            portal.transform.position = stage.PortalPosition;
            portal.ClearOutcome = IsLastStage ? GameState.Victory : GameState.StageCleared;
        }

        private void SpawnStageContent(StageDefinition stage)
        {
            foreach (EnemySpawn enemySpawn in stage.EnemySpawns)
            {
                if (enemySpawn.Prefab == null) continue;
                spawned.Add(Instantiate(enemySpawn.Prefab, enemySpawn.Position, Quaternion.identity));
            }
            foreach (PropSpawn propSpawn in stage.PropSpawns)
            {
                if (propSpawn.Prefab == null) continue;
                spawned.Add(Instantiate(propSpawn.Prefab, propSpawn.Position, Quaternion.identity));
            }
        }

        private void ClearSpawnedContent()
        {
            foreach (GameObject leftover in spawned)
                if (leftover != null) Destroy(leftover);
            spawned.Clear();
        }
    }
}
