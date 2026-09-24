using UnityEngine;
namespace RogueSurvivors
{
    // Eight original pixel silhouettes, generated once and shared by all instances.
    public sealed class BossAppearance : MonoBehaviour
    {
        static readonly Sprite[,] sprites=new Sprite[4,2];
        BossAI boss; SpriteRenderer body; Vector3 originalScale; int last=-1;
        void Start()
        {
            boss=GetComponent<BossAI>(); body=GetComponentInChildren<SpriteRenderer>();
            if(body) { originalScale=body.transform.localScale; var animator=body.GetComponent<Animator>(); if(animator) animator.enabled=false; }
        }
        void LateUpdate()
        {
            if(!body || !boss) return;
            int phase=boss.PhaseTwo?1:0, key=(int)boss.Kind*2+phase;
            if(key!=last) {
                last=key; body.sprite=Resources.Load<Sprite>("RogueSurvivors/Art/Bosses/"+boss.Kind+"_"+phase) ?? GetSprite(boss.Kind,boss.PhaseTwo); body.color=Color.white; GetComponent<HitFeedback>()?.RefreshColor();
                if(phase==1) { EffectsService.Instance?.Burst(transform.position,BossCatalog.Colors[(int)boss.Kind]); EffectsService.Instance?.Play("Hurt",.5f); CameraFollow.Instance?.Shake(.25f); HUDController.Instance?.Toast(boss.DisplayName+"："+BossCatalog.PhaseNames[(int)boss.Kind]+"！"); }
            }
            float pulse=boss.IsTransforming?1+.09f*Mathf.Sin(Time.time*14):1+.012f*Mathf.Sin(Time.time*3);
            body.transform.localScale=originalScale*pulse;
        }
        public static Sprite GetSprite(BossKind kind,bool phaseTwo)
        {
            int k=(int)kind,p=phaseTwo?1:0; if(sprites[k,p]) return sprites[k,p];
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
                        detail=(ax<7 && dy>-2 && dy<9) || (phaseTwo && (x+y)%11<2);
                        eye=dy>19 && dy<22 && ax>2 && ax<7; break;
                    case BossKind.Spider:
                        shape=dx*dx/225+dy*dy/324<1 || (ax<10 && dy<-11 && dy>-23);
                        for(int leg=0;leg<4;leg++) { float line=(leg-1.5f)*9+(ax-12)*(leg<2?-.65f:.65f); shape|=ax>9 && ax<30 && Mathf.Abs(dy-line)<(phaseTwo?2.5f:1.7f); }
                        baseColor=phaseTwo?new Color(.5f,.17f,.3f):new Color(.27f,.18f,.38f);
                        detail=ax<9 && Mathf.Abs(dy-5)<9 && ((x+y)%7<2);
                        eye=dy>-20 && dy<-15 && (ax<2 || (ax>4 && ax<7));
                        if(phaseTwo) shape|=ax>3 && ax<8 && dy<-22 && dy>-30; break;
                    case BossKind.Dragon:
                        shape=(ax<7 && dy>-23 && dy<20) || (ax<11 && dy>12 && dy<25) || (dy<-17 && ax<4+(dy+28)*.3f && dy>-30);
                        shape|=ax>5 && ax<(phaseTwo?31:26) && dy<19-ax*.2f && dy>(phaseTwo?-17:-5)+ax*.4f && (y+x/4)%9!=0;
                        shape|=ax>7 && ax<12 && dy>22 && dy<30;
                        baseColor=phaseTwo?new Color(.18f,.35f,.63f):new Color(.24f,.52f,.57f);
                        detail=ax<4 && dy>-15 && dy<16 || (phaseTwo && ax>12 && (x-y)%8<2);
                        eye=dy>18 && dy<21 && ax>4 && ax<8; break;
                    case BossKind.Guardian:
                        shape=ax<10 && dy>-20 && dy<12;
                        shape|=dx*dx/625+(dy-14)*(dy-14)/196<1;
                        shape|=dy<-14 && dy>-28 && ax<22 && Mathf.Abs(ax-(-dy-12))<5;
                        shape|=ax>8 && ax<27 && Mathf.Abs(dy-(ax-15)*.7f)<4;
                        if(phaseTwo) shape|=ax>12 && ax<30 && dy>12 && dy<30 && (x+y)%8<4;
                        baseColor=dy>10?(phaseTwo?new Color(.75f,.32f,.13f):new Color(.22f,.46f,.21f)):new Color(.38f,.24f,.13f);
                        detail=ax<7 && dy>-5 && dy<8 && x%5<2;
                        eye=dy>4 && dy<7 && ax>2 && ax<7; break;
                }
                Color c=Color.clear;
                if(shape) { c=Color.Lerp(baseColor,dark,((x*7+y*3)%13==0)?.4f:dx>0?.17f:0); if(detail) c=phaseTwo?accent:Color.Lerp(baseColor,accent,.45f); if(eye) c=new Color(1,.92f,.55f); }
                texture.SetPixel(x,y,c);
            }
            texture.Apply(); sprites[k,p]=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),32); return sprites[k,p];
        }
    }
}
