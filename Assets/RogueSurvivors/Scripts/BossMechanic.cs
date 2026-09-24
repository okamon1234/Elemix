using UnityEngine;
namespace RogueSurvivors
{
    // 予告と判定を同じ座標から描く。各端末で自分のプレイヤーだけを判定する。
    public sealed class BossMechanic : MonoBehaviour
    {
        BossAI boss; BossAttack attack; Vector2 center; Color color; float damage, age, delay=1.4f, duration=3.6f, baseAngle, nextHit, nextVisual; int phase;
        LineRenderer shape, directionGuide; static Material material;
        public static void Create(BossAI owner,BossAttack attack,Vector2 origin,Vector2 target,int phase,float damage,Color color)
        {
            if(attack==BossAttack.VenomTrail || attack==BossAttack.StormHunt) {
                Vector2 heading=(target-origin).normalized,side=new Vector2(-heading.y,heading.x);
                int count=phase>=3?7:4;
                for(int i=0;i<count;i++) {
                    Vector2 point=attack==BossAttack.VenomTrail?target+heading*i*2.1f+side*(i%2==0?1:-1):target+new Vector2(Mathf.Cos(i*2.4f),Mathf.Sin(i*2.4f))*(i==0?0:2+i*.55f);
                    BossHazard.Create(owner,BossArena.Clamp(point,2),BossArena.Clamp(point,2),attack==BossAttack.StormHunt?1.7f:1.3f,1.15f+i*.35f,attack==BossAttack.StormHunt?.45f:3,damage*(attack==BossAttack.StormHunt?1:.6f),color);
                }
                return;
            }
            var fx=new GameObject("ボスの固有攻撃："+BossCatalog.AttackName(attack)).AddComponent<BossMechanic>();
            fx.boss=owner; fx.attack=attack; fx.center=attack==BossAttack.WebCage?BossArena.Clamp(target,9):origin;
            fx.phase=phase; fx.damage=damage; fx.color=color; fx.baseAngle=Mathf.Atan2(target.y-origin.y,target.x-origin.x)*Mathf.Rad2Deg;
            if(attack==BossAttack.WebCage) {fx.duration=4.2f; fx.delay=1.8f;}
            if(attack==BossAttack.Harvest) fx.duration=4.2f;
            if(!material) material=new Material(Shader.Find("Sprites/Default"));
            fx.shape=fx.MakeLine("攻撃範囲",.16f,2); fx.directionGuide=fx.MakeLine("進行方向",.07f,1);
            fx.Draw(0,false);
        }
        LineRenderer MakeLine(string name,float width,int order)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(transform,false);line.sharedMaterial=material;
            line.sortingLayerName="Items";line.sortingOrder=order;line.startWidth=line.endWidth=width; return line;
        }
        bool IsRing => attack==BossAttack.SeismicRing || attack==BossAttack.WebCage || attack==BossAttack.Harvest;
        float Radius(float progress) => attack==BossAttack.SeismicRing?Mathf.Lerp(2,18,progress):attack==BossAttack.WebCage?Mathf.Lerp(8,2.5f,progress):Mathf.Lerp(15,3,progress);
        float Angle(float progress) => baseAngle-(attack==BossAttack.HammerSweep?65:40)+progress*(attack==BossAttack.ThornSpiral?210:attack==BossAttack.HammerSweep?130:110)*(phase>=3?1.15f:1);
        float Length => attack==BossAttack.SweepingBreath?17:attack==BossAttack.ThornSpiral?13:10;
        float GapAngle => baseAngle+90;
        public bool Contains(Vector2 point,float progress)
        {
            Vector2 delta=point-center;
            if(IsRing) {
                if(attack!=BossAttack.Harvest && Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,GapAngle))<32) return false;
                return Mathf.Abs(delta.magnitude-Radius(progress))<.65f;
            }
            Vector2 heading=Quaternion.Euler(0,0,Angle(progress))*Vector2.right;
            float along=Vector2.Dot(delta,heading); if(attack==BossAttack.ThornSpiral) along=Mathf.Abs(along);
            return along>=0 && along<=Length && Mathf.Abs(delta.x*heading.y-delta.y*heading.x)<(attack==BossAttack.SweepingBreath?1.0f:.75f);
        }
        void Draw(float progress,bool active)
        {
            shape.startColor=shape.endColor=active?color:Color.Lerp(color,Color.white,.5f);
            directionGuide.startColor=directionGuide.endColor=new Color(color.r,color.g,color.b,.3f);
            if(IsRing) {
                float radius=Radius(progress), gap=attack==BossAttack.Harvest?0:32;
                shape.positionCount=65;shape.loop=gap==0;shape.startWidth=shape.endWidth=active?.65f:.12f;
                for(int i=0;i<65;i++) { float a=(GapAngle+gap+(360-2*gap)*i/64)*Mathf.Deg2Rad;shape.SetPosition(i,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius); }
                directionGuide.positionCount=0;
                if(!active && gap>0) {Vector2 d=Quaternion.Euler(0,0,GapAngle)*Vector2.right; directionGuide.positionCount=2;directionGuide.SetPosition(0,center+d*(radius-1));directionGuide.SetPosition(1,center+d*(radius+2));}
            } else {
                Vector2 heading=Quaternion.Euler(0,0,Angle(progress))*Vector2.right;
                shape.startWidth=shape.endWidth=active?(attack==BossAttack.SweepingBreath?1.4f:1):.16f; shape.positionCount=2;
                shape.SetPosition(0,attack==BossAttack.ThornSpiral?center-heading*Length:center);shape.SetPosition(1,center+heading*Length);
                directionGuide.positionCount=25;
                for(int i=0;i<25;i++) {Vector2 d=Quaternion.Euler(0,0,Angle(i/24f))*Vector2.right; directionGuide.SetPosition(i,center+d*Length);}
            }
        }
        void EmitVisual(float progress)
        {
            // 判定外や環状攻撃の退避口には装飾を置かない。
            if(IsRing) {
                float gap=attack==BossAttack.Harvest?0:36;
                for(int i=0;i<12;i++) {
                    float a=(GapAngle+gap+(360-2*gap)*(i+.5f)/12)*Mathf.Deg2Rad;
                    Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    Vector2 at=center+d*Radius(progress);
                    int motif=attack==BossAttack.SeismicRing?2:attack==BossAttack.Harvest?1:0;
                    CombatMote.Create(at,Vector2.up*.6f,color,new Vector2(.24f,.5f),.3f,motif,attack==BossAttack.Harvest?90:0);
                }
            } else {
                Vector2 heading=Quaternion.Euler(0,0,Angle(progress))*Vector2.right;
                for(int i=0;i<7;i++) {
                    Vector2 at=center+heading*(1+i*(Length-1)/7);
                    int motif=attack==BossAttack.ThornSpiral?1:attack==BossAttack.HammerSweep?2:3;
                    CombatMote.Create(at,heading*2,color,new Vector2(.3f,.65f),.3f,motif,90);
                    if(attack==BossAttack.ThornSpiral) CombatMote.Create(center-(at-center),-heading*2,color,new Vector2(.3f,.65f),.3f,1,90);
                }
                if(attack==BossAttack.SweepingBreath) CombatFx.Stroke(new Vector3[]{center,center+heading*Length},new Color(1,.95f,.7f,.9f),.16f,.2f);
                if(attack==BossAttack.HammerSweep) CombatMote.Create(center+heading*(Length-.5f),heading*.3f,color,new Vector2(.8f,1.1f),.3f,2);
            }
        }
        void Update()
        {
            if(!boss || !boss.GetComponent<EnemyHealth>().Alive || boss.IsTransforming || !GameManager.Instance || !GameManager.Instance.IsPlaying) {Destroy(gameObject);return;}
            age+=Time.deltaTime;if(age>delay+duration) {Destroy(gameObject);return;}
            bool active=age>=delay;float progress=Mathf.Clamp01((age-delay)/duration);Draw(progress,active);
            if(active && age>=nextVisual) {nextVisual=age+.23f; EmitVisual(progress);}
            var player=PlayerHealth.Local;
            if(active && Time.time>=nextHit && player && player.Alive && Contains(player.transform.position,progress)) {player.Damage(damage);nextHit=Time.time+.8f;}
        }
    }
}
