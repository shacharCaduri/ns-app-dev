using UnityEditor;
using UnityEngine;
using WizardArena.Combat;
using WizardArena.Enemies;

namespace WizardArena.EditorTools
{
    // Creates the combat config assets (once; later tuning in the assets is kept)
    // and wires Health / ProjectileLauncher components to them.
    internal static class CombatSetup
    {
        private const string ConfigFolder = "Assets/Config/Combat";
        private const string ProjectileArt = "Assets/Art/Effects/Projectiles";

        internal static void BindWizard(GameObject wizard)
        {
            HealthConfig healthConfig = LoadOrCreateHealth("WizardHealth", 10, 0.75f);
            BindHealth(wizard.GetComponent<Health>(), healthConfig, Team.Player);

            ProjectileConfig bolt = LoadOrCreateProjectile("ArcaneBolt", 1, 11f, 2f, 0.12f);
            SerializedObject boltData = new SerializedObject(bolt);
            boltData.FindProperty("spriteRight").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileArt + "/arcane_projectile_right_01.png");
            boltData.FindProperty("spriteLeft").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileArt + "/arcane_projectile_left_01.png");
            boltData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bolt);

            SerializedObject launcher = new SerializedObject(wizard.GetComponent<ProjectileLauncher>());
            launcher.FindProperty("projectile").objectReferenceValue = bolt;
            launcher.FindProperty("team").enumValueIndex = (int)Team.Player;
            launcher.ApplyModifiedPropertiesWithoutUndo();
        }

        // contactTarget: the Health the bat hurts on touch (the wizard).
        internal static void BindBat(GameObject bat, Health contactTarget)
        {
            HealthConfig healthConfig = LoadOrCreateHealth("BatHealth", 3, 0f);
            BindHealth(bat.GetComponent<Health>(), healthConfig, Team.Enemy);
            SerializedObject controller = new SerializedObject(bat.GetComponent<BatEnemyController>());
            controller.FindProperty("target").objectReferenceValue = contactTarget;
            controller.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindHealth(Health health, HealthConfig config, Team team)
        {
            SerializedObject data = new SerializedObject(health);
            data.FindProperty("config").objectReferenceValue = config;
            data.FindProperty("team").enumValueIndex = (int)team;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HealthConfig LoadOrCreateHealth(string assetName, int maxHealth, float invulnerabilitySeconds)
        {
            string path = ConfigFolder + "/" + assetName + ".asset";
            HealthConfig config = AssetDatabase.LoadAssetAtPath<HealthConfig>(path);
            if (config != null) return config;
            config = HealthConfig.Create(maxHealth, invulnerabilitySeconds);
            CreateAsset(config, path);
            return config;
        }

        private static ProjectileConfig LoadOrCreateProjectile(string assetName, int damage, float speed, float lifetime, float hitRadius)
        {
            string path = ConfigFolder + "/" + assetName + ".asset";
            ProjectileConfig config = AssetDatabase.LoadAssetAtPath<ProjectileConfig>(path);
            if (config != null) return config;
            config = ProjectileConfig.Create(damage, speed, lifetime, hitRadius);
            CreateAsset(config, path);
            return config;
        }

        private static void CreateAsset(Object asset, string path)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Config")) AssetDatabase.CreateFolder("Assets", "Config");
            if (!AssetDatabase.IsValidFolder(ConfigFolder)) AssetDatabase.CreateFolder("Assets/Config", "Combat");
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
