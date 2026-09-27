using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardArena.Combat;
using WizardArena.Player;
using WizardArena.World;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Single entry point for generating the demo scene (and the main menu), from the menu or
    // headless.
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/WizardMovement.unity";

        // Both scenes, walkable surfaces, combat assets and build settings, then saved. Menu
        // last so its own scene ends up active in-editor least often (Validate() below expects
        // the arena scene active, matching every other headless command).
        [MenuItem("Wizard Prototype/Rebuild Demo Scene")]
        public static void Rebuild()
        {
            WizardPrototypeSetup.BuildScene();
            WizardPrototypeSetup.UpdateCombatAssets();
            MenuSetup.BuildScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SyncBuildSettings();
            AssetDatabase.SaveAssets();
        }

        // Build Settings: main menu first, then the arena -- skips a scene whose file does not
        // exist yet (e.g. before either builder has run once), so callers can invoke this from
        // either builder in any order and always end up with the correct full list.
        internal static void SyncBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(MenuSetup.ScenePath)) scenes.Add(new EditorBuildSettingsScene(MenuSetup.ScenePath, true));
            if (File.Exists(ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // Unity -batchmode -quit -executeMethod WizardArena.EditorTools.SceneBuilder.RebuildFromCommandLine
        public static void RebuildFromCommandLine()
        {
            try
            {
                Rebuild();
                Validate();
                Debug.Log("[SceneBuilder] Rebuilt " + ScenePath);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Validate()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Active scene is " + scene.path);
            if (scene.isDirty) throw new InvalidOperationException("Scene was left unsaved");
            WizardController wizard = Object.FindFirstObjectByType<WizardController>();
            if (wizard == null) throw new InvalidOperationException("No wizard in scene");
            if (wizard.GetComponent<IPlayerInput>() == null) throw new InvalidOperationException("Wizard has no player input");
            if (wizard.GetComponent<ProjectileLauncher>() == null) throw new InvalidOperationException("Wizard has no ProjectileLauncher");
            if (wizard.GetComponent<Health>() == null) throw new InvalidOperationException("Wizard has no Health");
            if (Object.FindFirstObjectByType<ArenaSurface>() == null) throw new InvalidOperationException("No arena surfaces in scene");
            EnemySetup.Validate();
            StageSetup.Validate(scene);
            UISetup.Validate(scene);
            FeedbackSetup.Validate(scene);
            MenuSetup.Validate();
        }
    }
}
