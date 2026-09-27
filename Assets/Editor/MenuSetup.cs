using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using WizardArena.UI;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Builds the title screen: Assets/Scenes/MainMenu.unity, generated the same way as the
    // arena (WizardPrototypeSetup) -- a camera, the arena background sprite behind it, and a
    // UI Toolkit overlay (title, Play, Quit). Never hand-edit the scene; change this and
    // regenerate (Wizard Prototype > Rebuild Main Menu, or Tools/unity/unity.sh rebuild-scene).
    public static class MenuSetup
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string BackgroundPath = "Assets/Art/Environments/arena-background.png";
        private const string MenuUxmlPath = "Assets/UI/MainMenu.uxml";

        [MenuItem("Wizard Prototype/Rebuild Main Menu")]
        public static void BuildScene()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            ConfigureBackgroundTexture();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.075f, 0.12f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            CreateBackdrop();

            GameObject menu = new GameObject("Menu");
            UIDocument document = menu.AddComponent<UIDocument>();
            document.panelSettings = UISetup.LoadOrCreatePanelSettings();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MenuUxmlPath);
            document.sortingOrder = 0;
            menu.AddComponent<MainMenuController>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            SceneBuilder.SyncBuildSettings();
            AssetDatabase.SaveAssets();
        }

        // Opens the menu scene additively (so it never disturbs whatever the caller has active),
        // checks it, then closes it again.
        internal static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                if (Object.FindFirstObjectByType<MainMenuController>() == null)
                    throw new InvalidOperationException("No MainMenuController in " + ScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ConfigureBackgroundTexture()
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void CreateBackdrop()
        {
            GameObject backdrop = new GameObject("Menu Background");
            SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            // Same fit as the arena backdrop (WizardPrototypeSetup.CreateBackdrop): the floor is
            // painted at 80% of the image height, scaled to a comparable on-screen size here.
            float scale = 11.2f / renderer.sprite.bounds.size.y;
            backdrop.transform.localScale = Vector3.one * scale;
            backdrop.transform.position = new Vector3(0f, 0.96f, 0f);
            renderer.sortingOrder = -10;
        }
    }
}
