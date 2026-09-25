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
                "属性は武器名の先頭：炎・水・氷・雷・風・木・土・光・闇\n\n炎 ＋ 風 → 拡散（炎が残り、周囲にも付着）\n炎 ＋ 雷 → 過負荷（追加ダメージ）\n炎 ＋ 氷 → 融解（高い単体ダメージ）\n炎 ＋ 水 → 蒸発（単体ダメージ増幅）\n水 ＋ 雷 → 感電（周囲へ放電）\n水 ＋ 氷 → 凍結（敵の動きを止める）\n光 ＋ 闇 → 対消滅（周囲にもダメージ）\n木＋水→開花（3秒残る種。敵ごとに1個）\n種の付いた敵＋雷→超開花（単体へ追撃）\n種の付いた敵＋炎→烈開花（広い範囲に爆発）\n木＋炎→燃焼 ／ 木＋雷→激化\n土＋炎・水・氷・雷→結晶（一撃無効のアイテム・拾って保持1個）\n雷＋氷→超電導（5秒間、物理ダメージ1.25倍）\n風＋炎・水・氷・雷→拡散（元の敵にも属性が残る）\n\n2属性はどちらが先でも発動。付着は4秒。\n反応は最短0.4秒、ボスは共有0.75秒間隔。\nボスは凍結せず、移動速度が少し下がります。", 16, Color.white);
            UIFactory.Label("物理", panel.transform, new Vector2(0,1), new Vector2(620,-110), new Vector2(550,440),
                "物理12種・属性9種。武器6枠、属性は最大4つ。\n両方Lv4で2本が合体 → 1本になり1枠空く\n\n剣 ＋ 盾 → 大剣（二連なぎ払い）\n回転剣 ＋ 斧 → 回転大斧（広がる斧）\n短剣 ＋ ナックル → 投刃グローブ（連射）\n槍 ＋ 剣 → ハルバード（なぎ払い＋突き）\n斧 ＋ ハンマー → 投擲戦斧（巨大な投げ斧）\nハンマー ＋ 槍 → 鎖鉄球（鎖付き鉄球）\n盾 ＋ ハンマー → スパイクシールド（跳弾）\n鎌 ＋ 回転剣 → 円盤鎌（飛んで戻る刃）\n鞭 ＋ 鎌 → 鎖鎌（長い鎖と鎌）\nボウガン ＋ ブーメラン → 連射ボウガン\n\n合体は最大5本＋通常1本。全物理武器に合体先。\n素材の攻撃は停止。合体後は独立したLv1〜8。\n同じ素材の使い回しは不可。先に成立した合体を保持。\n物理は装甲へ1.35倍。遠距離の投擲武器も物理扱い。\n進化相手は抽選重み3倍（Lv4で4倍）。\n死亡時は直近3強化を戻し、合体前なら素材2本へ戻る。\n光＋闇の同時進化は別の仕組み（2枠のまま）。", 16, Color.white);
            UIFactory.Button("閉じる", panel.transform, new Vector2(.5f,0), new Vector2(0,20), new Vector2(240,44), "閉じる", () => panel.SetActive(false));
        }
    }
}
