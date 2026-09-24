using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class LobbyUI : MonoBehaviour
    {
        Text status, room;
        InputField code;
        Button start;
        void Start()
        {
            var backdrop = UIFactory.Panel("Background", transform, Vector2.zero, Vector2.zero, Vector2.zero, UIFactory.Ink);
            backdrop.rectTransform.anchorMax = Vector2.one;
            UIFactory.Label("Eyebrow", transform, new Vector2(.5f, 1), new Vector2(0, -62), new Vector2(900, 36), "サバイバーズ　／　ビルドの準備完了", 20, UIFactory.Cyan, TextAnchor.MiddleCenter);
            UIFactory.Label("Title", transform, new Vector2(.5f, 1), new Vector2(0, -108), new Vector2(900, 72), "仲間と挑む、遺跡のボス", 42, null, TextAnchor.MiddleCenter);
            var build = GameManager.Instance.SavedBuild;
            UIFactory.Label("Build", transform, new Vector2(.5f, 1), new Vector2(0, -185), new Vector2(1000, 64),
                build != null ? "保存ビルド　・　レベル " + build.level + "　・　攻撃倍率 " + build.damageMultiplier.ToString("0.0") + "倍　・　最大HP " + build.maxHealth : "保存ビルドがありません。練習用のレベル5ビルドを使用します。", 21, null, TextAnchor.MiddleCenter);
            UIFactory.Button("Connect", transform, new Vector2(.5f, .5f), new Vector2(-330, 52), new Vector2(280, 58), "接続について", () => NetworkManager.Instance.Connect());
            var fieldPanel = UIFactory.Panel("RoomCode", transform, new Vector2(.5f, .5f), new Vector2(0, 52), new Vector2(300, 58), new Color(.11f, .16f, .24f));
            code = fieldPanel.gameObject.AddComponent<InputField>();
            code.textComponent = UIFactory.Label("Text", fieldPanel.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(270, 50), "", 23);
            code.text = "rift-" + Random.Range(1000, 10000); code.characterLimit = 24;
            UIFactory.Button("Join", transform, new Vector2(.5f, .5f), new Vector2(330, 52), new Vector2(280, 58), "部屋を作成／参加", () => NetworkManager.Instance.Join(code.text));
            room = UIFactory.Label("Room", transform, new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(900, 36), "", 22, UIFactory.Cyan, TextAnchor.MiddleCenter);
            start = UIFactory.Button("Start", transform, new Vector2(.5f, .5f), new Vector2(0, -80), new Vector2(360, 60), "全員でボス戦を開始", () => NetworkManager.Instance.StartArena());
            status = UIFactory.Label("Status", transform, new Vector2(.5f, 0), new Vector2(0, 135), new Vector2(1120, 80), "", 19, new Color(.7f, .77f, .85f), TextAnchor.MiddleCenter);
            UIFactory.Button("Practice", transform, new Vector2(.5f, 0), new Vector2(-200, 56), new Vector2(350, 56), "ボス戦の練習", () => NetworkManager.Instance.Practice());
            UIFactory.Button("Back", transform, new Vector2(.5f, 0), new Vector2(200, 56), new Vector2(350, 56), "退出してホームへ", () => NetworkManager.Instance.Leave());
        }
        void Update()
        {
            if (!status) return;
            status.text = NetworkManager.Instance.Status;
            room.text = NetworkManager.Instance.RoomSummary;
            start.interactable = NetworkManager.InRoom && NetworkManager.IsMaster && !NetworkManager.Instance.Busy;
        }
    }
}
