using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizardArena.Enemies;
using WizardArena.Player;
using WizardArena.World;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Single entry point for generating the demo scene, from the menu or headless.
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/WizardMovement.unity";

        // Scene, walkable surfaces, combat assets and build settings, then saved.
        [MenuItem("Wizard Prototype/Rebuild Demo Scene")]
        public static void Rebuild()
        {
            WizardPrototypeSetup.BuildScene();
            WizardPrototypeSetup.UpdateCombatAssets();
            AssetDatabase.SaveAssets();
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
            if (Object.FindFirstObjectByType<WizardController>() == null) throw new InvalidOperationException("No wizard in scene");
            if (Object.FindFirstObjectByType<BatEnemyController>() == null) throw new InvalidOperationException("No bat in scene");
            if (Object.FindFirstObjectByType<ArenaSurface>() == null) throw new InvalidOperationException("No arena surfaces in scene");
        }
    }
}
