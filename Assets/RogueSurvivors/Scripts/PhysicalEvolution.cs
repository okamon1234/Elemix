using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // 素材のレベルを保存用に保持し、成立した組み合わせだけを一つの装備として扱う。
    public static class PhysicalEvolution
    {
        public sealed class Recipe
        {
            public readonly string Id, Weapon, Partner, Name, Description;
            public readonly int Style;
            public Recipe(string id,string weapon,string partner,string name,int style,string description)
            {Id=id;Weapon=weapon;Partner=partner;Name=name;Style=style;Description=description;}
        }
        public static readonly Recipe[] Recipes={
            new Recipe("greatsword","sword","shield","大剣",0,"盾の重さを刃に合体。巨大な二連なぎ払い"),
            new Recipe("cycloneaxe","orbit","axe","回転大斧",1,"回転機構と斧が合体。複数の大斧が外へ広がって戻る"),
            new Recipe("knifegloves","dagger","gauntlet","投刃グローブ",2,"短剣を射出する籠手。貫通ナイフを高速連射"),
            new Recipe("halberd","spear","sword","ハルバード",3,"長い柄と大きな刃が合体。なぎ払いから貫通突き"),
            new Recipe("battleaxe","axe","hammer","投擲戦斧",4,"重い頭と両刃が合体。巨大な斧を二方向へ投げる"),
            new Recipe("flail","hammer","spear","鎖鉄球",5,"長い柄と鉄の頭が合体。鎖付き鉄球を振り回す"),
            new Recipe("spikeshield","shield","hammer","スパイクシールド",6,"盾と打撃用の突起が合体。敵を次々に跳ね返る盾"),
            new Recipe("sawdisc","scythe","orbit","円盤鎌",7,"回転機構に鎌を合体。複数の刃の円盤が飛んで戻る"),
            new Recipe("chainscythe","whip","scythe","鎖鎌",8,"鞭の先に鎌を合体。長い鎖でなぎ払い、鎌が往復する"),
            new Recipe("repeater","crossbow","boomerang","連射ボウガン",9,"往復する刃と弩が合体。大型の金属矢を扇状に連射")
        };
        public static Recipe ById(string id) {foreach(var r in Recipes) if(r.Id==id) return r;return null;}
        public static WeaponBase Weapon(PlayerStats stats,string id) {foreach(var w in stats.Weapons) if(w.Id==id)return w;return null;}
        public static void Refresh(PlayerStats stats)
        {
            if(!stats || stats.RestoringWeapons) return;
            var used=new HashSet<string>();
            for(int i=0;i<stats.FusionIds.Count;) {
                var r=ById(stats.FusionIds[i]);
                if(r==null || Weapon(stats,r.Weapon).Level<4 || Weapon(stats,r.Partner).Level<4 || used.Contains(r.Weapon) || used.Contains(r.Partner)) {stats.FusionIds.RemoveAt(i);continue;}
                used.Add(r.Weapon);used.Add(r.Partner);i++;
            }
            foreach(var r in Recipes) {
                if(stats.FusionIds.Count>=5)break;
                if(used.Contains(r.Weapon)||used.Contains(r.Partner)) continue;
                var a=Weapon(stats,r.Weapon);var b=Weapon(stats,r.Partner);
                if(a && b && a.Level>=4 && b.Level>=4) {stats.FusionIds.Add(r.Id);used.Add(r.Weapon);used.Add(r.Partner);}
            }
        }
        public static Recipe Find(WeaponBase weapon)
        {
            if(!weapon || weapon.Level<4 || !WeaponCatalog.IsPhysical(weapon.Id)) return null;
            var stats=weapon.GetComponent<PlayerStats>();Refresh(stats);
            foreach(var id in stats.FusionIds) {var r=ById(id);if(r.Weapon==weapon.Id||r.Partner==weapon.Id)return r;}
            return null;
        }
        public static bool Active(WeaponBase weapon) => Find(weapon)!=null;
        public static bool IsPartner(WeaponBase weapon) {var r=Find(weapon);return r!=null && r.Partner==weapon.Id;}
        public static int DisplayLevel(WeaponBase weapon) {var r=Find(weapon);return r==null?weapon.Level:Level(weapon.GetComponent<PlayerStats>(),r);}
        public static int Level(PlayerStats stats,Recipe recipe) => Mathf.Clamp(Weapon(stats,recipe.Weapon).Level+Weapon(stats,recipe.Partner).Level-7,1,8);
        public static float Power(WeaponBase weapon) => Active(weapon)?1.5f:1;
        public static bool UpgradeFusion(WeaponBase weapon)
        {
            var r=Find(weapon);if(r==null)return false;
            var stats=weapon.GetComponent<PlayerStats>();if(Level(stats,r)>=8)return true;
            var a=Weapon(stats,r.Weapon);var b=Weapon(stats,r.Partner);
            var target=a.Level<=b.Level?a:b;target.SetLevel(target.Level+1);return true;
        }
        public static float PartnerWeight(PlayerStats stats,string candidate)
        {
            if(!WeaponCatalog.IsPhysical(candidate))return 1;float weight=1;
            foreach(var r in Recipes) {
                string partner=r.Weapon==candidate?r.Partner:r.Partner==candidate?r.Weapon:null;if(partner==null)continue;
                var w=Weapon(stats,partner);if(w && w.Level>0 && !Active(w))weight=Mathf.Max(weight,w.Level>=4?4:3);
            }
            return weight;
        }
        public static string Name(WeaponBase weapon) => Find(weapon)?.Name ?? (ElementEvolution.Active(weapon)?ElementEvolution.Name(weapon):WeaponCatalog.Find(weapon.Id)?.Name ?? weapon.Id);
        public static string Hint(string id)
        {
            string text="";
            foreach(var r in Recipes) {string partner=r.Weapon==id?r.Partner:r.Partner==id?r.Weapon:null;if(partner!=null)text+="\n"+WeaponCatalog.Find(partner).Name+"と両方Lv4 → "+r.Name;}
            return text;
        }
        public static float EstimateDps(PlayerStats stats,Recipe r)
        {
            int level=Level(stats,r);float power=PhysicalFusionCombat.Damage(r.Style,level),interval=PhysicalFusionCombat.Interval(r.Style,level);
            float hits=r.Style==0?2:r.Style==1?3:r.Style==2?6+level/2:r.Style==3?2:r.Style==4?2:r.Style==5?3:r.Style==6?1.5f:r.Style==8?2.4f:r.Style==9?(3+level/3)*2:2.5f;
            return power*hits/interval*.75f;
        }
    }
}
