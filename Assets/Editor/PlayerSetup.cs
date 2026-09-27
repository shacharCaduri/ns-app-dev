using UnityEditor;
using UnityEngine;
using WizardArena.Player;
using WizardArena.UI;

namespace WizardArena.EditorTools
{
    // Creates the player config asset (once; later tuning is kept), points every
    // player part at it, and adds the temporary HUD.
    internal static class PlayerSetup
    {
        private const string ConfigFolder = "Assets/Config/Player";
        private const string ConfigPath = ConfigFolder + "/WizardPlayer.asset";

        internal static void BindConfig(GameObject wizard)
        {
            PlayerConfig config = LoadOrCreateConfig();
            AssignConfig(wizard.GetComponent<PlayerMotor>(), config);
            AssignConfig(wizard.GetComponent<PlayerCaster>(), config);
            AssignConfig(wizard.GetComponent<PlayerAnimator>(), config);
            AssignConfig(wizard.GetComponent<PlayerHitFeedback>(), config);
            AssignConfig(wizard.GetComponent<PlayerTransitions>(), config);
        }

        // [09] replaces this with the real UI.
        internal static void CreateLegacyHud(WizardController player)
        {
            GameObject hud = new GameObject("Legacy HUD");
            SerializedObject data = new SerializedObject(hud.AddComponent<LegacyHud>());
            data.FindProperty("player").objectReferenceValue = player;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignConfig(Component part, PlayerConfig config)
        {
            SerializedObject data = new SerializedObject(part);
            data.FindProperty("config").objectReferenceValue = config;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerConfig LoadOrCreateConfig()
        {
            PlayerConfig config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath);
            if (config != null) return config;
            if (!AssetDatabase.IsValidFolder("Assets/Config")) AssetDatabase.CreateFolder("Assets", "Config");
            if (!AssetDatabase.IsValidFolder(ConfigFolder)) AssetDatabase.CreateFolder("Assets/Config", "Player");
            config = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }
    }
}
