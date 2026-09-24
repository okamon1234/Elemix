using System;
using System.IO;
using UnityEngine;
namespace RogueSurvivors
{
    public static class BuildSaveService
    {
        public static string PathName => Path.Combine(Application.persistentDataPath, "rogue-build-v1.json");
        public static bool Save(PlayerDataData data)
        {
            if (data == null || !data.IsValid()) return false;
            try
            {
                string temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(PathName)) File.Replace(temp, PathName, PathName + ".bak");
                else File.Move(temp, PathName);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("Build save failed: " + e.Message); return false; }
        }
        public static PlayerDataData Load()
        {
            try
            {
                if (!File.Exists(PathName)) return null;
                var data = JsonUtility.FromJson<PlayerDataData>(File.ReadAllText(PathName));
                return data != null && data.IsValid() ? data : null;
            }
            catch (Exception e) { Debug.LogWarning("Build load failed: " + e.Message); return null; }
        }
    }
}
