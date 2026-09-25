using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // しなって伸びる鞭。描画した各線分で判定し、見た目と当たり方を一致させる。
    public sealed class PhysicalWhipStrike : MonoBehaviour
    {
        Vector2 origin,heading;float reach,damage,age;PlayerHealth owner;bool metal;LineRenderer line;Vector3[] points=new Vector3[24];
        readonly HashSet<EnemyHealth> hit=new HashSet<EnemyHealth>();static Material material;
        public static void Create(Vector2 origin,Vector2 heading,float reach,float damage,PlayerHealth owner,bool metal)
        {
            var fx=new GameObject(metal?"鎖鎌のなぎ払い":"鞭のしなり").AddComponent<PhysicalWhipStrike>();fx.origin=origin;fx.heading=heading;fx.reach=reach;fx.damage=damage;fx.owner=owner;fx.metal=metal;
            if(!metal)WeaponCastVisual.Show("whip",origin,heading);
            if(!material)material=new Material(Shader.Find("Sprites/Default"));fx.line=fx.gameObject.AddComponent<LineRenderer>();fx.line.sharedMaterial=material;fx.line.positionCount=24;fx.line.sortingLayerName="Projectiles";fx.line.sortingOrder=21;fx.line.startWidth=metal?.15f:.13f;fx.line.endWidth=metal?.1f:.045f;
        }
        void Update()
        {
            if(owner&&!owner.Alive || GameManager.Instance&&!GameManager.Instance.IsPlaying){Destroy(gameObject);return;}
            age+=Time.deltaTime;if(age>.42f){Destroy(gameObject);return;}float t=age/.42f;Vector2 side=new Vector2(-heading.y,heading.x);
            for(int i=0;i<points.Length;i++){float k=i/(float)(points.Length-1);points[i]=origin+heading*(reach*k*Mathf.Min(1,t*5))+side*(Mathf.Sin(k*Mathf.PI*2-t*6)*k*1.4f)*(1-t*.7f);}
            line.SetPositions(points);Color c=metal?new Color(.85f,.88f,.91f):new Color(.75f,.55f,.34f);c.a=Mathf.Min(1,(1-t)*4);line.startColor=line.endColor=c;
            if(!owner||!owner.IsLocal||damage<=0)return;
            foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if(!enemy||!enemy.Alive||hit.Contains(enemy))continue;Vector2 point=enemy.transform.position;
                for(int i=1;i<points.Length;i++) {Vector2 a=points[i-1],delta=points[i]-points[i-1];float along=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
                    if(Vector2.Distance(point,a+delta*along)>(enemy.IsBoss?1:.65f))continue;
                    hit.Add(enemy);enemy.Damage(damage,heading,owner);if(Vector2.Distance(point,owner.transform.position)<4)owner.GrantShield(owner.Maximum*.035f,.8f);
                    CombatMote.Create(point,Vector2.zero,new Color(.95f,.89f,.7f),new Vector2(.25f,.25f),.15f,3);break;
                }
            }
        }
    }
}
