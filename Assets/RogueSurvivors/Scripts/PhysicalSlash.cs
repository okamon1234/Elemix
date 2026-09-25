using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // 刃の進行角度と命中角度を一致させた大きななぎ払い。
    public sealed class PhysicalSlash : MonoBehaviour
    {
        Vector2 origin;float baseAngle,reach,age,damage,startAngle,sweep;PlayerHealth owner;SpriteRenderer blade;
        LineRenderer trail;readonly HashSet<EnemyHealth> struck=new HashSet<EnemyHealth>();static Material material;
        public static void Create(int style,Vector2 from,Vector2 heading,float reach,float damage,PlayerHealth owner,bool reverse=false)
        {
            var fx=new GameObject("合体武器のなぎ払い").AddComponent<PhysicalSlash>();
            fx.origin=from;fx.baseAngle=Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg;fx.reach=reach;fx.damage=damage;fx.owner=owner;
            fx.startAngle=reverse?110:-110;fx.sweep=reverse?-220:220;
            fx.blade=new GameObject("大きな刃").AddComponent<SpriteRenderer>();fx.blade.transform.SetParent(fx.transform);fx.blade.sprite=PhysicalFusionArt.Get(style);fx.blade.sortingLayerName="Projectiles";fx.blade.sortingOrder=22;fx.blade.transform.localScale=Vector3.one*1.6f;
            if(!material)material=new Material(Shader.Find("Sprites/Default"));
            fx.trail=fx.gameObject.AddComponent<LineRenderer>();fx.trail.sharedMaterial=material;fx.trail.positionCount=18;fx.trail.startWidth=.5f;fx.trail.endWidth=.05f;fx.trail.sortingLayerName="Projectiles";fx.trail.sortingOrder=19;fx.Draw(0);
        }
        void Draw(float t)
        {
            float current=baseAngle+startAngle+sweep*t;Vector2 direction=Quaternion.Euler(0,0,current)*Vector2.right;
            blade.transform.position=origin+direction*(reach-1);blade.transform.rotation=Quaternion.Euler(0,0,current);
            blade.color=new Color(1,1,1,Mathf.Min(1,(1-t)*5));
            for(int i=0;i<18;i++){float a=current-sweep*Mathf.Min(.45f,t)*i/17;Vector2 d=Quaternion.Euler(0,0,a)*Vector2.right;trail.SetPosition(i,origin+d*(reach-.2f));}
            trail.startColor=new Color(.92f,.95f,.98f,Mathf.Min(.8f,(1-t)*3));trail.endColor=new Color(.6f,.65f,.7f,0);
        }
        void Update()
        {
            if(owner&&!owner.Alive || GameManager.Instance&&!GameManager.Instance.IsPlaying){Destroy(gameObject);return;}
            float previous=age/.36f;age+=Time.deltaTime;float t=Mathf.Clamp01(age/.36f);Draw(t);
            if(owner&&owner.IsLocal&&damage>0)foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if(!enemy||!enemy.Alive||struck.Contains(enemy))continue;
                Vector2 delta=(Vector2)enemy.transform.position-origin;if(delta.magnitude>reach+(enemy.IsBoss?.8f:0))continue;
                float angle=Mathf.DeltaAngle(baseAngle,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                float a=startAngle+sweep*previous,b=startAngle+sweep*t;
                if(angle<Mathf.Min(a,b)-22||angle>Mathf.Max(a,b)+22)continue;
                struck.Add(enemy);enemy.Damage(damage,delta.normalized,owner);owner.GrantShield(owner.Maximum*.035f,.8f);
                CombatMote.Create(enemy.transform.position,Vector2.zero,new Color(.95f,.9f,.7f),new Vector2(.35f,.35f),.18f,3);
            }
            if(age>=.36f)Destroy(gameObject);
        }
    }
}
