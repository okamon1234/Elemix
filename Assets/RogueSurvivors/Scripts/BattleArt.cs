using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public static class BattleArt
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        static readonly string[] enemies={"skeleton","rat","armored","archer","charger"};
        static readonly float[] heights={1.65f,1.15f,1.95f,1.75f,1.7f};
        static Sprite Cell(string atlas,int columns,int rows,int index,float height)
        {
            string key=atlas+":"+index;
            if(cache.TryGetValue(key,out var sprite) && sprite)return sprite;
            var texture=Resources.Load<Texture2D>("RogueSurvivors/Art/Battle/"+atlas);
            if(!texture)return null;
            float w=texture.width/(float)columns,h=texture.height/(float)rows;
            sprite=Sprite.Create(texture,new Rect(index%columns*w,(rows-1-index/columns)*h,w,h),new Vector2(.5f,.5f),h/height,0,SpriteMeshType.FullRect);
            sprite.name=key;cache[key]=sprite;return sprite;
        }
        public static Sprite Enemy(EnemyRole role,int frame=0)
        {int i=Mathf.Clamp((int)role,0,4);return Cell(enemies[i]+"_v10",3,1,Mathf.Clamp(frame,0,2),heights[i]);}
        public static Sprite Effect(int index)=>Cell("CombatEffects_v10",4,3,Mathf.Clamp(index,0,11),1);
        public static int ElementIndex(CombatElement element)
        {
            switch(element) {
                case CombatElement.Fire:return 0;case CombatElement.Water:return 1;case CombatElement.Ice:return 2;
                case CombatElement.Lightning:return 3;case CombatElement.Wind:return 4;case CombatElement.Wood:return 5;
                case CombatElement.Earth:return 6;case CombatElement.Light:return 7;case CombatElement.Dark:return 8;default:return 9;
            }
        }
    }
}
