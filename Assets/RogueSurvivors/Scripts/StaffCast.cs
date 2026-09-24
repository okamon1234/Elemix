using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // 所有者だけが命中を計算し、ほかの端末では同じ段階の演出だけ再生する。
    public sealed class StaffCast : MonoBehaviour
    {
        CombatElement element; Vector2 origin, target, heading; int level, tier, pulse; float power, elapsed, nextPulse; PlayerHealth source; bool evolved;
        public static int Tier(int level) => level>=8?3:level>=5?2:level>=3?1:0;
        public static float Radius(CombatElement element,int level,bool evolved=false) => (element==CombatElement.Ice?1.0f:element==CombatElement.Dark?1.8f:1.4f)+Tier(level)*.35f+(evolved?.7f:0);
        public static StaffCast Create(CombatElement element,Vector2 from,Vector2 to,int level,float damage,PlayerHealth player,bool evolved=false)
        {
            var cast=new GameObject("属性武器の攻撃").AddComponent<StaffCast>(); cast.element=element; cast.origin=from; cast.target=to;
            cast.heading=(to-from).normalized; if(cast.heading.sqrMagnitude<.1f) cast.heading=Vector2.up;
            cast.level=Mathf.Clamp(level,1,8); cast.tier=Tier(level); cast.power=damage; cast.source=player; cast.evolved=evolved; cast.nextPulse=.18f;
            cast.LaunchVisual(); return cast;
        }
        void LaunchVisual()
        {
            Color c=ElementWeapon.ColorFor(element);
            CombatFx.Ring(origin,.4f+tier*.1f,new Color(c.r,c.g,c.b,.7f),.4f,.4f,.045f,6);
            CombatMote.Create(origin,Vector2.zero,Color.Lerp(c,Color.white,.6f),new Vector2(.4f,.4f),.25f,3);
            if(element==CombatElement.Ice) for(int i=-tier-1;i<=tier+1;i++) {
                Vector2 v=Quaternion.Euler(0,0,i*7)*heading;
                CombatFx.Shard(origin+v*.5f,v*16,c,.38f+tier*.1f,Mathf.Max(.2f,Vector2.Distance(origin,target)/16));
            }
            else if(element==CombatElement.Wood || element==CombatElement.Earth) {
                CombatFx.Ring(target,Radius(element,level),c,.3f,.1f,.06f,element==CombatElement.Earth?6:8);
                for(int i=0;i<3+tier;i++) CombatFx.Shard(Vector2.Lerp(origin,target,i/(float)(3+tier)),heading*5,c,.2f,.3f);
            }
            else if(element==CombatElement.Dark) { CombatFx.Ring(target,Radius(element,level,evolved),c,.9f,-.8f,.25f); CombatFx.Ring(target,.4f,c,.8f,3,.12f); }
            else if(element==CombatElement.Water) for(int i=0;i<2+tier;i++) CombatFx.Ring(Vector2.Lerp(origin,target,i/(float)(2+tier)),.4f,c,.4f,2,.12f);
            else CombatFx.Shard(origin,heading*18,c,.8f+tier*.25f,.45f);
        }
        void Update()
        {
            if(GameManager.Instance && !GameManager.Instance.IsPlaying) { Destroy(gameObject); return; }
            elapsed+=Time.deltaTime;
            int count=(element==CombatElement.Wood || element==CombatElement.Water)?2+tier:element==CombatElement.Dark?2:1+(tier>=2?1:0);
            if(elapsed>=nextPulse && pulse<count) { HitPulse(pulse++); nextPulse+=element==CombatElement.Wood?.3f:.22f; }
            if(pulse>=count && elapsed>nextPulse+.35f) Destroy(gameObject);
        }
        void ImpactVisual(Vector2 point,float radius,Color c,int index)
        {
            int count=4+tier*2;
            if(element==CombatElement.Light) {
                // 槍の穂先と翼状の光片。進化すると二重の聖印を残す。
                Vector2 side=new Vector2(-heading.y,heading.x);
                CombatFx.Stroke(new Vector3[]{origin,origin+heading*9},new Color(c.r,c.g,c.b,.25f),.65f+tier*.15f,.32f);
                CombatFx.Stroke(new Vector3[]{origin,origin+heading*9},Color.white,.07f,.25f);
                for(int i=0;i<count;i++) CombatMote.Create(origin+heading*(1+i*.9f),side*(i%2==0?1:-1)*2,c,new Vector2(.2f,.6f),.5f,3);
                CombatMote.Create(origin+heading*8,heading*2,Color.white,new Vector2(.5f,1.2f),.4f);
                if(evolved) for(int i=0;i<2;i++) CombatFx.Ring(point,.8f+i*.5f,c,.6f,.3f,.08f,8);
            } else if(element==CombatElement.Dark) {
                // 星屑が中心へ吸い込まれる重力渦。
                CombatMote.Create(point,Vector2.zero,new Color(.17f,.06f,.28f),new Vector2(radius*.6f,radius*.6f),.5f,3,90);
                for(int i=0;i<count+2;i++) {Vector2 d=Quaternion.Euler(0,0,i*360f/(count+2)+index*35)*Vector2.right;CombatMote.Create(point+d*radius,-d*radius*2,c,new Vector2(.15f,.42f),.5f,3,120);}
            } else if(element==CombatElement.Water) {
                for(int i=0;i<3;i++) {
                    var wave=new Vector3[18];
                    for(int j=0;j<wave.Length;j++) {float a=j*Mathf.PI/(wave.Length-1);wave[j]=point+new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.55f+i*.17f);}
                    CombatFx.Stroke(wave,i==2?Color.white:c,i==1?.2f:.065f,.45f);
                }
                for(int i=0;i<count;i++) {Vector2 d=Quaternion.Euler(0,0,i*360f/count)*Vector2.right;CombatMote.Create(point+d*.3f,d*3+Vector2.up,c,new Vector2(.18f,.34f),.5f);}
            } else if(element==CombatElement.Wood||element==CombatElement.Earth) {
                for(int i=0;i<count;i++) {
                    float a=i*Mathf.PI*2/count; Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)); Vector2 at=point+d*radius*.65f;
                    if(element==CombatElement.Earth) {CombatMote.Create(at,Vector2.up*.5f,c,new Vector2(.45f,.8f+tier*.16f),.6f,2);CombatMote.Create(at+d*.3f,d*3,c,new Vector2(.17f,.23f),.5f,2,80);}
                    else {
                        CombatMote.Create(at,Vector2.up*.4f,c,new Vector2(.24f,.8f+tier*.12f),.6f);
                        CombatMote.Create(at+Vector2.up*.4f,d*2,Color.Lerp(c,Color.white,.2f),new Vector2(.35f,.5f),.65f,1,100);
                    }
                }
            } else if(element==CombatElement.Ice) {
                CombatFx.Ring(point,radius,c,.5f,.3f,.08f,6);
                for(int i=0;i<6;i++) {Vector2 d=Quaternion.Euler(0,0,i*60)*Vector2.right;CombatFx.Stroke(new Vector3[]{point,point+d*radius},new Color(.85f,1,1,.8f),.06f,.45f);}
                CombatMote.Create(point,Vector2.up*.5f,Color.Lerp(c,Color.white,.5f),new Vector2(.45f,.9f+tier*.2f),.5f);
            }
        }
        void HitPulse(int index)
        {
            float radius=Radius(element,level,evolved); Color c=ElementWeapon.ColorFor(element);
            Vector2 side=new Vector2(-heading.y,heading.x), point=target;
            if(element==CombatElement.Wood) point+=side*(index%2==0?-1:1)*radius*.45f;
            if(element==CombatElement.Water) point+=heading*index*.65f;
            if(element==CombatElement.Ice) {
                for(int i=0;i<5+tier*2;i++) { Vector2 d=Quaternion.Euler(0,0,i*360f/(5+tier*2))*Vector2.right; CombatFx.Shard(point+d*.3f,d*3,c,.5f+tier*.12f,.4f); }
            }
            else if(element==CombatElement.Wood || element==CombatElement.Earth) {
                for(int i=0;i<3+tier*2;i++) {
                    float a=i*Mathf.PI*2/(3+tier*2); Vector2 basePoint=point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.7f;
                    float height=(element==CombatElement.Earth?1.3f:.9f)+tier*.25f;
                    var spike=CombatFx.Stroke(new[]{Vector3.zero,new Vector3(-.24f,.15f),new Vector3(.1f,height),new Vector3(.33f,.15f)},c,element==CombatElement.Earth?.22f:.12f,.48f,true);
                    spike.transform.position=basePoint;
                }
            }
            else if(element==CombatElement.Light || element==CombatElement.Wind) {
                for(int i=0;i<1+tier;i++) {
                    Vector2 offset=side*(i-tier*.5f)*.4f;
                    CombatFx.Stroke(new Vector3[]{origin+offset,origin+offset+heading*9},c,evolved?.32f:.15f,.35f);
                    CombatFx.Shard(origin+offset+heading*8,heading*3,Color.white,.7f,.25f);
                }
                if(evolved) CombatFx.Ring(target,2,Color.white,.5f,1,.15f,8);
            }
            else { CombatFx.Ring(point,radius,c,.45f,element==CombatElement.Dark?-.9f:.8f,.18f); if(element==CombatElement.Dark && index==1) CombatFx.Reaction(7,element,point); }
            ImpactVisual(point,radius,c,index);
            if(!source || !source.IsLocal || !source.Alive || power<=0) return;
            foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if(!enemy || !enemy.Alive) continue; Vector2 offset=(Vector2)enemy.transform.position-origin;
                bool hit=Vector2.Distance(enemy.transform.position,point)<=radius+(enemy.IsBoss?.7f:0);
                if(element==CombatElement.Light || element==CombatElement.Wind) hit=Vector2.Dot(offset,heading)>=0 && Vector2.Dot(offset,heading)<=9 && Mathf.Abs(Vector2.Dot(offset,side))<=.65f+tier*.3f+(enemy.IsBoss?.7f:0);
                if(!hit) continue;
                float multiplier=index==0?1:element==CombatElement.Dark?.85f:.45f;
                enemy.Damage(power*multiplier*(evolved?1.5f:1),heading,source,element);
                if(!enemy || !enemy.Alive) continue;
                if(element==CombatElement.Ice || element==CombatElement.Wood) {var ailment=enemy.GetComponent<EnemyAilment>(); if(!ailment) ailment=enemy.gameObject.AddComponent<EnemyAilment>(); ailment.Slow(1+tier*.25f);}
                if(element==CombatElement.Dark && !enemy.IsBoss) {var body=enemy.GetComponent<Rigidbody2D>(); if(body) body.position=Vector2.MoveTowards(body.position,point,evolved?.8f:.3f);}
            }
        }
    }
}
