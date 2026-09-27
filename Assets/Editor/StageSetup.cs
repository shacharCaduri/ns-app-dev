using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardArena.Combat;
using WizardArena.Player;
using WizardArena.Stage;

namespace WizardArena.EditorTools
{
    // Creates the stage-flow objects (once; later tuning in config assets is kept) and wires
    // GameSession, StageManager, StagePortal and StageRunner to the wizard, the exit portal
    // already built by WizardPrototypeSetup, and the three-stage sequence.
    internal static class StageSetup
    {
        private const string ConfigFolder = "Assets/Config/Stage";
        private const string StagesFolder = ConfigFolder + "/Stages";
        private const string PortalConfigPath = ConfigFolder + "/StagePortal.asset";
        private const string SequencePath = ConfigFolder + "/StageSequence.asset";
        private const string PortalName = "Stage Exit Portal";
        private const string SessionName = "Game Session";
        // Shared by every stage for now: the wizard always starts where the scene builder
        // puts it. Kept as a constant so the initial (non-StageRunner-driven) scene state and
        // stage 1's data agree.
        private static readonly Vector3 PlayerSpawn = new Vector3(0f, -2.4f, 0f);

        internal static void Bind(Scene scene, WizardController wizard)
        {
            GameObject portalObject = FindRoot(scene, PortalName);
            if (portalObject == null) return; // built by WizardPrototypeSetup first

            GameSession session = FindOrCreateSession(scene);
            StagePortal portal = BindPortal(portalObject, wizard, session);
            BindManager(session, portal, wizard);
            BindRunner(session, portal, wizard);
        }

        internal static void Validate(Scene scene)
        {
            if (Object.FindFirstObjectByType<GameSession>() == null)
                throw new System.InvalidOperationException("No GameSession in scene");
            if (Object.FindFirstObjectByType<StageManager>() == null)
                throw new System.InvalidOperationException("No StageManager in scene");
            if (Object.FindFirstObjectByType<StagePortal>() == null)
                throw new System.InvalidOperationException("No StagePortal in scene");
            StageRunner runner = Object.FindFirstObjectByType<StageRunner>();
            if (runner == null) throw new System.InvalidOperationException("No StageRunner in scene");
            if (runner.Sequence == null || runner.Sequence.Stages.Count < 3)
                throw new System.InvalidOperationException("StageSequence needs at least 3 stages");
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
            StagePortalConfig config = LoadOrCreatePortalConfig();
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

        private static void BindRunner(GameSession session, StagePortal portal, WizardController wizard)
        {
            GameObject go = session.gameObject;
            StageRunner runner = go.GetComponent<StageRunner>();
            if (runner == null) runner = go.AddComponent<StageRunner>();
            SerializedObject data = new SerializedObject(runner);
            data.FindProperty("sequence").objectReferenceValue = LoadOrCreateSequence();
            data.FindProperty("player").objectReferenceValue = wizard;
            data.FindProperty("portal").objectReferenceValue = portal;
            data.FindProperty("session").objectReferenceValue = session;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static StagePortalConfig LoadOrCreatePortalConfig()
        {
            StagePortalConfig config = AssetDatabase.LoadAssetAtPath<StagePortalConfig>(PortalConfigPath);
            if (config != null) return config;
            config = StagePortalConfig.Create(0.8f, 0.7f, new Vector2(0.65f, 0.5f));
            EnsureFolders(ConfigFolder);
            AssetDatabase.CreateAsset(config, PortalConfigPath);
            return config;
        }

        // Three stages of growing difficulty: stage 1 is bats only; [11] added the blue slime
        // and skeleton warrior to stages 2-3, trading some bats for them so the total enemy
        // count per stage is unchanged. The StageSequence and its three StageDefinition
        // assets are built once (Create/CreateAsset only run when missing), but stages 2/3's
        // enemySpawns are re-applied on every call via SetEnemySpawns so the composition
        // below is always what ends up in the assets -- idempotent, and it deliberately
        // updates the pre-[11] assets rather than only appending to whatever is there.
        private static StageSequence LoadOrCreateSequence()
        {
            GameObject bat = EnemySetup.LoadOrCreateBatPrefab();
            GameObject batSwift = EnemySetup.LoadOrCreateBatSwiftPrefab();
            GameObject slime = GroundEnemySetup.LoadOrCreateSlimePrefab();
            GameObject skeleton = GroundEnemySetup.LoadOrCreateSkeletonPrefab();

            StageDefinition ruinsAntechamber = LoadOrCreateStage("RuinsAntechamber", "Ruins Antechamber",
                PlayerSpawn, new Vector3(5.5f, -2.4f, 0f),
                new[] { EnemySpawn.At(bat, new Vector3(4f, 1.5f, 0f)) });

            StageDefinition mossyBridge = LoadOrCreateStage("MossyBridge", "Mossy Bridge",
                PlayerSpawn, new Vector3(6.5f, -2.4f, 0f),
                new[]
                {
                    EnemySpawn.At(batSwift, new Vector3(-4f, 0.5f, 0f)),
                    EnemySpawn.At(batSwift, new Vector3(0f, 2.6f, 0f)),
                    EnemySpawn.At(batSwift, new Vector3(4f, 0.5f, 0f)),
                });
            SetEnemySpawns(mossyBridge, new[]
            {
                EnemySpawn.At(batSwift, new Vector3(-4f, 0.5f, 0f)),
                EnemySpawn.At(batSwift, new Vector3(4f, 0.5f, 0f)),
                // On the floor, clear of the stairs on either side and of the wizard's spawn.
                EnemySpawn.At(slime, new Vector3(2.8f, -2.4f, 0f)),
            });

            StageDefinition portalSanctum = LoadOrCreateStage("PortalSanctum", "Portal Sanctum",
                PlayerSpawn, new Vector3(5.5f, -2.4f, 0f),
                new[]
                {
                    EnemySpawn.At(batSwift, new Vector3(-5f, 0.2f, 0f)),
                    EnemySpawn.At(batSwift, new Vector3(-2.5f, 2.8f, 0f)),
                    EnemySpawn.At(batSwift, new Vector3(0f, 0.8f, 0f)),
                    EnemySpawn.At(batSwift, new Vector3(2.5f, 2.8f, 0f)),
                    EnemySpawn.At(batSwift, new Vector3(5f, 0.2f, 0f)),
                });
            SetEnemySpawns(portalSanctum, new[]
            {
                EnemySpawn.At(batSwift, new Vector3(-5f, 0.2f, 0f)),
                EnemySpawn.At(batSwift, new Vector3(0f, 0.8f, 0f)),
                EnemySpawn.At(batSwift, new Vector3(5f, 0.2f, 0f)),
                // On the floor, clear of the stairs, spread out on either side of the spawn.
                EnemySpawn.At(slime, new Vector3(-2f, -2.4f, 0f)),
                EnemySpawn.At(skeleton, new Vector3(3f, -2.4f, 0f)),
            });

            StageSequence sequence = AssetDatabase.LoadAssetAtPath<StageSequence>(SequencePath);
            if (sequence != null) return sequence;

            sequence = StageSequence.Create(new[] { ruinsAntechamber, mossyBridge, portalSanctum });
            EnsureFolders(ConfigFolder);
            AssetDatabase.CreateAsset(sequence, SequencePath);
            return sequence;
        }

        // Deliberately overwrites a stage's enemySpawns to the given list (used for stages
        // whose composition [11] changed) -- idempotent, unlike LoadOrCreateStage which only
        // sets its initial spawns once and otherwise leaves an existing asset untouched.
        private static void SetEnemySpawns(StageDefinition stage, EnemySpawn[] spawns)
        {
            SerializedObject data = new SerializedObject(stage);
            SerializedProperty property = data.FindProperty("enemySpawns");
            property.arraySize = spawns.Length;
            for (int i = 0; i < spawns.Length; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("prefab").objectReferenceValue = spawns[i].Prefab;
                element.FindPropertyRelative("position").vector3Value = spawns[i].Position;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
        }

        private static StageDefinition LoadOrCreateStage(string assetName, string displayName,
            Vector3 playerSpawn, Vector3 portalPosition, EnemySpawn[] enemySpawns)
        {
            string path = StagesFolder + "/" + assetName + ".asset";
            StageDefinition stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
            if (stage != null) return stage;
            stage = StageDefinition.Create(displayName, playerSpawn, portalPosition, enemySpawns);
            EnsureFolders(StagesFolder);
            AssetDatabase.CreateAsset(stage, path);
            return stage;
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
