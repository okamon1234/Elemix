using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // A shared atlas lookup keeps gameplay IDs independent from artwork.
    public static class ArsenalArt
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        static readonly string[] physical={"dagger","axe","hammer","sword","shield","gauntlet","scythe","spear","whip","crossbow","boomerang","orbit"};
        static readonly string[] elemental={"fireball","water","ice","lightning","bolt","wood","earth","light","dark"};
        static Sprite Cell(string atlas,int columns,int rows,int index,float worldHeight)
        {
            string key=atlas+":"+index;
            if(cache.TryGetValue(key,out var found)&&found)return found;
            var texture=Resources.Load<Texture2D>("RogueSurvivors/Art/Polished/"+atlas);
            if(!texture)return null;
            float width=texture.width/(float)columns,height=texture.height/(float)rows;
            var rect=new Rect((index%columns)*width,(rows-1-index/columns)*height,width,height);
            var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),height/worldHeight,0,SpriteMeshType.FullRect);
            sprite.name=key;cache[key]=sprite;return sprite;
        }
        public static Sprite Hero(string id,int frame=0)=>Cell(id+"_v7",3,1,Mathf.Clamp(frame,0,2),1.7f);
        public static Sprite Physical(int kind)=>Cell("PhysicalWeapons_v7",4,3,Mathf.Clamp(kind,0,11),1.333333f);
        public static Sprite Fusion(int style)=>Cell("FusedWeapons_v7",5,2,Mathf.Clamp(style,0,9),2);
        public static Sprite Weapon(string id)
        {
            for(int i=0;i<physical.Length;i++)if(physical[i]==id)return Physical(i);
            for(int i=0;i<elemental.Length;i++)if(elemental[i]==id)return Cell("ElementWeapons_v7",3,3,i,1.4f);
            return null;
        }
        public static string ElementId(CombatElement element)
        {
            switch(element) {
                case CombatElement.Fire:return "fireball";case CombatElement.Water:return "water";
                case CombatElement.Ice:return "ice";case CombatElement.Lightning:return "lightning";
                case CombatElement.Wind:return "bolt";case CombatElement.Wood:return "wood";
                case CombatElement.Earth:return "earth";case CombatElement.Light:return "light";
                case CombatElement.Dark:return "dark";default:return null;
            }
        }
        public static Sprite Upgrade(UpgradeKind kind)
        {
            foreach(var entry in WeaponCatalog.All)if(entry.Kind==kind) {
                var local=PlayerHealth.Local;
                var weapon=local?WeaponCatalog.Get(local.GetComponent<PlayerStats>(),kind):null;
                if(weapon && PhysicalEvolution.Active(weapon)) {
                    for(int i=0;i<PhysicalEvolution.Recipes.Length;i++)
                        if(PhysicalEvolution.Recipes[i].Weapon==entry.Id && local.GetComponent<PlayerStats>().FusionIds.Contains(PhysicalEvolution.Recipes[i].Id))return Fusion(i);
                }
                return Weapon(entry.Id);
            }
            return null;
        }
    }
}
