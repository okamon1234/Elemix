using System;
using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class MapSelectionUI : MonoBehaviour
    {
        GameObject panel,overlay;
        public void Open(Action changed=null)
        {
            Close();
            var blocker=UIFactory.Panel("マップ選択の背景",transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(0,0,0,.7f));
            blocker.rectTransform.anchorMin=Vector2.zero;blocker.rectTransform.anchorMax=Vector2.one;
            blocker.rectTransform.offsetMin=blocker.rectTransform.offsetMax=Vector2.zero;overlay=blocker.gameObject;
            panel=UIFactory.Panel("出撃マップ選択",overlay.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1180,630),new Color(.035f,.05f,.10f,1)).gameObject;
            UIFactory.Label("題名",panel.transform,new Vector2(.5f,1),new Vector2(0,-18),new Vector2(1000,46),"ソロで探索するマップを選ぶ",29,UIFactory.Cyan,TextAnchor.MiddleCenter);
            for(int i=0;i<2;i++) {
                var kind=(SoloMapKind)i;var card=UIFactory.Panel("マップ "+kind,panel.transform,new Vector2(.5f,.5f),new Vector2(i==0?-280:280,12),new Vector2(520,452),new Color(.07f,.11f,.15f));
                var preview=UIFactory.Rect("地形プレビュー",card.transform,new Vector2(.5f,1),new Vector2(0,-14),new Vector2(484,175)).gameObject.AddComponent<RawImage>();preview.texture=MapArt.Ground(kind);preview.color=new Color(.8f,.85f,.8f);preview.raycastTarget=false;
                for(int n=0;n<3;n++) {
                    var prop=UIFactory.Panel("景観",preview.transform,new Vector2(.5f,.5f),new Vector2((n-1)*145,0),new Vector2(155,155),Color.white);prop.sprite=MapArt.Prop(kind==SoloMapKind.Meadow?n:n+4);prop.preserveAspect=true;prop.raycastTarget=false;
                }
                UIFactory.Label("マップ名",card.transform,new Vector2(.5f,1),new Vector2(0,-196),new Vector2(480,40),SoloMapCatalog.Name(kind),27,UIFactory.Cyan,TextAnchor.MiddleCenter);
                UIFactory.Label("特徴",card.transform,new Vector2(.5f,1),new Vector2(0,-246),new Vector2(475,114),SoloMapCatalog.Description(kind),18,Color.white,TextAnchor.UpperLeft);
                UIFactory.Button("選ぶ "+kind,card.transform,new Vector2(.5f,0),new Vector2(0,22),new Vector2(330,48),SoloMapCatalog.Selected==kind?"選択中："+SoloMapCatalog.Name(kind):SoloMapCatalog.Name(kind)+"を選ぶ",()=>{SoloMapCatalog.Select(kind);changed?.Invoke();Close();});
            }
            UIFactory.Label("注記",panel.transform,new Vector2(.5f,0),new Vector2(0,58),new Vector2(1000,30),"時間による敵の強化は共通。地形を使って自分のビルドに合う戦い方を探そう。",17,null,TextAnchor.MiddleCenter);
            UIFactory.Button("閉じる",panel.transform,new Vector2(.5f,0),new Vector2(0,12),new Vector2(200,36),"閉じる",Close);
        }
        public void Close(){if(overlay){overlay.SetActive(false);Destroy(overlay);}overlay=null;panel=null;}
    }
}
