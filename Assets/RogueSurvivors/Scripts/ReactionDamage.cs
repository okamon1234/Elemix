using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // Team-shared seed, one per target. Secondary damage cannot generate seeds.
    public sealed class ReactionDamage : MonoBehaviour
    {
        float burnDamage,burnAt;
        int burnTicks;
        PlayerHealth burnSource;
        public float BloomDamage { get; private set; }
        public float BloomRemaining { get; private set; }
        public int BloomStage { get; private set; }
        public PlayerHealth BloomOwner { get; private set; }
        SpriteRenderer seed;
        public Vector3 CaptureBloom() => new Vector3(BloomDamage,BloomRemaining,BloomStage);
        public void RestoreBloom(Vector3 state,PlayerHealth owner) { BloomDamage=Mathf.Max(0,state.x);BloomRemaining=Mathf.Max(0,state.y);BloomStage=Mathf.Clamp(Mathf.RoundToInt(state.z),0,3);BloomOwner=owner; }
        public void Begin(float damage,PlayerHealth source,bool bloom)
        {
            if(bloom) {
                if(BloomStage!=0)return;
                BloomDamage=damage;BloomOwner=source;BloomRemaining=3;BloomStage=1;
            } else { burnDamage=Mathf.Max(burnDamage,damage*.3f);burnSource=source;burnTicks=3;burnAt=Time.time+.6f; }
        }
        public bool TryCatalyze(CombatElement element,float amount,PlayerHealth source)
        {
            var sync=GetComponent<NetworkEnemySync>();
            if(sync && !sync.IsAuthority || BloomStage!=1 || (element!=CombatElement.Fire && element!=CombatElement.Lightning))return false;
            BloomStage=element==CombatElement.Lightning?2:3;
            BloomDamage=(BloomDamage+amount)*.5f;
            BloomOwner=source?source:BloomOwner;BloomRemaining=BloomStage==2?.32f:.28f;
            int reaction=BloomStage==2?14:15;
            if(sync && sync.IsNetworked)sync.BroadcastReaction(reaction);
            else GetComponent<ElementReaction>()?.Show(reaction);
            return true;
        }
        void Update()
        {
            var health=GetComponent<EnemyHealth>();var sync=GetComponent<NetworkEnemySync>();
            bool authority=!sync || sync.IsAuthority;
            if(!health || !health.Alive) { if(seed)seed.enabled=false;return; }
            UpdateVisual();
            if(!authority)return;
            if(GameManager.Instance && !GameManager.Instance.IsPlaying)return;
            if(burnTicks>0 && Time.time>=burnAt) {burnTicks--;burnAt=Time.time+.6f;health.ReceiveSecondary(burnDamage,burnSource);}
            if(BloomStage==0)return;
            BloomRemaining-=Time.deltaTime;
            if(BloomRemaining>0)return;
            float damage=BloomDamage;int stage=BloomStage;var owner=BloomOwner;
            BloomStage=0;BloomRemaining=0;BloomDamage=0;
            if(stage==2) {health.ReceiveSecondary(damage*2.2f,owner);return;}
            float radius=stage==3?3.6f:2.8f,multiplier=stage==3?1.55f:1;
            foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active))
                if(enemy && enemy.Alive && Vector2.Distance(transform.position,enemy.transform.position)<=radius)enemy.ReceiveSecondary(damage*multiplier,owner);
        }
        void UpdateVisual()
        {
            if(!seed && BloomStage>0) {
                var go=new GameObject("開花の種：雷・炎で起爆");go.transform.SetParent(transform,false);seed=go.AddComponent<SpriteRenderer>();
                seed.sprite=SeedSprite();seed.sortingLayerName="Projectiles";seed.sortingOrder=35;
            }
            if(!seed)return;
            seed.enabled=BloomStage==1;
            seed.transform.localPosition=new Vector3(0,.8f+Mathf.Sin(Time.time*4)*.12f,0);
            seed.transform.localScale=Vector3.one*(.6f+.06f*Mathf.Sin(Time.time*7));
        }
        static Sprite art;
        static Sprite SeedSprite()
        {
            if(art)return art;
            var t=new Texture2D(20,28,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            for(int y=0;y<28;y++)for(int x=0;x<20;x++) {
                float dx=(x-9.5f)/8,dy=(y-12)/11f;bool inside=dx*dx+dy*dy<1;
                t.SetPixel(x,y,inside?(Mathf.Abs(dx)<.2f?new Color(1,1,.55f):new Color(.35f,.85f,.2f)):Color.clear);
            }
            t.Apply();art=Sprite.Create(t,new Rect(0,0,20,28),new Vector2(.5f,.5f),28);return art;
        }
    }
}
