using UnityEngine;
namespace RogueSurvivors
{
    public static class NetworkConfiguration
    {
        [System.Serializable] sealed class Settings { public string fusionAppId; }
        public static string AppId
        {
            get {
                var file=Resources.Load<TextAsset>("RogueSurvivors/LocalNetworkSettings");
                if(!file) return "";
                try { return JsonUtility.FromJson<Settings>(file.text)?.fusionAppId ?? ""; }
                catch { Debug.LogWarning("個別通信設定を読み込めません。Photonの設定を確認してください。"); return ""; }
            }
        }
    }
}
