using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardArena.Combat;
using WizardArena.Feedback;
using WizardArena.Player;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Creates the FeedbackConfig asset (once; tuned values are kept) and the scene's single
    // FeedbackDirector, wired to the wizard's Health and the scene camera. Runs after
    // UISetup.Bind, mirroring its pattern.
    internal static class FeedbackSetup
    {
        private const string ConfigFolder = "Assets/Config/Feedback";
        private const string ConfigPath = ConfigFolder + "/FeedbackConfig.asset";
        private const string DirectorName = "Feedback";

        internal static void Bind(Scene scene, WizardController wizard)
        {
            FeedbackConfig config = LoadOrCreateConfig();
            GameObject go = FindRoot(scene, DirectorName) ?? new GameObject(DirectorName);
            FeedbackDirector director = go.TryGetComponent(out FeedbackDirector existing) ? existing : go.AddComponent<FeedbackDirector>();

            SerializedObject data = new SerializedObject(director);
            data.FindProperty("config").objectReferenceValue = config;
            data.FindProperty("playerHealth").objectReferenceValue = wizard.GetComponent<Health>();
            data.FindProperty("targetCamera").objectReferenceValue = Object.FindFirstObjectByType<Camera>();
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Validate(Scene scene)
        {
            if (Object.FindFirstObjectByType<FeedbackDirector>() == null)
                throw new InvalidOperationException("No FeedbackDirector in scene");
        }

        private static FeedbackConfig LoadOrCreateConfig()
        {
            FeedbackConfig config = AssetDatabase.LoadAssetAtPath<FeedbackConfig>(ConfigPath);
            if (config != null) return config;

            config = FeedbackConfig.Create(true);
            EnsureFolder("Assets", "Config");
            EnsureFolder("Assets/Config", "Feedback");
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
