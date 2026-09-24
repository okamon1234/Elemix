using System.Collections;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class BossAttackDirector : MonoBehaviour
    {
        BossAI boss;
        void Awake() => boss=GetComponent<BossAI>();
        public void Execute(BossAttack attack, Vector2 origin, Vector2 target, bool phaseTwo, float damage, int brokenMask)
        {
            Color color=BossCatalog.Colors[(int)boss.Kind];
            float angle=Mathf.Atan2(target.y-origin.y,target.x-origin.x)*Mathf.Rad2Deg;
            int fewer=(brokenMask&2)!=0?2:0;
            float life=(brokenMask&4)!=0?.7f:1;
            float hit=damage*((brokenMask&1)!=0?.8f:1);
            switch(attack) {
                case BossAttack.Shockwave: StartCoroutine(Rings(origin,phaseTwo?3:2,16-fewer,angle,4.2f,hit*.75f,.42f)); break;
                case BossAttack.CrossSlam:
                    Lane(origin-Vector2.right*13,origin+Vector2.right*13,.9f,1,1.2f*life,hit,color);
                    Lane(origin-Vector2.up*13,origin+Vector2.up*13,.9f,1,1.2f*life,hit,color); break;
                case BossAttack.PoisonPools:
                    Pool(target,phaseTwo?2.4f:2,1.3f,4*life,hit*.6f,color);
                    Pool(BossArena.Clamp(target+Vector2.right*3,3),1.8f,1.5f,3*life,hit*.6f,color);
                    Pool(BossArena.Clamp(target+Vector2.left*3,3),1.8f,1.5f,3*life,hit*.6f,color); break;
                case BossAttack.WebFan: Fan(origin,angle,7-fewer,80,4.8f,hit*.75f); break;
                case BossAttack.WebGrid:
                    for(int i=-1;i<=1;i++) { Vector2 offset=Vector2.right*i*5; Lane(target+offset-Vector2.up*9,target+offset+Vector2.up*9,.65f,1.3f,3.2f*life,hit*.65f,color); }
                    Lane(target-Vector2.right*10,target+Vector2.right*10,.65f,1.6f,2.9f*life,hit*.65f,color); break;
                case BossAttack.NeedleStorm: StartCoroutine(Fans(origin,angle,9-fewer,100,5.7f,hit*.7f)); break;
                case BossAttack.FireFan: Fan(origin,angle,phaseTwo?11-fewer:7-fewer,75,phaseTwo?6.5f:5.5f,hit*.85f); break;
                case BossAttack.WingRing: StartCoroutine(Rings(origin,2,18-fewer,angle,4.3f,hit*.75f,.55f)); break;
                case BossAttack.Spiral: StartCoroutine(Spiral(origin,angle,hit*.7f,fewer>0?3:4)); break;
                case BossAttack.LightningLanes:
                    for(int i=-2;i<=2;i++) { float x=Mathf.Clamp(target.x+i*4.8f,-25,25); Lane(new Vector2(x,-16),new Vector2(x,16),.8f,1.25f+Mathf.Abs(i)*.18f,.65f,hit,color); } break;
                case BossAttack.Roots:
                    for(int i=0;i<4;i++) Pool(BossArena.Clamp(target+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad))*i*2.7f),1.7f,1.1f+i*.32f,2*life,hit*.8f,color); break;
                case BossAttack.SeedRing: StartCoroutine(Rings(origin,2,14-fewer,angle,3.8f,hit*.75f,.8f)); break;
                case BossAttack.RootMaze:
                    for(int i=-2;i<=2;i++) {
                        float y=Mathf.Clamp(target.y+i*4,-15,15);
                        // A staggered five-unit opening remains in every row.
                        float gap=Mathf.Clamp(target.x+(i%2==0?-3:3),-16,16);
                        Lane(new Vector2(-26,y),new Vector2(gap-2.5f,y),.6f,1.4f+Mathf.Abs(i)*.15f,3.8f*life,hit*.65f,color);
                        Lane(new Vector2(gap+2.5f,y),new Vector2(26,y),.6f,1.4f+Mathf.Abs(i)*.15f,3.8f*life,hit*.65f,color);
                    } break;
                case BossAttack.TwinRings:
                    StartCoroutine(Rings(origin+Vector2.left*3,3,12-fewer,angle,4.2f,hit*.7f,.6f));
                    StartCoroutine(Rings(origin+Vector2.right*3,3,12-fewer,-angle+15,4.2f,hit*.7f,.6f)); break;
            }
        }
        void Pool(Vector2 point,float radius,float delay,float life,float damage,Color color) => BossHazard.Create(boss,point,point,radius,delay,life,damage,color);
        void Lane(Vector2 from,Vector2 to,float width,float delay,float life,float damage,Color color) => BossHazard.Create(boss,from,to,width,delay,life,damage,color);
        bool CanFire => boss && boss.GetComponent<EnemyHealth>().Alive && !boss.IsTransforming && GameManager.Instance && GameManager.Instance.IsPlaying;
        void Shot(Vector2 origin,float angle,float speed,float damage)
        {
            if(!CanFire || !boss.hostileBullet) return;
            Vector2 direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
            var bullet=Instantiate(boss.hostileBullet,origin+direction*1.6f,Quaternion.identity);
            bullet.Launch(direction,damage,speed,0,true);
        }
        void Fan(Vector2 origin,float angle,int count,float spread,float speed,float damage) { for(int i=0;i<count;i++) Shot(origin,angle-spread*.5f+spread*i/Mathf.Max(1,count-1),speed,damage); }
        IEnumerator Fans(Vector2 origin,float angle,int count,float spread,float speed,float damage) { for(int n=0;n<3;n++) { Fan(origin,angle+(n-1)*12,count,spread,speed,damage); yield return new WaitForSeconds(.38f); } }
        IEnumerator Rings(Vector2 origin,int waves,int count,float angle,float speed,float damage,float interval) { for(int n=0;n<waves;n++) { for(int i=0;i<count;i++) Shot(origin,angle+n*180f/count+i*360f/count,speed+n*.4f,damage); yield return new WaitForSeconds(interval); } }
        IEnumerator Spiral(Vector2 origin,float angle,float damage,int arms) { for(int n=0;n<12;n++) { for(int i=0;i<arms;i++) Shot(origin,angle+n*13+i*360f/arms,5.2f,damage); yield return new WaitForSeconds(.14f); } }
    }
}
