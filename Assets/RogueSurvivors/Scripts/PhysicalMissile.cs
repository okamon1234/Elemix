using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public enum PhysicalMotion { Straight, Lob, Return, Spiral, Chain, Ricochet }
    // 見える金属武器そのものが移動して命中する。高速飛翔は線分判定で通り抜けを防ぐ。
    public sealed class PhysicalMissile : MonoBehaviour
    {
        public static int LocalHits {get;private set;}
        Vector2 origin,heading,last; PlayerHealth source;float damage,age,life,range,radius,spin;int remaining; PhysicalMotion motion;
        readonly Dictionary<EnemyHealth,float> hitAt=new Dictionary<EnemyHealth,float>();
        LineRenderer chain; SpriteRenderer sprite;static Material material;float nextDust;
        public static PhysicalMissile Create(Sprite art,Vector2 from,Vector2 direction,float range,float seconds,float damage,PlayerHealth owner,PhysicalMotion motion,float size=1,float radius=.4f,int pierce=20,float spin=600)
        {
            var go=new GameObject("物理武器の飛翔");var shot=go.AddComponent<PhysicalMissile>();
            shot.origin=shot.last=from;shot.heading=direction.normalized;if(shot.heading.sqrMagnitude<.1f)shot.heading=Vector2.right;
            shot.range=range;shot.life=seconds;shot.damage=damage;shot.source=owner;shot.motion=motion;shot.radius=radius;shot.remaining=pierce;shot.spin=spin;
            go.transform.position=from;go.transform.localScale=Vector3.one*size;
            shot.sprite=go.AddComponent<SpriteRenderer>();shot.sprite.sprite=art;shot.sprite.sortingLayerName="Projectiles";shot.sprite.sortingOrder=20;
            if(!material)material=new Material(Shader.Find("Sprites/Default"));
            var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=material;trail.time=.13f;trail.startWidth=radius*.7f;trail.endWidth=0;trail.startColor=new Color(.83f,.86f,.89f,.45f);trail.endColor=new Color(.83f,.86f,.89f,0);trail.sortingLayerName="Projectiles";trail.sortingOrder=19;trail.minVertexDistance=.15f;
            if(motion==PhysicalMotion.Chain) {shot.chain=new GameObject("鉄球の鎖").AddComponent<LineRenderer>();shot.chain.transform.SetParent(go.transform);shot.chain.sharedMaterial=material;shot.chain.positionCount=17;shot.chain.startWidth=shot.chain.endWidth=.085f;shot.chain.startColor=shot.chain.endColor=new Color(.5f,.54f,.58f);shot.chain.sortingLayerName="Projectiles";shot.chain.sortingOrder=18;}
            return shot;
        }
        void Update()
        {
            if(GameManager.Instance && !GameManager.Instance.IsPlaying || source && !source.Alive){Destroy(gameObject);return;}
            age+=Time.deltaTime;if(age>=life){Destroy(gameObject);return;}
            float t=age/life;Vector2 side=new Vector2(-heading.y,heading.x),point=origin;
            switch(motion) {
                case PhysicalMotion.Straight:point=origin+heading*range*t;break;
                case PhysicalMotion.Lob:point=origin+heading*range*t+side*Mathf.Sin(t*Mathf.PI)*range*.08f;break;
                case PhysicalMotion.Return:point=Vector2.Lerp(origin,source?(Vector2)source.transform.position:origin,t)+heading*range*Mathf.Sin(t*Mathf.PI);break;
                case PhysicalMotion.Spiral:point=origin+(Vector2)(Quaternion.Euler(0,0,t*240)*heading)*range*Mathf.Sin(t*Mathf.PI);break;
                case PhysicalMotion.Chain:point=(source?(Vector2)source.transform.position:origin)+(Vector2)(Quaternion.Euler(0,0,t*480)*heading)*range*Mathf.Min(1,t*8,(1-t)*8);break;
                case PhysicalMotion.Ricochet:point=last+heading*(range/life)*Time.deltaTime;break;
            }
            transform.position=point;transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg+spin*age);
            if(chain) {Vector2 anchor=source?source.transform.position:origin,d=point-anchor,normal=new Vector2(-d.y,d.x).normalized;for(int i=0;i<17;i++)chain.SetPosition(i,Vector2.Lerp(anchor,point,i/16f)+normal*(i==0||i==16?0:i%2==0?.04f:-.04f));}
            Hit(point,t);
            // 鎖と飛翔の演出は戦闘用の乱数を消費しない。
            if(age>=nextDust && spin!=0) {nextDust=age+.12f;CombatFx.Stroke(new Vector3[]{last,point},new Color(.78f,.8f,.82f,.25f),.08f,.15f);}
            last=point;
        }
        void Hit(Vector2 point,float t)
        {
            foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if(!enemy || !enemy.Alive)continue;
                bool repeats=motion==PhysicalMotion.Chain||motion==PhysicalMotion.Spiral||motion==PhysicalMotion.Return;
                if(hitAt.TryGetValue(enemy,out float when) && (!repeats || age-when<.42f))continue;
                Vector2 delta=point-last;float projection=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot((Vector2)enemy.transform.position-last,delta)/delta.sqrMagnitude):0;
                float width=radius+(enemy.IsBoss?1f:.38f);
                if(Vector2.Distance(enemy.transform.position,last+delta*projection)>width)continue;
                hitAt[enemy]=age;
                if(source && source.IsLocal && damage>0) {
                    enemy.Damage(damage,heading,source,CombatElement.None);LocalHits++;
                    if(Vector2.Distance(source.transform.position,enemy.transform.position)<4)source.GrantShield(source.Maximum*.035f,.8f);
                }
                for(int i=0;i<3;i++) {Vector2 d=Quaternion.Euler(0,0,i*120+age*90)*Vector2.right;CombatMote.Create(point,d*3,new Color(.95f,.89f,.7f),new Vector2(.06f,.25f),.2f);}
                if(motion==PhysicalMotion.Ricochet) {
                    EnemyHealth nearest=null;float best=100;
                    foreach(var other in EnemyHealth.Active)if(other&&other.Alive&&!hitAt.ContainsKey(other)){float distance=Vector2.Distance(point,other.transform.position);if(distance<best){best=distance;nearest=other;}}
                    heading=nearest?((Vector2)nearest.transform.position-point).normalized:-heading;
                }
                if(--remaining<=0){Destroy(gameObject);return;}
            }
        }
    }
}
