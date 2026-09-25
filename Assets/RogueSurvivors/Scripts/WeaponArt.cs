using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public static class WeaponArt
    {
        static readonly Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
        public static Sprite Get(int kind)
        {
            if (sprites.TryGetValue(kind, out var result) && result) return result;
            var painted=ArsenalArt.Physical(kind); if(painted) {sprites[kind]=painted;return painted;}
            var texture = new Texture2D(32,32,TextureFormat.RGBA32,false); texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) {
                bool handle = y >= 14 && y <= 17 && x >= 3 && x <= 19;
                bool metal = kind == 0 ? x >= 12 && x <= 28 && Mathf.Abs(y-16) <= (28-x)/4 :
                    kind == 1 ? x > 15 && x < 27 && y > 5 && y < 27 && Mathf.Abs(y-16) < (x-12) :
                    kind == 2 ? x > 19 && x < 28 && y > 6 && y < 26 :
                    kind == 3 ? x > 10 && x < 29 && Mathf.Abs(y-16) <= 3 && Mathf.Abs(y-16) < 29-x :
                    kind == 4 ? x > 8 && x < 25 && Mathf.Abs(y-16) < 12-Mathf.Abs(x-16)/2 :
                    kind == 5 ? x > 14 && x < 27 && y > 9 && y < 23 && (x > 19 || y % 4 != 0) :
                    x > 15 && x < 29 && y > 18 && y < 29 && Mathf.Abs(x-21) + Mathf.Abs(y-23) < 11;
                texture.SetPixel(x,y, metal ? (y%4 == 0 ? new Color(.45f,.55f,.65f) : new Color(.8f,.88f,.95f)) : handle ? new Color(.55f,.32f,.15f) : Color.clear);
            }
            texture.Apply(); result = Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),24); sprites[kind] = result; return result;
        }
    }
}
