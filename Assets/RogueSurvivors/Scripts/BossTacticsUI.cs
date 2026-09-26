using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace RogueSurvivors
{
    public sealed class BossTacticsUI : MonoBehaviour
    {
        GameObject panel;readonly Button[] buttons=new Button[4];readonly Text[] markers=new Text[4];Text help,rewardText;
        BossParts boss;
        static readonly Color[] colors={new Color(.25f,1,1),new Color(1,.65f,.95f),new Color(.65f,1,.4f),new Color(.7f,.7f,1)};
        void Awake()
        {
            panel=UIFactory.Panel("部位ピン",transform,Vector2.up,new Vector2(24,-215),new Vector2(302,160),UIFactory.Ink).gameObject;
            UIFactory.Label("説明",panel.transform,Vector2.up,new Vector2(10,-5),new Vector2(282,28),"狙う部位を仲間へ共有",17,UIFactory.Cyan);
            for(int i=0;i<4;i++){
                int part=i;buttons[i]=UIFactory.Button("部位"+i,panel.transform,Vector2.up,new Vector2(10+i%2*144,-38-i/2*34),new Vector2(138,30),"",()=>Send(part));
                buttons[i].GetComponentInChildren<Text>().fontSize=16;
                markers[i]=UIFactory.Label("ピン"+i,transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,28),"",18,null,TextAnchor.MiddleCenter);
            }
            help=UIFactory.Label("操作",panel.transform,Vector2.up,new Vector2(10,-109),new Vector2(282,46),"Q／RB：次の部位　E／LB：解除\n中クリックでも指定・8秒表示",13);
            rewardText=UIFactory.Label("破壊チャンス",transform,Vector2.up,new Vector2(24,-381),new Vector2(310,88),"",16,new Color(1,.85f,.3f));
        }
        void Send(int part){var p=PlayerHealth.Local;if(p)p.GetComponent<PlayerPartPing>().SetPart(boss,part);}
        void Update()
        {
            if(!boss){foreach(var enemy in EnemyHealth.Active)if(enemy && enemy.IsBoss && enemy.Alive){boss=enemy.GetComponent<BossParts>();break;}}
            var player=PlayerHealth.Local;bool active=boss && boss.GetComponent<EnemyHealth>().Alive && GameManager.Instance && GameManager.Instance.IsPlaying;
            panel.SetActive(active);rewardText.text="";foreach(var marker in markers)marker.text="";if(!active || !player)return;
            var ping=player.GetComponent<PlayerPartPing>();var ai=boss.GetComponent<BossAI>();
            int chosen=ping.VisiblePart(boss);
            help.text=chosen>=0?"破壊後："+BossAttackRoutes.Description(ai.Kind,chosen+1)+"\nこの側へ回り込む　E／LB：解除":"Q／RB：次の部位　E／LB：解除\n中クリックでも指定・8秒表示";
            bool input=player.Alive && Time.timeScale>0 && !(HUDController.Instance && HUDController.Instance.LevelUI.HasSelectionCallback);
            for(int i=0;i<4;i++){
                buttons[i].interactable=input && boss.Health[i]>0;
                buttons[i].GetComponentInChildren<Text>().text=(ping.VisiblePart(boss)==i?"◆ ":"")+BossCatalog.PartName(ai.Kind,i)+(boss.Health[i]<=0?" 済":"");
            }
            if(input){
                var k=Keyboard.current;var pad=Gamepad.current;
                if((k!=null && k.qKey.wasPressedThisFrame)||(pad!=null && pad.rightShoulder.wasPressedThisFrame))ping.Cycle(boss);
                if((k!=null && k.eKey.wasPressedThisFrame)||(pad!=null && pad.leftShoulder.wasPressedThisFrame))Send(-1);
                if(Mouse.current!=null && Mouse.current.middleButton.wasPressedThisFrame && Camera.main && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject())){
                    Vector2 point=Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                    if(Vector2.Distance(point,boss.transform.position)<3.5f)Send(boss.PartFrom(point));
                }
            }
            int row=0;var used=new int[4];
            foreach(var candidate in FindObjectsByType<PlayerPartPing>(FindObjectsSortMode.InstanceID)){
                int part=candidate.VisiblePart(boss);if(part<0 || row>=markers.Length || !Camera.main)continue;
                Vector3 screen=Camera.main.WorldToViewportPoint(boss.PartPosition(part));if(screen.z<0)continue;
                var rt=(RectTransform)transform;var mark=markers[row++];
                mark.rectTransform.anchoredPosition=new Vector2((screen.x-.5f)*rt.rect.width,(screen.y-.5f)*rt.rect.height+24+used[part]++*24);
                mark.text="▼ P"+candidate.Number+" "+BossCatalog.PartName(ai.Kind,part);mark.color=colors[(candidate.Number-1)%4];
            }
            var reward=boss.GetComponent<BossBreakReward>();
            if(reward.Part>=0 && reward.Remaining>0){
                string title=BossCatalog.PartName(ai.Kind,reward.Part);
                rewardText.text=reward.Stagger>0?title+"破壊！ ボスがひるんでいる":reward.Open?title+"側の金色の範囲へ！ 残り"+reward.Remaining.ToString("0.0")+"秒\n本体弱点：物理1.6倍／属性1.3倍\n既に出た敵の攻撃には注意":reward.Budget<=0?"弱点の追加ダメージ上限に到達":ai.IsTransforming?"形態変化後に弱点が開く！":"";
            }
        }
    }
}
