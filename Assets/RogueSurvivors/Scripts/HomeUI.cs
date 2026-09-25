using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class HomeUI : MonoBehaviour
    {
        Text selection;
        Button mageChoice;
        Text preparation;
        Text bossSelection;
        readonly Button[] cards = new Button[CharacterCatalog.All.Length];
        void Start()
        {
            GetComponentInParent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var background = UIFactory.Panel("背景", transform, Vector2.zero, Vector2.zero, Vector2.zero, UIFactory.Ink);
            background.rectTransform.anchorMax = Vector2.one;
            UIFactory.Label("題名", transform, new Vector2(.5f, 1), new Vector2(0, -35), new Vector2(1000, 78), "サバイバーズ", 46, UIFactory.Cyan, TextAnchor.MiddleCenter);
            UIFactory.Label("説明", transform, new Vector2(.5f, 1), new Vector2(0, -120), new Vector2(1040, 50), "ひとりで育て、仲間と崩す。属性反応と部位破壊で巨大ボスに挑もう。", 21, null, TextAnchor.MiddleCenter);
            for (int i = 0; i < CharacterCatalog.All.Length; i++)
            {
                var character = CharacterCatalog.All[i];
                cards[i] = UIFactory.Button(character.Id, transform, new Vector2(.5f, .5f), new Vector2((i - (CharacterCatalog.All.Length - 1) * .5f) * 236, 46), new Vector2(220, 250),
                    character.Name + "\n\n" + character.Description, () => Select(character.Id));
                var label = cards[i].GetComponentInChildren<Text>();
                label.rectTransform.anchoredPosition = new Vector2(0, -35);
                label.rectTransform.sizeDelta = new Vector2(200, 156);
                label.fontSize = 15;
                var portrait = UIFactory.Panel("立ち絵", cards[i].transform, new Vector2(.5f, .5f), new Vector2(0, 72), new Vector2(86, 86), Color.white);
                portrait.sprite = ArsenalArt.Hero(character.Id) ?? Resources.Load<Sprite>("RogueSurvivors/Art/" + character.Id);
                portrait.preserveAspect = true; portrait.raycastTarget = false;
            }
            selection = UIFactory.Label("選択中", transform, new Vector2(.5f, .5f), new Vector2(0, -100), new Vector2(1000, 36), "", 21, UIFactory.Cyan, TextAnchor.MiddleCenter);
            mageChoice = UIFactory.Button("初期属性", transform, new Vector2(.5f,.5f), new Vector2(210,-100), new Vector2(460,32), "", () => gameObject.GetComponent<MageLoadoutUI>().Open(RefreshMage));
            gameObject.AddComponent<MageLoadoutUI>();
            preparation = UIFactory.Label("準備時間", transform, new Vector2(.5f, 0), new Vector2(0, 188), new Vector2(260, 36), "", 21, null, TextAnchor.MiddleCenter);
            UIFactory.Button("時間短縮", transform, new Vector2(.5f, 0), new Vector2(-185, 188), new Vector2(64, 36), "−", () => AdjustTime(-30));
            UIFactory.Button("時間延長", transform, new Vector2(.5f, 0), new Vector2(185, 188), new Vector2(64, 36), "＋", () => AdjustTime(30));
            AdjustTime(0);
            UIFactory.Button("出撃", transform, new Vector2(.5f, 0), new Vector2(-290, 108), new Vector2(340, 62), "このキャラクターで出撃", () => GameManager.Instance.StartSolo());
            UIFactory.Button("ボスロビー", transform, new Vector2(.5f, 0), new Vector2(110, 108), new Vector2(420, 62), "保存したビルドでボス戦へ", () => GameManager.Instance.OpenLobby());
            UIFactory.Button("練習", transform, new Vector2(.5f, 0), new Vector2(450, 108), new Vector2(200, 62), "ボス戦の練習", () => NetworkManager.Instance.Practice());
            UIFactory.Label("操作説明", transform, new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(1150, 56),
                "移動：WASD／矢印キー　｜　攻撃：自動　｜　強化選択：クリック／1・2・3　｜　一時停止：Esc\nソロ終了時にビルドを保存します。マルチボス戦は最大4人の協力プレイです。", 17, new Color(.72f, .78f, .86f), TextAnchor.MiddleCenter);
            var arsenal=gameObject.AddComponent<ArsenalGalleryUI>();
            UIFactory.Button("図鑑",transform,new Vector2(1,1),new Vector2(-20,-68),new Vector2(220,32),"キャラ・武器図鑑",()=>arsenal.Open()).GetComponentInChildren<Text>().fontSize=16;
            var guide = gameObject.AddComponent<BuildGuideUI>();
            UIFactory.Button("組み合わせ", transform, new Vector2(1,1), new Vector2(-20,-20), new Vector2(220,42), "武器・属性ガイド", guide.Open);
            var bossButton = UIFactory.Button("討伐対象", transform, Vector2.up, new Vector2(20,-20), new Vector2(290,42), "", CycleBoss);
            bossSelection = bossButton.GetComponentInChildren<Text>(); bossSelection.fontSize = 17; RefreshBoss();
            var bossGuide=gameObject.AddComponent<BossGuideUI>();
            var guideButton=UIFactory.Button("ボス攻略",transform,Vector2.up,new Vector2(20,-68),new Vector2(290,32),"ボスの特徴・回復",bossGuide.Open);
            guideButton.GetComponentInChildren<Text>().fontSize=16;
            Select(GameManager.Instance.SelectedCharacter);
        }
        void RefreshMage() { mageChoice.gameObject.SetActive(GameManager.Instance.SelectedCharacter == "mage"); mageChoice.GetComponentInChildren<Text>().text = "初期属性：" + WeaponCatalog.Find(MageLoadout.SelectedWeapon).Name + " Lv2　変更 ▷"; mageChoice.GetComponentInChildren<Text>().fontSize=18; }
        void CycleBoss() { BossCatalog.Selection = BossCatalog.Selection >= 3 ? -1 : BossCatalog.Selection + 1; RefreshBoss(); }
        void RefreshBoss() { bossSelection.text = "討伐：" + (BossCatalog.Selection < 0 ? "ランダム" : BossCatalog.Names[BossCatalog.Selection]) + " ▷"; }
        void AdjustTime(float change)
        {
            if (change != 0) GameManager.Instance.SetPreparationTime(GameManager.Instance.soloDuration + change);
            int seconds = (int)GameManager.Instance.soloDuration;
            preparation.text = "準備時間　" + (seconds / 60) + "分" + (seconds % 60).ToString("00") + "秒";
        }
        void Select(string id)
        {
            GameManager.Instance.SelectCharacter(id);
            selection.text = "選択中：" + CharacterCatalog.Find(id).Name;
            selection.rectTransform.anchoredPosition=new Vector2(id=="mage"?-290:0,-100);
            RefreshMage();
            for (int i = 0; i < cards.Length; i++)
                cards[i].GetComponent<Image>().color = CharacterCatalog.All[i].Id == id ? new Color(.16f, .37f, .45f) : new Color(.09f, .14f, .23f);
        }
    }
}
