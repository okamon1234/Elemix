using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RogueSurvivors
{
    public sealed class LevelUpUI : MonoBehaviour
    {
        GameObject panel;
        Action<int> selected;
        public bool HasSelectionCallback => selected != null;
        public void Show(List<UpgradeOption> options, Action<int> onSelected, int level)
        {
            Hide(); selected = onSelected;
            panel = UIFactory.Panel("UpgradeOverlay", transform, Vector2.zero, Vector2.zero, new Vector2(1280, 720), new Color(.015f, .025f, .06f, .96f)).gameObject;
            var overlay = panel.GetComponent<RectTransform>(); overlay.anchorMax = Vector2.one; overlay.sizeDelta = Vector2.zero;
            UIFactory.Label("Heading", panel.transform, new Vector2(.5f, 1), new Vector2(0, -130), new Vector2(900, 64), "レベル " + level + "　／　強化を1つ選択", 30, UIFactory.Cyan, TextAnchor.MiddleCenter);
            UIFactory.Label("Hint", panel.transform, new Vector2(.5f, 1), new Vector2(0, -200), new Vector2(800, 36), "カードをクリック、または数字キー 1・2・3 で選択", 18, null, TextAnchor.MiddleCenter);
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var button = UIFactory.Button("Card" + i, panel.transform, new Vector2(.5f, .5f), new Vector2((i - 1) * 335, -65), new Vector2(310, 350),
                    "[ " + (i + 1) + " ]\n\n" + options[i].Title + "\n\n" + options[i].Description, () => selected?.Invoke(index));
                var label=button.GetComponentInChildren<Text>();label.fontSize=16;
                label.text=options[i].Title+"\n\n"+options[i].Description;
                label.rectTransform.anchoredPosition=new Vector2(0,-40);label.rectTransform.sizeDelta=new Vector2(280,250);
                UIFactory.Label("番号",button.transform,new Vector2(.5f,.5f),new Vector2(85,130),new Vector2(56,44),(i+1).ToString(),25,UIFactory.Cyan,TextAnchor.MiddleCenter);
                var art=ArsenalArt.Upgrade(options[i].Kind);
                if(art) {var icon=UIFactory.Panel("武器",button.transform,new Vector2(.5f,.5f),new Vector2(-30,130),new Vector2(78,78),Color.white);icon.sprite=art;icon.preserveAspect=true;icon.raycastTarget=false;}
                if (i == 0 && EventSystem.current) EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
        void Update()
        {
            if (!panel || Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) selected?.Invoke(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) selected?.Invoke(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) selected?.Invoke(2);
        }
        public void Hide() { selected = null; if (panel) { panel.SetActive(false); Destroy(panel); } }
    }
}
