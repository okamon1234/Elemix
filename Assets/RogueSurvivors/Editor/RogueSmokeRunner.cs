using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    [InitializeOnLoad]
    public static class RogueSmokeRunner
    {
        static RogueSmokeRunner()
        {
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Watchdog;
        }
        [MenuItem("Tools/Rogue Survivors/Run Play Mode Smoke Tests")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save the scene before running smoke tests.");
            RogueProjectSetup.Validate();
            if (File.Exists("Logs/RogueSmokeResult.json")) File.Delete("Logs/RogueSmokeResult.json");
            SessionState.SetBool("RogueSmokeRunning", true);
            SessionState.SetString("RogueSmokeStarted", System.DateTime.UtcNow.ToString("O"));
            EditorSceneManager.OpenScene("Assets/RogueSurvivors/Scenes/SoloScene.unity");
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool("RogueSmokeRunning", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                new GameObject("Rogue Smoke Tests").AddComponent<RogueSmokeTests>();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("RogueSmokeRunning", false);
                string report = File.Exists("Logs/RogueSmokeResult.json") ? File.ReadAllText("Logs/RogueSmokeResult.json") : "No test report produced.";
                Debug.Log("ROGUE_SMOKE_RESULT: " + report);
                if (Application.isBatchMode) EditorApplication.Exit(report.Contains("\"success\": true") ? 0 : 1);
            }
        }
        static void Watchdog()
        {
            if (!SessionState.GetBool("RogueSmokeRunning", false)) return;
            if (!System.DateTime.TryParse(SessionState.GetString("RogueSmokeStarted", ""), out var started)) return;
            if ((System.DateTime.UtcNow - started.ToUniversalTime()).TotalSeconds < 180) return;
            File.WriteAllText("Logs/RogueSmokeResult.json", "{\"success\": false, \"error\": \"Play Mode test timed out.\"}");
            EditorApplication.isPlaying = false;
        }
    }
}
