using UnityEngine;
namespace RogueSurvivors
{
    // 属性の発光色を使わず、金属・木・革で合体後の構造を描く。
    public static class PhysicalFusionArt
    {
        static readonly Sprite[] sprites=new Sprite[10];
        public static Sprite Get(int style)
        {
            style=Mathf.Clamp(style,0,9);if(sprites[style])return sprites[style];
            var painted=ArsenalArt.Fusion(style);if(painted){sprites[style]=painted;return painted;}
            const int size=64;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,name="合体武器"+style};
            var pixels=new Color[size*size];Color steel=new Color(.73f,.78f,.82f),edge=new Color(.12f,.15f,.19f),bronze=new Color(.64f,.43f,.23f),leather=new Color(.29f,.2f,.15f);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float dx=x-31.5f,dy=y-31.5f,ax=Mathf.Abs(dx),ay=Mathf.Abs(dy),r=Mathf.Sqrt(dx*dx+dy*dy),angle=Mathf.Atan2(dy,dx);
                bool metal=false,grip=false,gold=false;
                switch(style) {
                    case 0: metal=x>20&&x<60&&ay<Mathf.Min(8,(61-x)*.8f);grip=x>3&&x<23&&ay<3;gold=x>=17&&x<=22&&ay<15;break;
                    case 1: metal=(ax<20&&ay<26&&ay>9&&ax>ay*.35f)||(r<7);grip=ax<3&&ay<29;gold=ax<5&&ay<13;break;
                    case 2: metal=x>19&&x<53&&ay<12 || x>43&&x<62&&ay<4;grip=x>6&&x<21&&ay<10;gold=x>21&&x<27&&ay<14;break;
                    case 3: grip=x>1&&x<48&&ay<2;metal=x>41&&x<62&&ay<Mathf.Min(4,(63-x)*.5f)||x>34&&x<48&&ay<20&&ay>5&&x>32+ay*.25f;gold=x>32&&x<38&&ay<6;break;
                    case 4: grip=x>4&&x<51&&ay<3;metal=x>31&&x<56&&ay<24&&ay>6&&x>28+ay*.3f;gold=x>30&&x<36&&ay<11;break;
                    case 5: metal=r<20||r<29&&Mathf.Abs(Mathf.Sin(angle*6))<.22f;gold=r<9;break;
                    case 6: metal=r<22||r<29&&Mathf.Abs(Mathf.Sin(angle*8))<.15f;gold=r>16&&r<20||r<6;break;
                    case 8: grip=x>4&&x<47&&ay<2;metal=x>39&&x<58&&dy>0&&dy<25&&Mathf.Abs(x-46)+Mathf.Abs(y-47)<19;gold=x>34&&x<40&&ay<5;break;
                    case 9: grip=x>5&&x<49&&ay<3;metal=x>31&&x<49&&Mathf.Abs(ax-8-ay*.3f)<4&&ay<26||x>47&&x<62&&ay<Mathf.Min(4,(63-x)*.6f);gold=x>25&&x<33&&ay<7;break;
                    default: metal=r<20&&r>9 || r>17&&r<30&&Mathf.Sin(angle*6+r*.12f)>.1f;gold=r<9;break;
                }
                Color c=Color.clear;
                if(metal)c=Color.Lerp(steel,edge,(dy<0?.28f:0)+(dx>0?.12f:0));
                if(grip)c=(x+y)%7<2?bronze:leather;
                if(gold)c=bronze;
                if(metal&&!gold&&!grip&&Mathf.Abs(dy)<1)c=Color.white;
                pixels[y*size+x]=c;
            }
            var outlined=(Color[])pixels.Clone();
            for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++) {int i=y*size+x;if(pixels[i].a==0)continue;if(pixels[i-1].a==0||pixels[i+1].a==0||pixels[i-size].a==0||pixels[i+size].a==0)outlined[i]=edge;else if(y<size-2&&pixels[i+size*2].a==0)outlined[i]=Color.Lerp(pixels[i],Color.white,.6f);}
            texture.SetPixels(outlined);texture.Apply();sprites[style]=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),32);return sprites[style];
        }
    }
}
