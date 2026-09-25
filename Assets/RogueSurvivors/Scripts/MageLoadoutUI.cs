using System;
using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class MageLoadoutUI : MonoBehaviour
    {
        GameObject panel;
        public void Open(Action changed)
        {
            if(panel) Destroy(panel);
            panel=UIFactory.Panel("初期属性を選択",transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1100,600),new Color(.035f,.05f,.1f,1)).gameObject;
            UIFactory.Label("見出し",panel.transform,new Vector2(.5f,1),new Vector2(0,-26),new Vector2(1000,42),"メイジの初期属性を選ぶ",30,UIFactory.Cyan,TextAnchor.MiddleCenter);
            UIFactory.Label("説明",panel.transform,new Vector2(.5f,1),new Vector2(0,-75),new Vector2(1000,36),"選んだ武器のLv2で出撃。保存済みのビルドは変わりません。",18,null,TextAnchor.MiddleCenter);
            for(int i=0;i<MageLoadout.Weapons.Length;i++) {
                string id=MageLoadout.Weapons[i]; var definition=WeaponCatalog.Find(id);
                var button=UIFactory.Button(id,panel.transform,new Vector2(.5f,1),new Vector2((i%3-1)*345,-130-(i/3)*124),new Vector2(328,112),definition.Name+" Lv2\n"+definition.Description,()=>{MageLoadout.SelectedWeapon=id;changed?.Invoke();Destroy(panel);});
                var label=button.GetComponentInChildren<Text>();label.fontSize=15;label.rectTransform.anchoredPosition=new Vector2(32,0);label.rectTransform.sizeDelta=new Vector2(252,102);
                var icon=UIFactory.Panel("武器",button.transform,new Vector2(.5f,.5f),new Vector2(-127,0),new Vector2(56,70),Color.white);icon.sprite=ArsenalArt.Weapon(id);icon.preserveAspect=true;icon.raycastTarget=false;
                button.GetComponent<Image>().color=id==MageLoadout.SelectedWeapon?new Color(.16f,.37f,.45f):new Color(.09f,.14f,.23f);
            }
            UIFactory.Button("閉じる",panel.transform,new Vector2(.5f,0),new Vector2(0,20),new Vector2(240,38),"閉じる",()=>Destroy(panel));
        }
    }
}
