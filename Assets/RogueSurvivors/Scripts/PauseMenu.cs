using UnityEngine;
using UnityEngine.InputSystem;
namespace RogueSurvivors
{
    public sealed class PauseMenu : MonoBehaviour
    {
        GameObject panel;
        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.IsPlaying || gm.ChoosingUpgrade || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (panel) Close(); else Open();
        }
        void Open()
        {
            if (!NetworkManager.InRoom) Time.timeScale = 0;
            panel = UIFactory.Panel("一時停止", transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(560, 320), UIFactory.Ink).gameObject;
            UIFactory.Label("見出し", panel.transform, new Vector2(.5f, 1), new Vector2(0, -25), new Vector2(500, 58),
                NetworkManager.InRoom ? "メニュー（戦闘は継続中）" : "一時停止", 29, UIFactory.Cyan, TextAnchor.MiddleCenter);
            UIFactory.Button("再開", panel.transform, new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(360, 58), "戦闘に戻る", Close);
            UIFactory.Button("ホーム", panel.transform, new Vector2(.5f, 0), new Vector2(0, 44), new Vector2(360, 58), "この戦闘を終了してホームへ", () => { Close(); GameManager.Instance.ReturnHome(); });
        }
        void Close() { if (panel) { panel.SetActive(false); Destroy(panel); } Time.timeScale = 1; }
        void OnDestroy() => Time.timeScale = 1;
    }
}
