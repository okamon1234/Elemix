using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class SoloMinimap : MonoBehaviour
    {
        RectTransform panel;Text title,location;Image origin;readonly Image[] obstacles=new Image[96];float next;
        void Start()
        {
            panel=UIFactory.Panel("周辺の地形",transform,Vector2.one,new Vector2(-24,-80),new Vector2(210,156),UIFactory.Ink).rectTransform;
            title=UIFactory.Label("地名",panel,Vector2.up,new Vector2(8,-5),new Vector2(194,24),"",14,UIFactory.Cyan);
            location=UIFactory.Label("現在地",panel,Vector2.zero,new Vector2(8,4),new Vector2(194,20),"",11);
            for(int i=0;i<obstacles.Length;i++)obstacles[i]=UIFactory.Panel("障害物",panel,new Vector2(.5f,.5f),Vector2.zero,Vector2.one,new Color(.58f,.63f,.53f));
            origin=UIFactory.Panel("出発地点",panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(7,7),new Color(1,.8f,.3f));
            UIFactory.Panel("自分",panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(6,6),UIFactory.Cyan);
        }
        void Update()
        {
            if(!panel || Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;
            var map=SoloMap.Instance;var player=PlayerHealth.Local;bool visible=map && player && GameManager.Instance.Mode==RunMode.Solo;panel.gameObject.SetActive(visible);if(!visible)return;
            Vector2 p=player.transform.position;var key=SoloMapLayout.Key(p);
            title.text=SoloMapCatalog.Name(map.Kind)+"・"+SoloMapCatalog.District(map.Kind,SoloMapLayout.District(map.Kind,key));
            location.text="出発地点 "+Mathf.RoundToInt(p.magnitude)+"m　水色：自分";
            int index=0;
            foreach(var rect in map.ObstacleBounds) {
                Vector2 d=rect.center-p;if(Mathf.Abs(d.x)>22 || Mathf.Abs(d.y)>10)continue;if(index>=obstacles.Length)break;
                var dot=obstacles[index++];dot.enabled=true;dot.rectTransform.anchoredPosition=d*4;dot.rectTransform.sizeDelta=new Vector2(Mathf.Max(3,rect.width*4),Mathf.Max(3,rect.height*4));
            }
            while(index<obstacles.Length)obstacles[index++].enabled=false;
            origin.enabled=Mathf.Abs(p.x)<22 && Mathf.Abs(p.y)<10;origin.rectTransform.anchoredPosition=-p*4;
        }
    }
}
