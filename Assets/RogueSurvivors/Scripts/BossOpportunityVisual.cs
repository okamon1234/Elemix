using UnityEngine;
namespace RogueSurvivors
{
    public sealed class BossOpportunityVisual : MonoBehaviour
    {
        BossBreakReward reward;LineRenderer line;static Material material;
        void Awake()
        {
            reward=GetComponent<BossBreakReward>();
            var go=new GameObject("破壊側の攻撃チャンス");go.transform.SetParent(transform,false);line=go.AddComponent<LineRenderer>();
            if(!material)material=new Material(Shader.Find("Sprites/Default"));line.sharedMaterial=material;
            line.useWorldSpace=true;line.loop=true;line.widthMultiplier=.055f;line.positionCount=26;
            line.sortingLayerName="Projectiles";line.sortingOrder=70;
        }
        void Update()
        {
            line.enabled=reward.Open;if(!line.enabled)return;
            Color c=new Color(1,.82f,.25f,.65f+.15f*Mathf.Sin(Time.time*8));line.startColor=line.endColor=c;
            for(int i=0;i<13;i++){
                float a=(reward.Part*90-43+i*86f/12)*Mathf.Deg2Rad;Vector3 d=new Vector3(Mathf.Cos(a),Mathf.Sin(a));
                line.SetPosition(i,transform.position+d*BossBreakReward.Reach);line.SetPosition(25-i,transform.position+d*1.6f);
            }
        }
    }
}
