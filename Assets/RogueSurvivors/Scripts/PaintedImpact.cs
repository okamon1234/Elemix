using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // 演出には当たり判定を付けず、味方エフェクトは敵の予告より後ろへ置く。
    public sealed class PaintedImpact : MonoBehaviour
    {
        const int Limit=72;
        static readonly Queue<PaintedImpact> pool=new Queue<PaintedImpact>();
        static int active;
        SpriteRenderer sprite;float age,life,size,spin;bool counted;
        public static int ActiveCount=>active;
        public static void Show(int art,Vector2 point,float diameter,float seconds=.38f,float angle=0,float rotation=0)
        {
            if(active>=Limit)return;
            var picture=BattleArt.Effect(art);if(!picture)return;
            PaintedImpact fx=null;while(pool.Count>0 && !fx)fx=pool.Dequeue();
            if(!fx){fx=new GameObject("着弾の描画").AddComponent<PaintedImpact>();fx.sprite=fx.gameObject.AddComponent<SpriteRenderer>();fx.sprite.sortingLayerName="Projectiles";fx.sprite.sortingOrder=22;}
            fx.gameObject.SetActive(true);fx.counted=true;active++;
            fx.age=0;fx.life=Mathf.Max(.05f,seconds);fx.size=Mathf.Clamp(diameter,.15f,4.4f);fx.spin=rotation;
            fx.sprite.sprite=picture;fx.sprite.color=new Color(1,1,1,.7f);
            fx.transform.SetPositionAndRotation(point,Quaternion.Euler(0,0,angle));fx.transform.localScale=Vector3.one*fx.size*.55f;
        }
        void Update()
        {
            age+=Time.deltaTime;float t=age/life;
            if(t>=1){gameObject.SetActive(false);pool.Enqueue(this);return;}
            transform.localScale=Vector3.one*size*Mathf.Lerp(.55f,1.08f,Mathf.Sqrt(t));
            transform.Rotate(0,0,spin*Time.deltaTime);
            sprite.color=new Color(1,1,1,.7f*(1-t*t));
        }
        void OnDisable(){if(counted){active=Mathf.Max(0,active-1);counted=false;}}
    }
}
