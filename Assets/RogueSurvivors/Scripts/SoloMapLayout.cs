using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // 地形を座標だけから決める。再訪・負座標でも同じ配置を復元する。
    public static class SoloMapLayout
    {
        public const float ChunkSize=24;
        public readonly struct Placement
        {
            public readonly int Art;public readonly Vector2 Position;public readonly float Size;public readonly bool Solid;
            public Placement(int art,Vector2 position,float size,bool solid){Art=art;Position=position;Size=size;Solid=solid;}
        }
        public static Vector2Int Key(Vector2 position)=>new Vector2Int(Mathf.FloorToInt((position.x+12)/24),Mathf.FloorToInt((position.y+12)/24));
        public static int District(SoloMapKind kind,Vector2Int key)=>key==Vector2Int.zero?0:(int)((uint)Seed(kind,key)%3);
        static int Seed(SoloMapKind kind,Vector2Int key)=>unchecked(key.x*73856093^key.y*19349663^((int)kind+1)*83492791);
        public static List<Placement> Generate(SoloMapKind kind,Vector2Int key)
        {
            var list=new List<Placement>();var random=new System.Random(Seed(kind,key));int district=District(kind,key);
            Vector2 center=(Vector2)key*ChunkSize;
            for(int corner=0;corner<4;corner++) {
                Vector2 sign=new Vector2(corner%2==0?-1:1,corner<2?-1:1);
                Vector2 spot=center+new Vector2(sign.x*(6.5f+(float)random.NextDouble()),sign.y*(6.5f+(float)random.NextDouble()));
                if(kind==SoloMapKind.Meadow) {
                    int art=district==1?0:district==2?1:corner%2==0?1:2;
                    list.Add(new Placement(art,spot,art==0?5.8f:art==1?3.8f:4.1f,true));
                    if(district==1)list.Add(new Placement(0,center+new Vector2(sign.x*10,sign.y*5.5f),4.5f,true));
                } else {
                    int art=district==0?4:district==1?4:5;
                    list.Add(new Placement(art,spot,art==5?5.2f:3.8f,true));
                    if(district!=0)list.Add(new Placement(district==1?4:7,center+new Vector2(sign.x*9.5f,sign.y*4.6f),3.1f,true));
                }
            }
            // 広場の北側に通り抜けられる目印。道路の中心には支柱を置かない。
            if(kind==SoloMapKind.Ruins && district==0)list.Add(new Placement(6,center+Vector2.up*8.8f,7.6f,true));
            for(int i=0;i<8;i++) {
                Vector2 spot=center+new Vector2((float)random.NextDouble()*22-11,(float)random.NextDouble()*22-11);
                if(Mathf.Abs(spot.x-center.x)<3 || Mathf.Abs(spot.y-center.y)<3)continue;
                list.Add(new Placement(kind==SoloMapKind.Meadow?3:7,spot,1.2f+(float)random.NextDouble()*.6f,false));
            }
            return list;
        }
        public static Rect[] Bounds(Placement prop)
        {
            if(!prop.Solid)return new Rect[0];
            Vector2 p=prop.Position;float s=prop.Size;
            if(prop.Art==6)return new[]{Box(p+new Vector2(-s*.22f,-s*.18f),new Vector2(s*.12f,s*.16f)),Box(p+new Vector2(s*.22f,-s*.18f),new Vector2(s*.12f,s*.16f))};
            Vector2 size=prop.Art==0?new Vector2(s*.13f,s*.13f):prop.Art==5?new Vector2(s*.65f,s*.16f):prop.Art==2?new Vector2(s*.54f,s*.2f):prop.Art==4?new Vector2(s*.27f,s*.2f):new Vector2(s*.5f,s*.27f);
            return new[]{Box(p+Vector2.down*s*(prop.Art==0?.23f:prop.Art==4?.2f:.08f),size)};
        }
        static Rect Box(Vector2 center,Vector2 size)=>new Rect(center-size*.5f,size);
    }
}
