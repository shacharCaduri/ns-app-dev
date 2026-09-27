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
    // Builds enemies and the arena bounds they move in. Enemy config assets are created
    // once (later tuning is kept); their sprites are re-linked on every rebuild.
    internal static class EnemySetup
    {
        private const string ConfigFolder = "Assets/Config/Enemies";
        private const string BatRoot = "Assets/Art/Enemies/cave_bat";
        private const string BoundsName = "Arena Bounds";
        // Where flying enemies keep their centre: inside the camera view, above the -2.4 floor.
        private static readonly Rect FlightArea = Rect.MinMaxRect(-7f, -1.8f, 7f, 3.5f);

        internal static GameObject CreateBat(Vector3 position)
        {
            GameObject bat = new GameObject("Bat Enemy");
            bat.transform.position = position;

            SpriteRenderer renderer = bat.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BatRoot + "/fly/cave_bat_fly_left_01.png");
            renderer.sortingOrder = 10;

            CircleCollider2D collider = bat.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;

            Rigidbody2D rigidbody = bat.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;

            bat.AddComponent<BatEnemyController>();
            BindBat(bat);
            return bat;
        }

        // Adds any missing enemy components (also upgrades a bat from an older scene)
        // and links the bat config.
        internal static void BindBat(GameObject bat)
        {
            Ensure<Health>(bat);
            Ensure<Enemy>(bat);
            Ensure<ContactDamage>(bat);
            Ensure<WorldHealthBar>(bat);

            EnemyConfig config = LoadOrCreateBatConfig();
            CombatSetup.BindHealth(bat.GetComponent<Health>(), config.Health, Team.Enemy);
            SerializedObject enemy = new SerializedObject(bat.GetComponent<Enemy>());
            enemy.FindProperty("config").objectReferenceValue = config;
            enemy.ApplyModifiedPropertiesWithoutUndo();
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
            BatEnemyController bat = Object.FindFirstObjectByType<BatEnemyController>();
            if (bat == null) throw new InvalidOperationException("No bat in scene");
            Enemy enemy = bat.GetComponent<Enemy>();
            if (enemy == null || enemy.Config == null) throw new InvalidOperationException("Bat has no Enemy with a config");
            if (enemy.Config.Health == null) throw new InvalidOperationException("Bat config has no HealthConfig");
            if (bat.GetComponent<ContactDamage>() == null) throw new InvalidOperationException("Bat has no ContactDamage");
            if (Object.FindFirstObjectByType<ArenaBounds>() == null) throw new InvalidOperationException("No ArenaBounds in scene");
        }

        private static EnemyConfig LoadOrCreateBatConfig()
        {
            string path = ConfigFolder + "/Bat.asset";
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (config == null)
            {
                config = EnemyConfig.Create(CombatSetup.LoadOrCreateHealth("BatHealth", 3, 0f));
                if (!AssetDatabase.IsValidFolder("Assets/Config")) AssetDatabase.CreateFolder("Assets", "Config");
                if (!AssetDatabase.IsValidFolder(ConfigFolder)) AssetDatabase.CreateFolder("Assets/Config", "Enemies");
                AssetDatabase.CreateAsset(config, path);
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

        private static T Ensure<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }
    }
}
