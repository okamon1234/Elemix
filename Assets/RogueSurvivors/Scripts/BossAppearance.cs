using UnityEngine;
namespace RogueSurvivors
{
    // 4体×3形態のオリジナルの輪郭と形態ごとの放出エフェクト。
    public sealed class BossAppearance : MonoBehaviour
    {
        static readonly Sprite[,] sprites=new Sprite[4,3];
        BossAI boss; SpriteRenderer body; Vector3 originalScale; Quaternion originalRotation; int last=-1; float nextEmber;
        void Start()
        {
            boss=GetComponent<BossAI>(); body=GetComponentInChildren<SpriteRenderer>();
            if(body) { originalScale=body.transform.localScale;originalRotation=body.transform.localRotation; var animator=body.GetComponent<Animator>(); if(animator) animator.enabled=false; }
        }
        void LateUpdate()
        {
            if(!body || !boss) return;
            int phase=boss.Phase-1, key=(int)boss.Kind*3+phase;
            if(key!=last) {
                last=key; body.sprite=GetSprite(boss.Kind,boss.Phase); body.color=Color.white; GetComponent<HitFeedback>()?.RefreshColor();
                if(phase>=1) {
                    Color accent=BossCatalog.Colors[(int)boss.Kind];
                    CombatFx.Ring(transform.position,1,accent,1.2f,5,.18f,phase==2?8:32);
                    for(int i=0;i<12;i++) {Vector2 d=Quaternion.Euler(0,0,i*30)*Vector2.right;CombatMote.Create((Vector2)transform.position+d,d*4,accent,new Vector2(.25f,.8f),1,phase==2?3:0,60);}
                    EffectsService.Instance?.Burst(transform.position,BossCatalog.Colors[(int)boss.Kind]); EffectsService.Instance?.Play("Hurt",.5f); CameraFollow.Instance?.Shake(.25f); HUDController.Instance?.Toast(boss.DisplayName+"："+(phase==2?"第3形態・限界突破":BossCatalog.PhaseNames[(int)boss.Kind])+"！"); }
            }
            if(phase>=1 && Time.time>=nextEmber && GetComponent<EnemyHealth>().Alive) {
                nextEmber=Time.time+(phase==2?.22f:.45f);
                Color accent=BossCatalog.Colors[(int)boss.Kind];
                float a=Time.time*2.3f; Vector2 offset=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(phase==2?1.8f:1.4f);
                CombatMote.Create((Vector2)transform.position+offset,Vector2.up*1.6f,accent,new Vector2(.13f,.35f),.8f,boss.Kind==BossKind.Guardian?1:boss.Kind==BossKind.Golem?2:3,60);
            }
            float pulse=boss.IsTransforming?1+.09f*Mathf.Sin(Time.time*14):1+.012f*Mathf.Sin(Time.time*3);
            body.transform.localScale=originalScale*pulse;
            body.transform.localRotation=originalRotation*(boss.State==BossState.Stagger?Quaternion.Euler(0,0,Mathf.Sin(Time.time*30)*4):Quaternion.identity);
        }
        public static Sprite GetSprite(BossKind kind,bool phaseTwo) => GetSprite(kind,phaseTwo?2:1);
        public static Sprite GetSprite(BossKind kind,int phase)
        {
            bool phaseTwo=phase>=2; int k=(int)kind,p=Mathf.Clamp(phase-1,0,2); if(sprites[k,p]) return sprites[k,p];
            var sheet=Resources.Load<Texture2D>("RogueSurvivors/Art/Bosses/"+kind+"_v6");
            if(sheet) { float cell=sheet.width/3f; sprites[k,p]=Sprite.Create(sheet,new Rect(p*cell,0,cell,sheet.height),new Vector2(.5f,.5f),cell/2f,0,SpriteMeshType.FullRect); return sprites[k,p]; }
            const int size=64; var texture=new Texture2D(size,size,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point, name=BossCatalog.Names[k]+(phaseTwo?"・変身後":"・通常") };
            Color dark=new Color(.08f,.1f,.14f), accent=BossCatalog.Colors[k];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) {
                float dx=x-31.5f,dy=y-31.5f,ax=Mathf.Abs(dx); bool shape=false, detail=false, eye=false;
                Color baseColor=Color.gray;
                switch(kind) {
                    case BossKind.Golem:
                        shape=(ax<13 && dy>-14 && dy<17) || (ax<9 && dy>=17 && dy<26) || (ax>14 && ax<25 && dy>-9 && dy<14) || (ax>4 && ax<13 && dy>-28 && dy<=-14);
                        if(phaseTwo) shape|=ax>12 && ax<29 && dy>11 && dy<22-ax*.15f;
                        baseColor=phaseTwo?new Color(.32f,.24f,.2f):new Color(.43f,.51f,.59f);
                        detail=(ax<7 && dy>-2 && dy<9) || (phaseTwo && ax<12 && Mathf.Abs(dy-(ax*1.6f-10))<1.2f);
                        eye=dy>19 && dy<22 && ax>2 && ax<7; break;
                    case BossKind.Spider:
                        shape=dx*dx/225+dy*dy/324<1 || (ax<10 && dy<-11 && dy>-23);
                        for(int leg=0;leg<4;leg++) { float line=(leg-1.5f)*9+(ax-12)*(leg<2?-.65f:.65f); shape|=ax>9 && ax<30 && Mathf.Abs(dy-line)<(phaseTwo?2.5f:1.7f); }
                        baseColor=phaseTwo?new Color(.5f,.17f,.3f):new Color(.27f,.18f,.38f);
                        detail=ax<9 && Mathf.Abs(dy-5)<10 && Mathf.Abs(ax-Mathf.Abs(dy-5)*.65f)<2;
                        eye=dy>-20 && dy<-15 && (ax<2 || (ax>4 && ax<7));
                        if(phaseTwo) shape|=ax>3 && ax<8 && dy<-22 && dy>-30; break;
                    case BossKind.Dragon:
                        shape=(ax<7 && dy>-23 && dy<20) || (ax<11 && dy>12 && dy<25) || (dy<-17 && ax<4+(dy+28)*.3f && dy>-30);
                        shape|=ax>5 && ax<(phaseTwo?31:26) && dy<19-ax*.2f && dy>(phaseTwo?-17:-5)+ax*.4f ;
                        shape|=ax>7 && ax<12 && dy>22 && dy<30;
                        baseColor=phaseTwo?new Color(.18f,.35f,.63f):new Color(.24f,.52f,.57f);
                        detail=ax<4 && dy>-15 && dy<16 || (phaseTwo && ax>12 && Mathf.Abs(dy-(18-ax*.65f))<1.2f);
                        eye=dy>18 && dy<21 && ax>4 && ax<8; break;
                    case BossKind.Guardian:
                        shape=ax<10 && dy>-20 && dy<12;
                        shape|=dx*dx/625+(dy-14)*(dy-14)/196<1;
                        shape|=dy<-14 && dy>-28 && ax<22 && Mathf.Abs(ax-(-dy-12))<5;
                        shape|=ax>8 && ax<27 && Mathf.Abs(dy-(ax-15)*.7f)<4;
                        if(phaseTwo) shape|=ax>12 && ax<30 && dy>12 && dy<30 && Mathf.Sin(ax*.6f+dy*.3f)>.1f;
                        baseColor=dy>10?(phaseTwo?new Color(.75f,.32f,.13f):new Color(.22f,.46f,.21f)):new Color(.38f,.24f,.13f);
                        detail=ax<7 && dy>-5 && dy<8 && x%5<2;
                        eye=dy>4 && dy<7 && ax>2 && ax<7; break;
                }
                if(phase==3) {
                    // 第3形態は輪郭自体を拡張：冠、鋏、翼端、枝の違いを残す。
                    if(kind==BossKind.Golem) shape|=ax<29 && ax>19 && dy>-15 && dy<22 && (ax<25 || dy>5);
                    if(kind==BossKind.Spider) shape|=ax>14 && ax<31 && dy<-4 && dy>-27 && Mathf.Abs(ax+dy-8)<5;
                    if(kind==BossKind.Dragon) shape|=ax>12 && ax<32 && dy>6 && dy<30 && Mathf.Abs(dy-ax*.6f)<6;
                    if(kind==BossKind.Guardian) shape|=ax>7 && ax<31 && dy>15 && dy<32 && Mathf.Sin(ax*.6f+dy*.3f)>.1f;
                    baseColor=Color.Lerp(baseColor,accent,.4f); detail|=shape && ax<4 && dy>12 && dy<28;
                }
                Color c=Color.clear;
                if(shape) { c=Color.Lerp(baseColor,dark,dx>0?.24f:.04f);
                    if(kind==BossKind.Golem && (Mathf.Abs(dy-13)<1 || Mathf.Abs(dy+11)<1 || ax>15 && Mathf.Abs(dy+1)<1)) c=dark;
                    if(kind==BossKind.Dragon && ax<5 && dy<14 && dy>-16 && y%6<2) c=Color.Lerp(baseColor,Color.white,.25f);
                    if(kind==BossKind.Guardian && dy<12 && (x+(y/7))%7<2) c=Color.Lerp(baseColor,dark,.4f); if(detail) c=phaseTwo?accent:Color.Lerp(baseColor,accent,.45f); if(eye) c=new Color(1,.92f,.55f); }
                texture.SetPixel(x,y,c);
            }
            // 外周を暗く縁取り、左上の面を明るくして小さい表示でも形を読めるようにする。
            var pixels=texture.GetPixels(); var finished=(Color[])pixels.Clone();
            for(int y=1;y<size-1;y++) for(int x=1;x<size-1;x++) {
                int i=y*size+x; if(pixels[i].a==0) continue;
                bool edge=pixels[i-1].a==0||pixels[i+1].a==0||pixels[i-size].a==0||pixels[i+size].a==0;
                if(edge) finished[i]=dark;
                else if(pixels[i+size+1].a==0 || pixels[i+size-1].a==0 || (y<size-2 && pixels[i+size*2].a==0)) finished[i]=Color.Lerp(pixels[i],Color.white,.3f);
            }
            texture.SetPixels(finished);
            texture.Apply(); sprites[k,p]=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),32); return sprites[k,p];
        }
    }
}
