using UnityEngine;
namespace RogueSurvivors
{
    public sealed class BuildGuideUI : MonoBehaviour
    {
        GameObject panel;
        public void Open()
        {
            if (panel) { panel.SetActive(true); panel.transform.SetAsLastSibling(); return; }
            panel = UIFactory.Panel("組み合わせガイド", transform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(1200,640), new Color(.035f,.05f,.1f,1)).gameObject;
            UIFactory.Label("見出し", panel.transform, new Vector2(.5f,1), new Vector2(0,-28), new Vector2(950,52), "属性反応と物理武器の進化", 30, UIFactory.Cyan, TextAnchor.MiddleCenter);
            UIFactory.Label("属性", panel.transform, new Vector2(0,1), new Vector2(35,-110), new Vector2(550,470),
                "属性武器：風の弓・火の玉・雷・氷／水／光／闇／木／土の杖\n\n炎 ＋ 風 → 大爆発（周囲にもダメージ）\n炎 ＋ 雷 → 過負荷（追加ダメージ）\n炎 ＋ 氷 → 融解（高い単体ダメージ）\n炎 ＋ 水 → 蒸発（単体ダメージ増幅）\n水 ＋ 雷 → 感電（周囲へ放電）\n水 ＋ 氷 → 凍結（敵の動きを止める）\n光 ＋ 闇 → 対消滅（周囲にもダメージ）\n木＋水→開花 ／ 木＋炎→燃焼 ／ 木＋雷→激化\n土＋炎・水・氷・雷→結晶（最大HP20%の盾・5秒）\n雷＋氷→超電導（5秒間、物理ダメージ1.25倍）\n風＋水・氷・雷→拡散（属性を周囲に付着）\n\nどちらを先に当てても発動。付着は4秒。\n反応は最短0.4秒、ボスは共有0.75秒間隔。\nボスは凍結せず、移動速度が少し下がります。", 16, Color.white);
            UIFactory.Label("物理", panel.transform, new Vector2(0,1), new Vector2(620,-110), new Vector2(550,440),
                "物理9種・属性9種。武器6枠、属性は最大4つ。\n物理武器は両方Lv4になると左側の武器が進化\n\n剣 ＋ 盾 → 大剣\n回転剣 ＋ 斧 → 回転大斧\n短剣 ＋ ナックル → 双剣\n槍 ＋ 剣 → ハルバード\n斧 ＋ ハンマー → 両刃の戦斧\nハンマー ＋ 槍 → ウォーハンマー\n盾 ＋ ハンマー → スパイクシールド\nナックル ＋ 短剣 → クロー\n鎌 ＋ 回転剣 → 大鎌\n\n進化後は威力1.5倍、射程・範囲も強化。\n物理はボス装甲へのダメージ1.35倍。\n近接職は物理、遠距離職は属性が出やすい。\n死亡で条件を失うと進化前に戻ります。", 18, Color.white);
            UIFactory.Button("閉じる", panel.transform, new Vector2(.5f,0), new Vector2(0,20), new Vector2(240,44), "閉じる", () => panel.SetActive(false));
        }
    }
}

