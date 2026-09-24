using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    public static class RogueBuild
    {
        [MenuItem("Tools/Rogue Survivors/Build Windows Player")]
        public static void Build()
        {
            BossExpansionSetup.Apply();
            Directory.CreateDirectory("Builds/RiftSurvivors");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled && s.path.Contains("RogueSurvivors/Scenes/")).Select(s => s.path).ToArray(),
                locationPathName = "Builds/RiftSurvivors/RiftSurvivors.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            File.WriteAllText("Logs/RogueBuildResult.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("ROGUE_BUILD_OK: Builds/RiftSurvivors/RiftSurvivors.exe");
        }
    }
}
