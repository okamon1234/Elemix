using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class ArsenalGalleryUI : MonoBehaviour
    {
        GameObject panel;
        public void Close() {if(panel)Destroy(panel);panel=null;}
        public void Open(int category=0)
        {
            Close();
            var root=UIFactory.Panel("キャラクターと武器図鑑",transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1190,650),new Color(.035f,.05f,.10f,1));
            panel=root.gameObject;
            UIFactory.Label("題名",root.transform,new Vector2(.5f,1),new Vector2(0,-16),new Vector2(1000,38),"キャラクター・武器図鑑",26,UIFactory.Cyan,TextAnchor.MiddleCenter);
            for(int i=0;i<CharacterCatalog.All.Length;i++) {
                var entry=CharacterCatalog.All[i];float x=(i-2)*205;
                var portrait=UIFactory.Panel(entry.Id,root.transform,new Vector2(.5f,1),new Vector2(x,-65),new Vector2(100,104),Color.white);
                portrait.sprite=ArsenalArt.Hero(entry.Id);portrait.preserveAspect=true;portrait.raycastTarget=false;
                UIFactory.Label("名前",root.transform,new Vector2(.5f,1),new Vector2(x,-173),new Vector2(190,30),entry.Name,20,null,TextAnchor.MiddleCenter);
            }
            string[] tabs={"物理武器 12種","属性武器 9種","合体武器 10種"};
            for(int i=0;i<3;i++) {
                int index=i;var tab=UIFactory.Button(tabs[i],root.transform,new Vector2(.5f,1),new Vector2((i-1)*330,-216),new Vector2(310,36),tabs[i],()=>Open(index));
                if(i==category)tab.GetComponent<Image>().color=new Color(.16f,.37f,.45f);
            }
            int count=0;
            if(category==2)foreach(var recipe in PhysicalEvolution.Recipes) Add(root.transform,count++,recipe.Name,ArsenalArt.Fusion(recipe.Style));
            else foreach(var entry in WeaponCatalog.All)if(WeaponCatalog.IsPhysical(entry.Id)==(category==0)) Add(root.transform,count++,entry.Name,ArsenalArt.Weapon(entry.Id));
            UIFactory.Button("閉じる",root.transform,new Vector2(.5f,0),new Vector2(0,16),new Vector2(220,38),"閉じる",Close);
        }
        static void Add(Transform root,int index,string name,Sprite sprite)
        {
            var tile=UIFactory.Panel(name,root,new Vector2(.5f,1),new Vector2((index%6-2.5f)*190,-270-index/6*140),new Vector2(178,130),new Color(.09f,.14f,.23f));
            var icon=UIFactory.Panel("絵",tile.transform,new Vector2(.5f,1),new Vector2(0,-4),new Vector2(102,90),Color.white);
            icon.sprite=sprite;icon.preserveAspect=true;icon.raycastTarget=false;
            UIFactory.Label("名前",tile.transform,new Vector2(.5f,0),new Vector2(0,5),new Vector2(172,32),name,18,null,TextAnchor.MiddleCenter);
        }
    }
}
