using UnityEngine;
namespace RogueSurvivors
{
    public enum SoloMapKind { Meadow, Ruins }
    public static class SoloMapCatalog
    {
        public static SoloMapKind Selected => (SoloMapKind)Mathf.Clamp(PlayerPrefs.GetInt("SoloMap",0),0,1);
        public static void Select(SoloMapKind kind){PlayerPrefs.SetInt("SoloMap",Mathf.Clamp((int)kind,0,1));PlayerPrefs.Save();}
        public static string Name(SoloMapKind kind)=>kind==SoloMapKind.Meadow?"草原":"遺跡";
        public static string Description(SoloMapKind kind)=>kind==SoloMapKind.Meadow?
            "開けた野原・林の縁・岩場\n広い道で群れを引き離し、木や岩を回って逃げる。\n草むらは通過でき、木・岩・倒木は障害物。":
            "中央広場・石柱の回廊・崩れた外壁\n通路へ敵を誘い、広場で切り返す。\n石柱や壁は弾も遮る。アーチの中央は通過できる。";
        public static string District(SoloMapKind kind,int area)=>kind==SoloMapKind.Meadow?
            new[]{"開けた野原","林の縁","岩場"}[Mathf.Clamp(area,0,2)]:new[]{"中央広場","石柱の回廊","崩れた外壁"}[Mathf.Clamp(area,0,2)];
    }
}
