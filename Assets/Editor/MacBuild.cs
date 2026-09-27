using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WizardArena.EditorTools
{
    // Headless macOS player build (Tools/unity/unity.sh build-macos -> Builds/macOS/WizardArena.app,
    // git-ignored). The Standalone scripting backend is Mono, not IL2CPP: IL2CPP is not
    // installed on this machine (ProjectSettings.asset carries the same choice; re-asserted
    // here too so a build from the command line never depends on the Editor's cached setting).
    public static class MacBuild
    {
        public const string OutputPath = "Builds/macOS/WizardArena.app";

        // Unity -batchmode -quit -executeMethod WizardArena.EditorTools.MacBuild.BuildFromCommandLine
        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static void Build()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No scenes in Build Settings");

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(projectRoot, OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    "Build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors)");

            Debug.Log("[MacBuild] Built " + outputPath);
        }
    }
}
