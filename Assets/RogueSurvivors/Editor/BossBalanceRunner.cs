using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    [InitializeOnLoad] public static class BossBalanceRunner
    {
        static BossBalanceRunner(){EditorApplication.playModeStateChanged+=Changed;}
        [MenuItem("Tools/Rogue Survivors/Run Boss Balance Comparison")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("比較前にシーンを保存してください。");
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/RogueBossBalanceResult.json","{\"running\":true}");
            SessionState.SetBool("RogueBossBalanceRunning",true);EditorSceneManager.OpenScene("Assets/RogueSurvivors/Scenes/HomeScene.unity");EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool("RogueBossBalanceRunning",false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("ボス実戦バランス比較").AddComponent<BossBalanceProbe>();
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("RogueBossBalanceRunning",false);if(Application.isBatchMode)EditorApplication.Exit(0);}
        }
    }
}
