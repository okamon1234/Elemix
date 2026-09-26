using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public static class MapArt
    {
        static readonly Dictionary<int,Sprite> props=new Dictionary<int,Sprite>();
        public static Texture2D Ground(SoloMapKind kind)=>Resources.Load<Texture2D>("RogueSurvivors/Art/Maps/"+(kind==SoloMapKind.Meadow?"meadow":"ruins")+"_v11");
        public static Sprite Prop(int index)
        {
            index=Mathf.Clamp(index,0,7);if(props.TryGetValue(index,out var sprite) && sprite)return sprite;
            var texture=Resources.Load<Texture2D>("RogueSurvivors/Art/Maps/props_v11");if(!texture)return null;
            float w=texture.width/4f,h=texture.height/2f;
            sprite=Sprite.Create(texture,new Rect(index%4*w,(1-index/4)*h,w,h),new Vector2(.5f,.5f),h,0,SpriteMeshType.FullRect);props[index]=sprite;return sprite;
        }
    }
}
