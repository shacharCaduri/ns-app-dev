using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.UI;
using WizardArena.World;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Builds enemy prefabs and the arena bounds they move in. Enemy config assets and
    // prefabs are created once (later tuning is kept); a config's sprites are re-linked on
    // every rebuild. StageRunner instantiates these prefabs at play time -- the scene itself
    // holds no hand-placed enemy.
    internal static class EnemySetup
    {
        private const string ConfigFolder = "Assets/Config/Enemies";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string BatRoot = "Assets/Art/Enemies/cave_bat";
        private const string BatConfigPath = ConfigFolder + "/Bat.asset";
        private const string BatSwiftConfigPath = ConfigFolder + "/BatSwift.asset";
        private const string BatPrefabPath = PrefabFolder + "/Bat.prefab";
        private const string BatSwiftPrefabPath = PrefabFolder + "/BatSwift.prefab";
        // Stage 2/3 bats fly a bit faster than stage 1's (EnemyConfig's own default is 2.2).
        private const float SwiftMoveSpeed = 2.9f;
        private const string BoundsName = "Arena Bounds";
        // Where flying enemies keep their centre: inside the camera view, above the -2.4 floor.
        private static readonly Rect FlightArea = Rect.MinMaxRect(-7f, -1.8f, 7f, 3.5f);

        // The stage 1 bat: normal speed.
        internal static GameObject LoadOrCreateBatPrefab()
        {
            return LoadOrCreatePrefab(BatPrefabPath, LoadOrCreateBatConfig(BatConfigPath, null));
        }

        // The stage 2/3 bat: same everything, just faster.
        internal static GameObject LoadOrCreateBatSwiftPrefab()
        {
            return LoadOrCreatePrefab(BatSwiftPrefabPath, LoadOrCreateBatConfig(BatSwiftConfigPath, SwiftMoveSpeed));
        }

        internal static void EnsureArenaBounds(Scene scene)
        {
            ArenaBounds bounds = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.TryGetComponent(out ArenaBounds found)) bounds = found;
            if (bounds == null) bounds = new GameObject(BoundsName).AddComponent<ArenaBounds>();
            SerializedObject data = new SerializedObject(bounds);
            data.FindProperty("area").rectValue = FlightArea;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Validate()
        {
            ValidatePrefab(BatPrefabPath);
            ValidatePrefab(BatSwiftPrefabPath);
            if (Object.FindFirstObjectByType<ArenaBounds>() == null) throw new InvalidOperationException("No ArenaBounds in scene");
        }

        private static void ValidatePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing enemy prefab " + path);
            Enemy enemy = prefab.GetComponent<Enemy>();
            if (enemy == null || enemy.Config == null) throw new InvalidOperationException(path + " has no Enemy with a config");
            if (enemy.Config.Health == null) throw new InvalidOperationException(path + " config has no HealthConfig");
            if (prefab.GetComponent<ContactDamage>() == null) throw new InvalidOperationException(path + " has no ContactDamage");
        }

        private static GameObject LoadOrCreatePrefab(string path, EnemyConfig config)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;

            GameObject instance = BuildBat(config);
            EnsureFolders(PrefabFolder);
            prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject BuildBat(EnemyConfig config)
        {
            GameObject bat = new GameObject("Bat");
            SpriteRenderer renderer = bat.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BatRoot + "/fly/cave_bat_fly_left_01.png");
            renderer.sortingOrder = 10;

            CircleCollider2D collider = bat.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;

            Rigidbody2D rigidbody = bat.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;

            Health health = bat.AddComponent<Health>();
            Enemy enemy = bat.AddComponent<Enemy>();
            bat.AddComponent<ContactDamage>();
            bat.AddComponent<WorldHealthBar>();
            bat.AddComponent<BatEnemyController>();
            bat.AddComponent<EnemyDropper>();

            CombatSetup.BindHealth(health, config.Health, Team.Enemy);
            SerializedObject enemyData = new SerializedObject(enemy);
            enemyData.FindProperty("config").objectReferenceValue = config;
            enemyData.ApplyModifiedPropertiesWithoutUndo();
            return bat;
        }

        private static EnemyConfig LoadOrCreateBatConfig(string path, float? moveSpeedOverride)
        {
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (config == null)
            {
                config = EnemyConfig.Create(CombatSetup.LoadOrCreateHealth("BatHealth", 3, 0f));
                if (moveSpeedOverride.HasValue)
                {
                    SerializedObject speedData = new SerializedObject(config);
                    speedData.FindProperty("moveSpeed").floatValue = moveSpeedOverride.Value;
                    speedData.ApplyModifiedPropertiesWithoutUndo();
                }
                EnsureFolders(ConfigFolder);
                AssetDatabase.CreateAsset(config, path);
                // [13]: a 1-in-5 chance of a health potion, set once and kept afterward like
                // the tuning above.
                SetDrops(config, DropEntry.At(PickupSetup.LoadOrCreateHealthPotionPrefab(), 0.2f));
            }

            SerializedObject data = new SerializedObject(config);
            SerializedProperty sprites = data.FindProperty("sprites");
            AssignFrames(sprites.FindPropertyRelative("moveRight"), BatRoot + "/fly/cave_bat_fly_right_0{0}.png", 2);
            AssignFrames(sprites.FindPropertyRelative("moveLeft"), BatRoot + "/fly/cave_bat_fly_left_0{0}.png", 2);
            sprites.FindPropertyRelative("hurtRight").objectReferenceValue = LoadSprite(BatRoot + "/hurt/cave_bat_hurt_right_01.png");
            sprites.FindPropertyRelative("hurtLeft").objectReferenceValue = LoadSprite(BatRoot + "/hurt/cave_bat_hurt_left_01.png");
            sprites.FindPropertyRelative("deadRight").objectReferenceValue = LoadSprite(BatRoot + "/death/cave_bat_death_right_01.png");
            sprites.FindPropertyRelative("deadLeft").objectReferenceValue = LoadSprite(BatRoot + "/death/cave_bat_death_left_01.png");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        // Sets an EnemyConfig's drop table (EnemyConfig.Drops), reflecting each DropEntry's
        // private fields the same way other tuning is set from editor code.
        private static void SetDrops(EnemyConfig config, params DropEntry[] entries)
        {
            SerializedObject data = new SerializedObject(config);
            SerializedProperty property = data.FindProperty("drops");
            property.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("prefab").objectReferenceValue = entries[i].Prefab;
                element.FindPropertyRelative("chance").floatValue = entries[i].Chance;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignFrames(SerializedProperty property, string pathPattern, int frameCount)
        {
            property.arraySize = frameCount;
            for (int i = 0; i < frameCount; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(string.Format(pathPattern, i + 1));
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
