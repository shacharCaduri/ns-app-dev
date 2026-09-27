using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WizardArena.EditorTools
{
    // Headless visual check: renders Camera.main to a PNG so agents can eyeball the scene
    // layout without entering Play Mode. UI Toolkit overlays (HUD, screens) are not captured --
    // there is no camera involved in rendering them, so this is a world/art check only.
    public static class SceneScreenshot
    {
        private const int Width = 1600;
        private const int Height = 900;

        // Unity -batchmode -quit -executeMethod WizardArena.EditorTools.SceneScreenshot.CaptureFromCommandLine
        public static void CaptureFromCommandLine()
        {
            try
            {
                Capture("Logs/screenshot.png");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static void Capture(string outputPathRelativeToProject)
        {
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("No main camera in scene");

            RenderTexture renderTexture = new RenderTexture(Width, Height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                image.Apply();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(renderTexture);
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.Combine(projectRoot, outputPathRelativeToProject);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Debug.Log("[SceneScreenshot] Saved " + fullPath);
        }
    }
}
