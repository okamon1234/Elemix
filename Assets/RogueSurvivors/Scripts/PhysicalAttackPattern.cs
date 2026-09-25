using System.Collections;
using UnityEngine;
namespace RogueSurvivors
{
    // 合体は0〜9、通常の物理武器は20〜25。同じ入口を通信・描画検証にも使う。
    public sealed class PhysicalAttackPattern : MonoBehaviour
    {
        PlayerHealth owner;Vector2 origin,heading;int style,level;float power;
        public static PhysicalAttackPattern Create(int style,Vector2 from,Vector2 target,int level,float damage,PlayerHealth owner)
        {
            var cast=new GameObject("物理攻撃パターン").AddComponent<PhysicalAttackPattern>();cast.style=style;cast.origin=from;cast.heading=(target-from).normalized;if(cast.heading.sqrMagnitude<.1f)cast.heading=Vector2.right;
            cast.level=level;cast.power=damage;cast.owner=owner;cast.StartCoroutine(cast.Play());return cast;
        }
        void Shoot(int art,Vector2 direction,PhysicalMotion motion,float range,float seconds,float size,float radius,int pierce=20,float spin=600)
        {
            if(owner&&!owner.Alive)return;
            var sprite=art<10?PhysicalFusionArt.Get(art):art==24?Resources.Load<Sprite>("RogueSurvivors/Art/spear"):WeaponArt.Get(art==20?0:art==21?1:6);
            PhysicalMissile.Create(sprite,origin,direction,range,seconds,power,owner,motion,size,radius,pierce,spin);
        }
        IEnumerator Play()
        {
            if(style==0) {
                PhysicalSlash.Create(0,origin,heading,4.3f+level*.08f,power,owner);
                yield return new WaitForSeconds(.28f);
                if(!owner||owner.Alive)PhysicalSlash.Create(0,origin,heading,4.6f+level*.08f,power,owner,true);
            } else if(style==1) {
                int count=3+level/3;for(int i=0;i<count;i++)Shoot(1,Quaternion.Euler(0,0,i*360f/count)*heading,PhysicalMotion.Spiral,4.5f+level*.12f,1.35f,1.3f,.6f);
            } else if(style==2||style==20) {
                int count=style==2?6+level/2:2+level/3;
                // 籠手を表示し、その先から短剣が順番に発射される。
                if(style==2) {var glove=new GameObject("投刃グローブ本体").AddComponent<SpriteRenderer>();glove.sprite=PhysicalFusionArt.Get(2);glove.sortingLayerName="Projectiles";glove.sortingOrder=20;glove.transform.position=origin+heading*.55f;glove.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg);glove.transform.localScale=Vector3.one*.7f;Destroy(glove.gameObject,.65f);}
                for(int i=0;i<count;i++) {
                    Vector2 d=Quaternion.Euler(0,0,(i%3-1)*(style==2?5:7))*heading;
                    Shoot(20,d,PhysicalMotion.Straight,style==2?12:8,.65f,style==2?1.3f:1,.22f,style==2?8:2+level/2,0);
                    yield return new WaitForSeconds(style==2?.045f:.065f);
                }
            } else if(style==3) {
                PhysicalSlash.Create(3,origin,heading,4.2f+level*.08f,power,owner);
                yield return new WaitForSeconds(.28f);Shoot(3,heading,PhysicalMotion.Straight,9,.45f,1.7f,.7f,30,0);
            } else if(style==4||style==21) {
                int count=style==4?2+level/5:1+level/4;
                for(int i=0;i<count;i++) {Vector2 d=Quaternion.Euler(0,0,(i-(count-1)*.5f)*28)*heading;Shoot(style==4?4:21,d,PhysicalMotion.Lob,style==4?10:7,.9f,style==4?1.6f:1.15f+level*.045f,style==4?.7f:.42f,12,style==4?680:520);}
            } else if(style==5) {
                int count=level>=5?2:1;for(int i=0;i<count;i++)Shoot(5,Quaternion.Euler(0,0,i*180)*heading,PhysicalMotion.Chain,4+level*.1f,1.85f,1.2f,.75f,40,360);
            } else if(style==6) {
                Shoot(6,heading,PhysicalMotion.Ricochet,24,1.8f,1.35f,.7f,5+level/2,540);
                if(owner&&owner.IsLocal)owner.GrantShield(owner.Maximum*.08f,1.5f);
            } else if(style==7||style==22) {
                int count=style==7?3+level/3:1+level/5;
                for(int i=0;i<count;i++) {Vector2 d=Quaternion.Euler(0,0,(i-(count-1)*.5f)*(style==7?50:28))*heading;Shoot(style==7?7:22,d,PhysicalMotion.Return,style==7?8.5f:6,1.4f,style==7?1.35f:1.3f,.6f,24,720);}
            }
            if(style==8||style==23) {
                PhysicalWhipStrike.Create(origin,heading,style==8?7:4.5f+level*.12f,power,owner,style==8);
                if(style==8) {yield return new WaitForSeconds(.25f);Shoot(8,heading,PhysicalMotion.Return,7,1.25f,1.5f,.6f,20,540);}
            } else if(style==9||style==24) {
                if(style==9) {
                    var launcher=new GameObject("連射ボウガン本体").AddComponent<SpriteRenderer>();launcher.sprite=PhysicalFusionArt.Get(9);launcher.sortingLayerName="Projectiles";launcher.sortingOrder=20;
                    launcher.transform.position=origin+heading*.6f;launcher.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg);Destroy(launcher.gameObject,.5f);
                }
                int count=style==9?3+level/3:1+level/3;
                for(int volley=0;volley<(style==9?2:1);volley++) {
                    for(int i=0;i<count;i++)Shoot(24,Quaternion.Euler(0,0,(i-(count-1)*.5f)*9)*heading,PhysicalMotion.Straight,12,.6f,style==9?1.45f:1,.25f,4+level/2,0);
                    yield return new WaitForSeconds(.16f);
                }
            } else if(style==25) {int count=1+level/4;for(int i=0;i<count;i++)Shoot(7,Quaternion.Euler(0,0,(i-(count-1)*.5f)*30)*heading,PhysicalMotion.Return,7,1.2f,.7f,.38f,15,620);}
            Destroy(gameObject);
        }
    }
}
