using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    [InitializeOnLoad]
    public static class RogueEditorCommands
    {
        static double nextCheck;
        static RogueEditorCommands() => EditorApplication.update += Update;
        static void Update()
        {
            if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            const string path = "Logs/RogueCommand.txt";
            if (!File.Exists(path)) return;
            string stamp = File.GetLastWriteTimeUtc(path).Ticks.ToString();
            if (SessionState.GetString("RogueCommandRefreshStamp", "") != stamp)
            {
                SessionState.SetString("RogueCommandRefreshStamp", stamp);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); nextCheck = EditorApplication.timeSinceStartup + 2; return;
            }
            string command = File.ReadAllText(path).Trim(); File.Delete(path);
            try
            {
                switch (command)
                {
                    case "Setup": RogueProjectSetup.Setup(); break;
                    case "Smoke": RogueSmokeRunner.Run(); break;
                    case "Balance": CombatBalanceRunner.Run(); break;
                    case "Build": RogueBuild.Build(); break;
                    case "ReloadTest": RogueReloadTest.Run(); break;
                    default: throw new InvalidOperationException("Unsupported Rogue command: " + command);
                }
                File.WriteAllText("Logs/RogueCommandResult.txt", command + " dispatched successfully.");
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/RogueCommandResult.txt", command + " failed: " + error);
                Debug.LogException(error);
            }
        }
    }
}
