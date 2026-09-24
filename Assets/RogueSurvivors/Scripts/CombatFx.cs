using UnityEngine;
namespace RogueSurvivors
{
    // 色だけに頼らず、輪・破片・稲妻・花弁で反応を区別する短命な演出。
    public sealed class CombatFx : MonoBehaviour
    {
        static Material material;
        LineRenderer line; Vector3[] points; Color color; float age, life; Vector2 drift; float expansion;
        public static CombatFx Stroke(Vector3[] points, Color tint, float width, float seconds, bool loop=false, float grow=0, Vector2 velocity=default)
        {
            var fx=new GameObject("戦闘エフェクト").AddComponent<CombatFx>();
            fx.points=points; fx.color=tint; fx.life=seconds; fx.expansion=grow; fx.drift=velocity;
            if(!material) material=new Material(Shader.Find("Sprites/Default"));
            fx.line=fx.gameObject.AddComponent<LineRenderer>(); fx.line.sharedMaterial=material;
            fx.line.sortingLayerName="Projectiles"; fx.line.sortingOrder=15; fx.line.useWorldSpace=false;
            fx.line.positionCount=points.Length; fx.line.SetPositions(points); fx.line.loop=loop;
            fx.line.startWidth=fx.line.endWidth=width; fx.line.startColor=fx.line.endColor=tint; return fx;
        }
        public static void Ring(Vector2 center,float radius,Color color,float life=.45f,float grow=1,float width=.10f,int sides=40)
        {
            var points=new Vector3[sides]; for(int i=0;i<sides;i++) { float a=i*Mathf.PI*2/sides; points[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius; }
            Stroke(points,color,width,life,true,grow).transform.position=center;
        }
        public static void Shard(Vector2 center,Vector2 velocity,Color color,float size=.35f,float life=.45f)
        {
            CombatMote.Create(center,velocity,color,new Vector2(size*.55f,size),life);
            if(velocity.sqrMagnitude>1) {
                Vector2 tail=-velocity.normalized*size*1.6f;
                var trail=Stroke(new Vector3[]{tail,Vector3.zero},new Color(color.r,color.g,color.b,.45f),size*.3f,life,false,0,velocity);
                trail.transform.position=center;
            }
        }
        public static void Reaction(int kind,CombatElement spread,Vector2 point)
        {
            Color tint=kind==13?ElementWeapon.ColorFor(spread):kind==6?ElementWeapon.ColorFor(CombatElement.Ice):kind==5||kind==12?new Color(.7f,.55f,1):kind==7?new Color(1,.75f,1):kind==8||kind==10?new Color(.45f,1,.3f):kind==11?new Color(1,.85f,.35f):new Color(1,.48f,.18f);
            Ring(point,.65f,tint,.6f,3,.14f,kind==11?6:40);
            if(kind==13) for(int arc=0;arc<3;arc++) {
                var points=new Vector3[20]; for(int i=0;i<points.Length;i++) {float a=arc*Mathf.PI*2/3+i*.12f; float r=.3f+i*.07f; points[i]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r);}
                Stroke(points,tint,.12f,.65f,false,1.8f).transform.position=point;
            }
            else if(kind==5||kind==12) for(int ray=0;ray<5;ray++) {
                Vector2 direction=Quaternion.Euler(0,0,ray*72)*Vector2.right, normal=new Vector2(-direction.y,direction.x);
                var points=new Vector3[7]; for(int i=0;i<7;i++) points[i]=direction*i*.35f+normal*(i%2==0?.18f:-.18f);
                Stroke(points,tint,.12f,.35f).transform.position=point;
            }
            else for(int i=0;i<8;i++) { Vector2 direction=Quaternion.Euler(0,0,i*45)*Vector2.right; Shard(point+direction*.3f,direction*(kind==7?-2:3),i%2==0?tint:Color.white,kind==6?.55f:.3f,.55f); }
            if(kind==7) { Ring(point,2,new Color(.4f,.15f,.7f),.5f,-.8f,.25f); Ring(point,.2f,Color.white,.45f,6,.18f); }
        }
        void Update()
        {
            age+=Time.deltaTime; if(age>=life) { Destroy(gameObject); return; }
            transform.position+=(Vector3)drift*Time.deltaTime;
            transform.localScale=Vector3.one*Mathf.Max(.03f,1+expansion*age/life);
            Color fade=color; fade.a*=1-age/life; line.startColor=line.endColor=fade;
        }
    }
}
