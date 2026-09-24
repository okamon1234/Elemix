using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    [InitializeOnLoad]
    public static class RogueReloadTest
    {
        const string Key = "RogueReloadPhase";
        static double nextStep;
        static RogueReloadTest()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RogueReloadBatchExit", false)) {
                    SessionState.SetBool("RogueReloadBatchExit", false);
                    EditorApplication.Exit(SessionState.GetBool("RogueReloadSuccess", false) ? 0 : 1);
                }
            };
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, trace, type) => {
                if (SessionState.GetInt(Key, 0) > 0 && (type == LogType.Exception || type == LogType.Error))
                    SessionState.SetString("RogueReloadError", message + "\n" + trace);
            };
            nextStep = EditorApplication.timeSinceStartup + 1;
        }
        [MenuItem("Tools/Rogue Survivors/Test Script Reload")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the scene first.");
            SessionState.SetInt(Key, 1); SessionState.SetString("RogueReloadError", "");
            EditorSceneManager.OpenScene("Assets/RogueSurvivors/Scenes/SoloScene.unity");
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            int phase = SessionState.GetInt(Key, 0);
            if (phase == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < nextStep) return;
            nextStep = EditorApplication.timeSinceStartup + 1;
            try
            {
                string error = SessionState.GetString("RogueReloadError", "");
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                if (!PlayerHealth.Local || !GameManager.Instance || !HUDController.Instance || !EffectsService.Instance || !CameraFollow.Instance || !NetworkManager.Instance)
                    throw new InvalidOperationException("A singleton was not restored after script reload.");
                var stats = PlayerHealth.Local.GetComponent<PlayerStats>();
                if (phase == 1)
                {
                    UnityEngine.Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
                    stats.AddExperience(8); SessionState.SetInt(Key, 2); return;
                }
                if (phase == 2)
                {
                    if (Time.timeScale != 0 || !GameObject.Find("Card0")) throw new InvalidOperationException("Upgrade UI did not pause before reload.");
                    SessionState.SetInt(Key, 3); EditorUtility.RequestScriptReload(); return;
                }
                if (phase == 3)
                {
                    if (stats.Level != 2 || !GameManager.Instance.IsPlaying || !GameObject.Find("Card0")) throw new InvalidOperationException("Run state or cards were lost after reload.");
                    PlayerHealth.Local.GetComponent<LevelUpManager>().Choose(0);
                    if (Time.timeScale != 1) throw new InvalidOperationException("Upgrade choice could not resume after reload.");
                    stats.AddExperience(stats.RequiredExperience - stats.Experience); SessionState.SetInt(Key, 4); return;
                }
                if (phase == 4)
                {
                    if (Time.timeScale != 0 || stats.Level != 3) throw new InvalidOperationException("Level up subscription was lost after reload.");
                    PlayerHealth.Local.GetComponent<LevelUpManager>().Choose(0);
                    Complete(true, "Singletons, level, upgrade cards, pause/resume and subsequent level-up survive an actual script domain reload.");
                }
            }
            catch (Exception ex) { Complete(false, ex.ToString()); }
        }
        static void Complete(bool success, string message)
        {
            SessionState.SetInt(Key, 0);
            SessionState.SetBool("RogueReloadSuccess", success);
            SessionState.SetBool("RogueReloadBatchExit", Application.isBatchMode);
            File.WriteAllText("Logs/RogueReloadResult.json", JsonUtility.ToJson(new Result { success = success, message = message }, true));
            Debug.Log("ROGUE_RELOAD_TEST: " + success + " " + message);
            Time.timeScale = 1; EditorApplication.isPlaying = false;
        }
        [Serializable] sealed class Result { public bool success; public string message; }
    }
}
