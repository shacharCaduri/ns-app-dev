using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using WizardArena.Combat;
using WizardArena.Player;
using WizardArena.Stage;
using WizardArena.UI;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Creates the UI Toolkit HUD and screens overlay (once; the PanelSettings asset persists
    // across rebuilds) and wires them to the wizard's Health and the scene's GameSession.
    // Runs after StageSetup.Bind, which creates the GameSession this needs.
    internal static class UISetup
    {
        private const string ConfigFolder = "Assets/Config/UI";
        private const string PanelSettingsPath = ConfigFolder + "/GamePanelSettings.asset";
        private const string HudUxmlPath = "Assets/UI/Hud.uxml";
        private const string ScreensUxmlPath = "Assets/UI/Screens.uxml";
        private const string HudName = "HUD";
        private const string ScreensName = "Screens";

        internal static void Bind(Scene scene, WizardController wizard)
        {
            GameSession session = Object.FindFirstObjectByType<GameSession>();
            if (session == null) return; // StageSetup runs first and creates it

            PanelSettings panelSettings = LoadOrCreatePanelSettings();
            BindHud(scene, panelSettings, wizard);
            BindScreens(scene, panelSettings, session, wizard);
        }

        internal static void Validate(Scene scene)
        {
            if (Object.FindFirstObjectByType<HudController>() == null)
                throw new System.InvalidOperationException("No HUD in scene");
            if (Object.FindFirstObjectByType<EndScreenController>() == null)
                throw new System.InvalidOperationException("No end screen in scene");
            if (Object.FindFirstObjectByType<PauseMenuController>() == null)
                throw new System.InvalidOperationException("No pause menu in scene");
        }

        private static void BindHud(Scene scene, PanelSettings panelSettings, WizardController wizard)
        {
            GameObject go = FindRoot(scene, HudName) ?? new GameObject(HudName);
            UIDocument document = Ensure<UIDocument>(go);
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            document.sortingOrder = 0;

            HudController hud = Ensure<HudController>(go);
            SerializedObject data = new SerializedObject(hud);
            data.FindProperty("playerHealth").objectReferenceValue = wizard.GetComponent<Health>();
            data.FindProperty("stageRunner").objectReferenceValue = Object.FindFirstObjectByType<StageRunner>();
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindScreens(Scene scene, PanelSettings panelSettings, GameSession session, WizardController wizard)
        {
            GameObject go = FindRoot(scene, ScreensName) ?? new GameObject(ScreensName);
            UIDocument document = Ensure<UIDocument>(go);
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScreensUxmlPath);
            document.sortingOrder = 10;

            EndScreenController endScreen = Ensure<EndScreenController>(go);
            SerializedObject endData = new SerializedObject(endScreen);
            endData.FindProperty("session").objectReferenceValue = session;
            endData.ApplyModifiedPropertiesWithoutUndo();

            PauseMenuController pause = Ensure<PauseMenuController>(go);
            SerializedObject pauseData = new SerializedObject(pause);
            pauseData.FindProperty("session").objectReferenceValue = session;
            pauseData.FindProperty("player").objectReferenceValue = wizard;
            pauseData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Ensure<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        // Internal (not private): MenuSetup reuses this so the main menu shares the same
        // PanelSettings asset as the HUD/screens instead of creating a second one.
        internal static PanelSettings LoadOrCreatePanelSettings()
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1600, 900);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            if (!AssetDatabase.IsValidFolder("Assets/Config")) AssetDatabase.CreateFolder("Assets", "Config");
            if (!AssetDatabase.IsValidFolder(ConfigFolder)) AssetDatabase.CreateFolder("Assets/Config", "UI");
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            return settings;
        }
    }
}
