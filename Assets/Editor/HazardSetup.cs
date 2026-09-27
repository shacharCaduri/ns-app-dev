using System;
using UnityEditor;
using UnityEngine;
using WizardArena.Combat;
using WizardArena.Hazards;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Builds the environmental hazard prefabs as self-contained assets (built once each, like
    // EnemySetup/GroundEnemySetup's prefabs). StageSetup places them per stage via
    // StageDefinition.PropSpawns.
    internal static class HazardSetup
    {
        private const string ConfigFolder = "Assets/Config/Hazards";
        private const string PrefabFolder = "Assets/Prefabs/Hazards";
        private const string HazardArt = "Assets/Art/Environments/Hazards";
        private const string PuzzleArt = "Assets/Art/Environments/Puzzle";
        private const string SpikesPrefabPath = PrefabFolder + "/Spikes.prefab";
        private const string ExplosiveCrystalPrefabPath = PrefabFolder + "/ExplosiveCrystal.prefab";
        private const string ExplosiveBarrelPrefabPath = PrefabFolder + "/ExplosiveBarrel.prefab";

        internal static GameObject LoadOrCreateSpikesPrefab()
        {
            return LoadOrCreatePrefab(SpikesPrefabPath, CreateSpikes);
        }

        internal static GameObject LoadOrCreateExplosiveCrystalPrefab()
        {
            return LoadOrCreatePrefab(ExplosiveCrystalPrefabPath, () => CreateExplosive("Explosive Crystal", PuzzleArt + "/energy_crystal.png"));
        }

        internal static GameObject LoadOrCreateExplosiveBarrelPrefab()
        {
            return LoadOrCreatePrefab(ExplosiveBarrelPrefabPath, () => CreateExplosive("Explosive Barrel", PuzzleArt + "/explosive_barrel.png"));
        }

        [MenuItem("Wizard Prototype/Build Hazard Prefabs")]
        public static void BuildPrefabs()
        {
            LoadOrCreateSpikesPrefab();
            LoadOrCreateExplosiveCrystalPrefab();
            LoadOrCreateExplosiveBarrelPrefab();
            AssetDatabase.SaveAssets();
        }

        // Unity -batchmode -quit -executeMethod WizardArena.EditorTools.HazardSetup.BuildPrefabsFromCommandLine
        public static void BuildPrefabsFromCommandLine()
        {
            try
            {
                BuildPrefabs();
                Validate();
                Debug.Log("[HazardSetup] Built hazard prefabs in " + PrefabFolder);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Validate()
        {
            foreach (string path in new[] { SpikesPrefabPath, ExplosiveCrystalPrefabPath, ExplosiveBarrelPrefabPath })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    throw new InvalidOperationException("Missing hazard prefab: " + path);
        }

        private static GameObject CreateSpikes()
        {
            GameObject spikes = new GameObject("Spikes");
            SpriteRenderer renderer = spikes.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HazardArt + "/spike_trap_extended.png");
            renderer.sortingOrder = 5;

            BoxCollider2D collider = spikes.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            if (renderer.sprite != null) collider.size = renderer.sprite.bounds.size;

            SpikeHazard hazard = spikes.AddComponent<SpikeHazard>();
            hazard.Configure(LoadOrCreateSpikeConfig());
            return spikes;
        }

        private static GameObject CreateExplosive(string name, string spritePath)
        {
            GameObject explosive = new GameObject(name);
            SpriteRenderer renderer = explosive.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.sortingOrder = 6;

            CircleCollider2D collider = explosive.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            Health health = explosive.AddComponent<Health>();
            CombatSetup.BindHealth(health, CombatSetup.LoadOrCreateHealth("ExplosiveHazardHealth", 1, 0f), Team.Neutral);

            ExplosiveHazard hazard = explosive.AddComponent<ExplosiveHazard>();
            hazard.Configure(LoadOrCreateExplosiveConfig());
            return explosive;
        }

        private static SpikeHazardConfig LoadOrCreateSpikeConfig()
        {
            string path = ConfigFolder + "/SpikeHazard.asset";
            SpikeHazardConfig config = AssetDatabase.LoadAssetAtPath<SpikeHazardConfig>(path);
            if (config != null) return config;
            config = SpikeHazardConfig.Create(2, 0.5f);
            EnsureFolder("Assets", "Config");
            EnsureFolder("Assets/Config", "Hazards");
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static ExplosiveHazardConfig LoadOrCreateExplosiveConfig()
        {
            string path = ConfigFolder + "/ExplosiveHazard.asset";
            ExplosiveHazardConfig config = AssetDatabase.LoadAssetAtPath<ExplosiveHazardConfig>(path);
            if (config != null) return config;
            config = ExplosiveHazardConfig.Create(3, 1.5f, 0.15f);
            EnsureFolder("Assets", "Config");
            EnsureFolder("Assets/Config", "Hazards");
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static GameObject LoadOrCreatePrefab(string path, Func<GameObject> factory)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;

            GameObject instance = factory();
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Hazards");
            prefab = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
            Object.DestroyImmediate(instance);
            if (!success || prefab == null) throw new InvalidOperationException("Failed to save prefab: " + path);
            return prefab;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
