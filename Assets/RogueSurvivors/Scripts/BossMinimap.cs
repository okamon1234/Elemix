using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class BossMinimap : MonoBehaviour
    {
        RectTransform panel;
        Image boss;
        readonly Image[] players=new Image[4];
        void Start()
        {
            panel=UIFactory.Panel("決戦マップ",transform,Vector2.one,new Vector2(-24,-80),new Vector2(190,122),UIFactory.Ink).rectTransform;
            UIFactory.Label("凡例",panel,Vector2.up,new Vector2(6,-3),new Vector2(178,20),"自分：水色　仲間：白　ボス：赤",11);
            boss=Dot("ボス",new Color(1,.3f,.35f),10);
            for(int i=0;i<4;i++) players[i]=Dot("プレイヤー"+i,Color.white,7);
        }
        Image Dot(string name,Color color,float size) { var image=UIFactory.Panel(name,panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(size,size),color); image.raycastTarget=false; return image; }
        void Place(Image image,Vector2 position) => image.rectTransform.anchoredPosition=new Vector2(position.x/BossArena.HalfWidth*86,position.y/BossArena.HalfHeight*44-7);
        void Update()
        {
            if(!panel) return;
            bool visible=GameManager.Instance && GameManager.Instance.Mode==RunMode.Boss;
            panel.gameObject.SetActive(visible); if(!visible) return;
            BossAI target=null; foreach(var enemy in EnemyHealth.Active) if(enemy && enemy.IsBoss) { target=enemy.GetComponent<BossAI>(); break; }
            boss.enabled=target; if(target) Place(boss,target.transform.position);
            var all=FindObjectsByType<PlayerHealth>(FindObjectsSortMode.InstanceID);
            for(int i=0;i<players.Length;i++) { players[i].enabled=i<all.Length; if(i<all.Length) { players[i].color=all[i].Alive?(all[i].IsLocal?UIFactory.Cyan:Color.white):Color.gray; Place(players[i],all[i].transform.position); } }
        }
    }
}
