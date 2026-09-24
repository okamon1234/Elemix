using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
namespace RogueSurvivors.Editor
{
    [InitializeOnLoad]
    public static class FusionIntegrationSetup
    {
        public static void InitializeSdk()
        {
#if ROGUE_FUSION
            PlayerSettings.allowUnsafeCode = true;
            Fusion.Editor.FusionGlobalScriptableObjectUtils.EnsureAssetExists<Fusion.Photon.Realtime.PhotonAppSettings>();
            Fusion.Editor.FusionGlobalScriptableObjectUtils.EnsureAssetExists<Fusion.NetworkProjectConfigAsset>();
            if (!string.IsNullOrWhiteSpace(NetworkManager.FusionAppId)) Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdFusion = NetworkManager.FusionAppId;
            EditorUtility.SetDirty(Fusion.Photon.Realtime.PhotonAppSettings.Global);
            AssetDatabase.SaveAssets();
#endif
            DetectSdk();
        }
        static FusionIntegrationSetup() => EditorApplication.delayCall += DetectSdk;
        public static void DetectSdk()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool installed = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType("Fusion.NetworkRunner", false) != null);
            var target = NamedBuildTarget.Standalone;
            var symbols = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            bool changed = installed ? !symbols.Contains("ROGUE_FUSION") : symbols.Contains("ROGUE_FUSION");
            if (installed) changed |= symbols.RemoveAll(s => s == "PHOTON_UNITY_NETWORKING" || s.StartsWith("PUN_")) > 0;
            if (!changed) return;
            if (installed) symbols.Add("ROGUE_FUSION"); else symbols.Remove("ROGUE_FUSION");
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
        }
    }
}

