using UnityEngine;
namespace RogueSurvivors
{
    public sealed class BossGuideUI : MonoBehaviour
    {
        GameObject panel;
        public void Open()
        {
            if(panel) { panel.SetActive(true); panel.transform.SetAsLastSibling(); return; }
            panel=UIFactory.Panel("ボス攻略",transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1190,650),new Color(.035f,.05f,.1f,1)).gameObject;
            UIFactory.Label("見出し",panel.transform,new Vector2(.5f,1),new Vector2(0,-18),new Vector2(1100,42),"討伐対象を選び、準備して挑もう",28,UIFactory.Cyan,TextAnchor.MiddleCenter);
            string[] descriptions={
                "通常：突進と衝撃波。予告線の横へ回避。\n炉心開放：三連突進・十字粉砕。突進が終わるまで足を止めない。",
                "通常：毒だまりと扇状の糸。毒の外側から攻撃。\n狩猟形態：広い蜘蛛の巣・三連針射。設置範囲から早めに離れる。",
                "通常：扇状ブレスと翼の弾幕。弾の隙間を見つける。\n嵐の翼：旋回弾幕・落雷の列。横移動で雷の列を避ける。",
                "通常：根の追撃と種の輪。根の予告を踏み続けない。\n古木の怒り：根の迷路・二重の弾幕。根の切れ目を通って位置を変える。"
            };
            for(int k=0;k<4;k++) {
                float y=-85-k*106;
                for(int phase=0;phase<2;phase++) { var icon=UIFactory.Panel("形態"+k+phase,panel.transform,Vector2.up,new Vector2(26+phase*82,y),new Vector2(78,78),Color.white); icon.sprite=BossAppearance.GetSprite((BossKind)k,phase==1); icon.preserveAspect=true; icon.raycastTarget=false; }
                UIFactory.Label("名前"+k,panel.transform,Vector2.up,new Vector2(210,y),new Vector2(330,28),BossCatalog.Names[k],21,BossCatalog.Colors[k]);
                UIFactory.Label("攻略"+k,panel.transform,Vector2.up,new Vector2(210,y-30),new Vector2(940,64),descriptions[k],18);
            }
            UIFactory.Label("共通ルール",panel.transform,Vector2.up,new Vector2(30,-516),new Vector2(1130,80),
                "四方向の装甲を壊すと本体が露出。本体HP50%で形態変化（2.4秒無敵）。\n右側破壊：威力低下／頭部：弾数減少／左側：設置時間短縮／下側：減速。\n回復：被弾後5秒で自動回復スキルが再開。応急手当はHP45%以下・6秒被弾なしで18%回復、35秒間隔・1戦3回。",16);
            UIFactory.Button("閉じる",panel.transform,new Vector2(.5f,0),new Vector2(0,14),new Vector2(240,38),"閉じる",()=>panel.SetActive(false));
        }
    }
}
