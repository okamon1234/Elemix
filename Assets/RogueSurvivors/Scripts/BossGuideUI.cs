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
                "右腕先：突進・地震。頭先：なぎ払い。左腕先：衝撃環。脚先：定点粉砕。\n突進は横へ、衝撃環は切れ目へ。HP50%から第3形態の攻撃が加わる。",
                "右脚先：毒の追跡。頭先：巣の包囲。左脚先：針射。腹先：毒針・捕縛。\n毒針は移動し続け、縮む巣は出口へ。HP50%から連続する包囲攻撃。",
                "右翼先：旋回ブレス。頭先：雷嵐。左翼先：落雷の列。尾先：旋回弾幕。\nブレスは背後へ、落雷は立ち止まらない。HP50%から嵐の追撃が加わる。",
                "右枝先：根の迷路。樹冠先：回転根。左枝先：収穫の輪。根先：回転・収穫。\n回転根と同じ方向へ、収穫は輪の内側へ。HP50%から追撃が増える。"
            };
            for(int k=0;k<4;k++) {
                float y=-85-k*106;
                for(int phase=0;phase<3;phase++) { var icon=UIFactory.Panel("形態"+k+phase,panel.transform,Vector2.up,new Vector2(22+phase*62,y),new Vector2(60,78),Color.white); icon.sprite=BossAppearance.GetSprite((BossKind)k,phase+1); icon.preserveAspect=true; icon.raycastTarget=false; }
                UIFactory.Label("名前"+k,panel.transform,Vector2.up,new Vector2(210,y),new Vector2(330,28),BossCatalog.Names[k],21,BossCatalog.Colors[k]);
                UIFactory.Label("攻略"+k,panel.transform,Vector2.up,new Vector2(210,y-30),new Vector2(940,64),descriptions[k],18);
            }
            UIFactory.Label("共通ルール",panel.transform,Vector2.up,new Vector2(30,-516),new Vector2(1130,80),
                "Q／RB・部位ボタンで8秒間ピン共有。E／LBで解除。破壊順で攻撃が分岐し、最初の破壊で第2形態・HP50%で第3形態。\n破壊時1.1秒ひるみ→破壊側3.6m以内に4秒の弱点。物理1.6倍／属性1.3倍、追加分は本体HP3.5%が上限。変身中は時間停止。\n破壊ごとに攻撃力＋7.5%・頻度＋8%。全破壊で通常の本体攻撃も有効。回復：HP45%以下で6秒無傷→18%回復、1戦3回。",15);
            UIFactory.Button("閉じる",panel.transform,new Vector2(.5f,0),new Vector2(0,14),new Vector2(240,38),"閉じる",()=>panel.SetActive(false));
        }
    }
}
