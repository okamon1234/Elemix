using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    [InitializeOnLoad] public static class CombatBalanceRunner
    {
        static CombatBalanceRunner() { EditorApplication.playModeStateChanged+=Changed; }
        [MenuItem("Tools/Rogue Survivors/Run Combat Balance Comparison")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("比較前にシーンを保存してください。");
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/RogueBalanceResult.json","{\"running\":true}");
            SessionState.SetBool("RogueBalanceRunning",true); EditorSceneManager.OpenScene("Assets/RogueSurvivors/Scenes/SoloScene.unity"); EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool("RogueBalanceRunning",false)) return;
            if(state==PlayModeStateChange.EnteredPlayMode) new GameObject("育成速度の実プレイ比較").AddComponent<CombatBalanceProbe>();
            if(state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("RogueBalanceRunning",false); if(Application.isBatchMode) EditorApplication.Exit(0); }
        }
    }
}
