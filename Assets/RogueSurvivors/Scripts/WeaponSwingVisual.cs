using UnityEngine;
namespace RogueSurvivors
{
    // ダメージとは独立した振り抜き。ネットワーク側も同じ入口から再生する。
    public sealed class WeaponSwingVisual : MonoBehaviour
    {
        Vector2 origin, heading; float age, life, reach, angle; int kind; SpriteRenderer sprite;
        public static void Create(int kind,Vector2 origin,Vector2 target)
        {
            var fx=new GameObject("武器の振り抜き").AddComponent<WeaponSwingVisual>();
            fx.kind=kind; fx.origin=origin; fx.heading=(target-origin).normalized;
            if(fx.heading.sqrMagnitude<.1f) fx.heading=Vector2.right;
            fx.reach=Mathf.Clamp(Vector2.Distance(origin,target),1.2f,3.5f); fx.life=kind==2?.36f:.25f;
            fx.angle=Mathf.Atan2(fx.heading.y,fx.heading.x)*Mathf.Rad2Deg;
            fx.sprite=fx.gameObject.AddComponent<SpriteRenderer>(); fx.sprite.sortingLayerName="Projectiles"; fx.sprite.sortingOrder=18;
            fx.sprite.sprite=WeaponArt.Get(kind);
            fx.transform.localScale=Vector3.one*(kind==2?1.5f:1.15f);
            Color tint=kind==2||kind==4?new Color(1,.68f,.27f):new Color(.65f,.88f,1);
            if(kind==0||kind==5||kind==7) {
                for(int i=-1;i<=1;i++) {
                    Vector2 side=new Vector2(-fx.heading.y,fx.heading.x)*i*.18f;
                    CombatFx.Stroke(new Vector3[]{origin+side,target+side},new Color(tint.r,tint.g,tint.b,.65f),i==0?.16f:.045f,fx.life);
                }
            } else {
                var arc=new Vector3[22]; float sweep=kind==6?300:130;
                for(int i=0;i<arc.Length;i++) {Vector2 d=Quaternion.Euler(0,0,fx.angle-sweep*.5f+sweep*i/(arc.Length-1))*Vector2.right;arc[i]=origin+d*fx.reach;}
                CombatFx.Stroke(arc,new Color(tint.r,tint.g,tint.b,.22f),.55f,fx.life);
                CombatFx.Stroke(arc,tint,.10f,fx.life);
            }
            for(int i=0;i<5;i++) {Vector2 d=Quaternion.Euler(0,0,fx.angle-65+i*32.5f)*Vector2.right;CombatMote.Create(target,d*3.5f,tint,new Vector2(.12f,.4f),.28f);}
            CombatMote.Create(target,Vector2.zero,Color.white,new Vector2(.48f,.48f),.18f,3);
            if(kind==2||kind==4) CombatFx.Ring(target,.35f,tint,.35f,3,.13f,kind==2?8:24);
            fx.Pose(0);
        }
        void Pose(float t)
        {
            bool thrust=kind==0||kind==5||kind==7; float eased=1-Mathf.Pow(1-t,3);
            float rotation=angle+(thrust?0:Mathf.Lerp(kind==6?-150:-70,kind==6?150:70,eased));
            Vector2 d=Quaternion.Euler(0,0,rotation)*Vector2.right;
            transform.position=origin+d*reach*(thrust?Mathf.Lerp(.25f,.85f,Mathf.Sin(t*Mathf.PI)):.72f);
            transform.rotation=Quaternion.Euler(0,0,rotation); sprite.color=new Color(1,1,1,Mathf.Min(1,(1-t)*4));
        }
        void Update() {age+=Time.deltaTime;if(age>=life){Destroy(gameObject);return;}Pose(age/life);}
    }
}
