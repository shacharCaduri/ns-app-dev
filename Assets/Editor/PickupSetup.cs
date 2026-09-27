using System;
using UnityEditor;
using UnityEngine;
using WizardArena.Pickups;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Builds the pickup prefabs as self-contained assets (built once each, like
    // EnemySetup/GroundEnemySetup/HazardSetup's prefabs). EnemySetup/GroundEnemySetup wire
    // these into EnemyConfig.Drops; StageSetup can also place one directly via
    // StageDefinition.PropSpawns.
    internal static class PickupSetup
    {
        private const string ConfigFolder = "Assets/Config/Pickups";
        private const string PrefabFolder = "Assets/Prefabs/Pickups";
        private const string PotionArt = "Assets/Art/Items/Potions";
        private const string HealthPotionPrefabPath = PrefabFolder + "/HealthPotion.prefab";

        internal static GameObject LoadOrCreateHealthPotionPrefab()
        {
            return LoadOrCreatePrefab(HealthPotionPrefabPath, CreateHealthPotion);
        }

        [MenuItem("Wizard Prototype/Build Pickup Prefabs")]
        public static void BuildPrefabs()
        {
            LoadOrCreateHealthPotionPrefab();
            AssetDatabase.SaveAssets();
        }

        // Unity -batchmode -quit -executeMethod WizardArena.EditorTools.PickupSetup.BuildPrefabsFromCommandLine
        public static void BuildPrefabsFromCommandLine()
        {
            try
            {
                BuildPrefabs();
                Validate();
                Debug.Log("[PickupSetup] Built pickup prefabs in " + PrefabFolder);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthPotionPrefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing pickup prefab " + HealthPotionPrefabPath);
            if (prefab.GetComponent<Pickup>() == null) throw new InvalidOperationException(HealthPotionPrefabPath + " has no Pickup");
            if (prefab.GetComponent<HealEffect>() == null) throw new InvalidOperationException(HealthPotionPrefabPath + " has no HealEffect");
        }

        private static GameObject CreateHealthPotion()
        {
            GameObject potion = new GameObject("Health Potion");
            SpriteRenderer renderer = potion.AddComponent<SpriteRenderer>();
            ConfigureItemTexture(PotionArt + "/potion_health_small.png");
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PotionArt + "/potion_health_small.png");
            renderer.sortingOrder = 6;

            CircleCollider2D collider = potion.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.2f;

            Pickup pickup = potion.AddComponent<Pickup>();
            pickup.Configure(LoadOrCreatePickupConfig());

            potion.AddComponent<HealEffect>().Configure(4);
            return potion;
        }

        private static PickupConfig LoadOrCreatePickupConfig()
        {
            string path = ConfigFolder + "/HealthPotion.asset";
            PickupConfig config = AssetDatabase.LoadAssetAtPath<PickupConfig>(path);
            if (config != null) return config;
            config = PickupConfig.Create(12f, 2f, 0.08f, 2.5f);
            EnsureFolder("Assets", "Config");
            EnsureFolder("Assets/Config", "Pickups");
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        // Item art (Assets/Art/Items) isn't pre-configured as Sprite the way enemy/hazard art
        // is (GroundEnemySetup.ConfigureSpriteTextures) -- set it up once here so it imports
        // as a small, centred pickup rather than a near-enemy-sized image.
        private static void ConfigureItemTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static GameObject LoadOrCreatePrefab(string path, Func<GameObject> factory)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;

            GameObject instance = factory();
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Pickups");
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
