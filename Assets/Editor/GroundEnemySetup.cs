using System;
using UnityEditor;
using UnityEngine;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.UI;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Builds the blue slime and skeleton warrior prefabs (once; later tuning in the config
    // assets is kept), the ground-enemy counterpart to EnemySetup's bat prefabs. StageRunner
    // instantiates these at play time; StageSetup wires them into stage spawn lists.
    internal static class GroundEnemySetup
    {
        private const string ConfigFolder = "Assets/Config/Enemies";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string SlimeRoot = "Assets/Art/Enemies/blue_slime";
        private const string SkeletonRoot = "Assets/Art/Enemies/skeleton_warrior";
        private const string SlimeConfigPath = ConfigFolder + "/BlueSlime.asset";
        private const string SkeletonConfigPath = ConfigFolder + "/SkeletonWarrior.asset";
        private const string SlimePrefabPath = PrefabFolder + "/BlueSlime.prefab";
        private const string SkeletonPrefabPath = PrefabFolder + "/SkeletonWarrior.prefab";

        internal static GameObject LoadOrCreateSlimePrefab()
        {
            return LoadOrCreatePrefab(SlimePrefabPath, BuildSlime, LoadOrCreateSlimeConfig());
        }

        internal static GameObject LoadOrCreateSkeletonPrefab()
        {
            return LoadOrCreatePrefab(SkeletonPrefabPath, BuildSkeleton, LoadOrCreateSkeletonConfig());
        }

        [MenuItem("Wizard Prototype/Build Ground Enemy Prefabs")]
        public static void BuildPrefabs()
        {
            LoadOrCreateSlimePrefab();
            LoadOrCreateSkeletonPrefab();
            AssetDatabase.SaveAssets();
        }

        // Unity -batchmode -projectPath <p> -executeMethod WizardArena.EditorTools.GroundEnemySetup.BuildPrefabsFromCommandLine
        public static void BuildPrefabsFromCommandLine()
        {
            try
            {
                BuildPrefabs();
                Validate();
                Debug.Log("[GroundEnemySetup] Built ground enemy prefabs");
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
            ValidatePrefab(SlimePrefabPath);
            ValidatePrefab(SkeletonPrefabPath);
        }

        private static void ValidatePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing ground enemy prefab " + path);
            Enemy enemy = prefab.GetComponent<Enemy>();
            if (enemy == null || enemy.Config == null) throw new InvalidOperationException(path + " has no Enemy with a config");
            if (enemy.Config.Health == null) throw new InvalidOperationException(path + " config has no HealthConfig");
            if (prefab.GetComponent<ContactDamage>() == null) throw new InvalidOperationException(path + " has no ContactDamage");
        }

        private static GameObject LoadOrCreatePrefab(string path, Func<EnemyConfig, GameObject> build, EnemyConfig config)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;

            GameObject instance = build(config);
            EnsureFolders(PrefabFolder);
            prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject BuildSlime(EnemyConfig config)
        {
            GameObject go = new GameObject("Blue Slime");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(SlimeRoot + "/walk/blue_slime_walk_right_01.png");
            renderer.sortingOrder = 10;

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            collider.offset = new Vector2(0f, 0.3f);

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            Health health = go.AddComponent<Health>();
            Enemy enemy = go.AddComponent<Enemy>();
            go.AddComponent<ContactDamage>();
            go.AddComponent<WorldHealthBar>();
            go.AddComponent<BlueSlimeController>();

            CombatSetup.BindHealth(health, config.Health, Team.Enemy);
            LinkConfig(enemy, config);
            return go;
        }

        private static GameObject BuildSkeleton(EnemyConfig config)
        {
            GameObject go = new GameObject("Skeleton Warrior");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(SkeletonRoot + "/walk/skeleton_warrior_walk_right_01.png");
            renderer.sortingOrder = 10;

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
            collider.offset = new Vector2(0f, 0.5f);

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            Health health = go.AddComponent<Health>();
            Enemy enemy = go.AddComponent<Enemy>();
            go.AddComponent<ContactDamage>();
            go.AddComponent<WorldHealthBar>();
            go.AddComponent<SkeletonWarriorController>();

            CombatSetup.BindHealth(health, config.Health, Team.Enemy);
            LinkConfig(enemy, config);
            return go;
        }

        private static void LinkConfig(Enemy enemy, EnemyConfig config)
        {
            SerializedObject data = new SerializedObject(enemy);
            data.FindProperty("config").objectReferenceValue = config;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyConfig LoadOrCreateSlimeConfig()
        {
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(SlimeConfigPath);
            if (config == null)
            {
                config = EnemyConfig.Create(CombatSetup.LoadOrCreateHealth("BlueSlimeHealth", 2, 0f));
                SerializedObject tuning = new SerializedObject(config);
                tuning.FindProperty("moveSpeed").floatValue = 0.6f;
                tuning.FindProperty("gravity").floatValue = 9f;
                tuning.FindProperty("chaseRange").floatValue = 3f;
                tuning.FindProperty("hopInterval").floatValue = 0.45f;
                tuning.FindProperty("hopSpeed").floatValue = 3.2f;
                tuning.FindProperty("contactDamage").intValue = 1;
                tuning.FindProperty("contactCooldown").floatValue = 0.6f;
                tuning.ApplyModifiedPropertiesWithoutUndo();
                CreateConfigAsset(config, SlimeConfigPath);
            }

            // Sprites are re-linked every rebuild; numeric tuning above is set once and
            // any later hand-tuning in the asset is kept (same convention as Bat.asset).
            SerializedObject data = new SerializedObject(config);
            ConfigureSpriteTextures(SlimeRoot, new Vector2(0.5f, 0f));
            BindSprites(data, SlimeRoot, "blue_slime");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static EnemyConfig LoadOrCreateSkeletonConfig()
        {
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(SkeletonConfigPath);
            if (config == null)
            {
                config = EnemyConfig.Create(CombatSetup.LoadOrCreateHealth("SkeletonWarriorHealth", 4, 0f));
                SerializedObject tuning = new SerializedObject(config);
                tuning.FindProperty("moveSpeed").floatValue = 1.1f;
                tuning.FindProperty("gravity").floatValue = 9f;
                tuning.FindProperty("chaseRange").floatValue = 3.5f;
                tuning.FindProperty("meleeRange").floatValue = 0.9f;
                tuning.FindProperty("meleeDamage").intValue = 2;
                tuning.FindProperty("attackWindUpSeconds").floatValue = 0.5f;
                tuning.FindProperty("attackRecoverSeconds").floatValue = 0.5f;
                tuning.FindProperty("contactDamage").intValue = 1;
                tuning.FindProperty("contactCooldown").floatValue = 0.6f;
                tuning.ApplyModifiedPropertiesWithoutUndo();
                CreateConfigAsset(config, SkeletonConfigPath);
            }

            // Sprites are re-linked every rebuild; numeric tuning above is set once and
            // any later hand-tuning in the asset is kept (same convention as Bat.asset).
            SerializedObject data = new SerializedObject(config);
            ConfigureSpriteTextures(SkeletonRoot, new Vector2(0.5f, 0f));
            BindSprites(data, SkeletonRoot, "skeleton_warrior");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void BindSprites(SerializedObject config, string root, string fileStem)
        {
            SerializedProperty sprites = config.FindProperty("sprites");
            AssignFrames(sprites.FindPropertyRelative("moveRight"), root + "/walk/" + fileStem + "_walk_right_0{0}.png", 2);
            AssignFrames(sprites.FindPropertyRelative("moveLeft"), root + "/walk/" + fileStem + "_walk_left_0{0}.png", 2);
            sprites.FindPropertyRelative("hurtRight").objectReferenceValue = LoadSprite(root + "/hurt/" + fileStem + "_hurt_right_01.png");
            sprites.FindPropertyRelative("hurtLeft").objectReferenceValue = LoadSprite(root + "/hurt/" + fileStem + "_hurt_left_01.png");
            sprites.FindPropertyRelative("deadRight").objectReferenceValue = LoadSprite(root + "/death/" + fileStem + "_death_right_01.png");
            sprites.FindPropertyRelative("deadLeft").objectReferenceValue = LoadSprite(root + "/death/" + fileStem + "_death_left_01.png");
            sprites.FindPropertyRelative("attackRight").objectReferenceValue = LoadSprite(root + "/attack/" + fileStem + "_attack_right_01.png");
            sprites.FindPropertyRelative("attackLeft").objectReferenceValue = LoadSprite(root + "/attack/" + fileStem + "_attack_left_01.png");
        }

        private static void AssignFrames(SerializedProperty property, string pathPattern, int frameCount)
        {
            property.arraySize = frameCount;
            for (int i = 0; i < frameCount; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(string.Format(pathPattern, i + 1));
        }

        private static void ConfigureSpriteTextures(string assetRoot, Vector2 pivot)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 128f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void CreateConfigAsset(Object asset, string path)
        {
            EnsureFolders(ConfigFolder);
            AssetDatabase.CreateAsset(asset, path);
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolders(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            string leaf = folder.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolders(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
