using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardArena.Combat;
using WizardArena.Player;
using WizardArena.Stage;
using WizardArena.UI;

namespace WizardArena.EditorTools
{
    // Creates the stage-flow objects (once; later tuning in the config asset is kept) and
    // wires GameSession, StageManager and StagePortal to the wizard, the exit portal already
    // built by WizardPrototypeSetup, and the temporary HUD.
    internal static class StageSetup
    {
        private const string ConfigFolder = "Assets/Config/Stage";
        private const string ConfigPath = ConfigFolder + "/StagePortal.asset";
        private const string PortalName = "Stage Exit Portal";
        private const string SessionName = "Game Session";

        internal static void Bind(Scene scene, WizardController wizard)
        {
            GameObject portalObject = FindRoot(scene, PortalName);
            if (portalObject == null) return; // built by WizardPrototypeSetup first

            GameSession session = FindOrCreateSession(scene);
            StagePortal portal = BindPortal(portalObject, wizard, session);
            BindManager(session, portal, wizard);
            BindHud(scene, session);
        }

        internal static void Validate(Scene scene)
        {
            if (Object.FindFirstObjectByType<GameSession>() == null)
                throw new System.InvalidOperationException("No GameSession in scene");
            if (Object.FindFirstObjectByType<StageManager>() == null)
                throw new System.InvalidOperationException("No StageManager in scene");
            if (Object.FindFirstObjectByType<StagePortal>() == null)
                throw new System.InvalidOperationException("No StagePortal in scene");
        }

        private static GameSession FindOrCreateSession(Scene scene)
        {
            GameObject go = FindRoot(scene, SessionName);
            if (go == null) go = new GameObject(SessionName);
            GameSession session = go.GetComponent<GameSession>();
            if (session == null) session = go.AddComponent<GameSession>();
            return session;
        }

        private static StagePortal BindPortal(GameObject portalObject, WizardController wizard, GameSession session)
        {
            StagePortal portal = portalObject.GetComponent<StagePortal>();
            if (portal == null) portal = portalObject.AddComponent<StagePortal>();
            StagePortalConfig config = LoadOrCreateConfig();
            SerializedObject data = new SerializedObject(portal);
            data.FindProperty("config").objectReferenceValue = config;
            data.FindProperty("player").objectReferenceValue = wizard;
            data.FindProperty("session").objectReferenceValue = session;
            data.ApplyModifiedPropertiesWithoutUndo();
            return portal;
        }

        private static void BindManager(GameSession session, StagePortal portal, WizardController wizard)
        {
            GameObject go = session.gameObject;
            StageManager manager = go.GetComponent<StageManager>();
            if (manager == null) manager = go.AddComponent<StageManager>();
            SerializedObject data = new SerializedObject(manager);
            data.FindProperty("session").objectReferenceValue = session;
            data.FindProperty("portal").objectReferenceValue = portal;
            data.FindProperty("playerHealth").objectReferenceValue = wizard.GetComponent<Health>();
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindHud(Scene scene, GameSession session)
        {
            GameObject hudObject = FindRoot(scene, "Legacy HUD");
            if (hudObject == null) return;
            LegacyHud hud = hudObject.GetComponent<LegacyHud>();
            if (hud == null) return;
            SerializedObject data = new SerializedObject(hud);
            data.FindProperty("session").objectReferenceValue = session;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static StagePortalConfig LoadOrCreateConfig()
        {
            StagePortalConfig config = AssetDatabase.LoadAssetAtPath<StagePortalConfig>(ConfigPath);
            if (config != null) return config;
            config = StagePortalConfig.Create(0.8f, 0.7f, new Vector2(0.65f, 0.5f));
            if (!AssetDatabase.IsValidFolder("Assets/Config")) AssetDatabase.CreateFolder("Assets", "Config");
            if (!AssetDatabase.IsValidFolder(ConfigFolder)) AssetDatabase.CreateFolder("Assets/Config", "Stage");
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }
    }
}
