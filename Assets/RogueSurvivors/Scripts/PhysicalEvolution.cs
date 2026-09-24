using UnityEngine;
namespace RogueSurvivors
{
    public static class PhysicalEvolution
    {
        public sealed class Recipe
        {
            public readonly string Weapon, Partner, Name;
            public Recipe(string weapon, string partner, string name) { Weapon = weapon; Partner = partner; Name = name; }
        }
        public static readonly Recipe[] Recipes = {
            new Recipe("sword", "shield", "大剣"), new Recipe("orbit", "axe", "回転大斧"),
            new Recipe("dagger", "gauntlet", "双剣"), new Recipe("spear", "sword", "ハルバード"),
            new Recipe("axe", "hammer", "両刃の戦斧"), new Recipe("hammer", "spear", "ウォーハンマー"),
            new Recipe("shield", "hammer", "スパイクシールド"), new Recipe("gauntlet", "dagger", "クロー"),
            new Recipe("scythe", "orbit", "大鎌")
        };
        public static Recipe Find(WeaponBase weapon)
        {
            if (weapon.Level < 4) return null;
            foreach (var recipe in Recipes) if (recipe.Weapon == weapon.Id)
                foreach (var partner in weapon.GetComponents<WeaponBase>())
                    if (partner.Id == recipe.Partner && partner.Level >= 4) return recipe;
            return null;
        }
        public static bool Active(WeaponBase weapon) => Find(weapon) != null;
        public static float Power(WeaponBase weapon) => Active(weapon) ? 1.5f : 1;
        public static float PartnerWeight(PlayerStats stats,string candidate)
        {
            if(!WeaponCatalog.IsPhysical(candidate)) return 1;
            float weight=1;
            foreach(var recipe in Recipes) {
                string partner=recipe.Weapon==candidate ? recipe.Partner : recipe.Partner==candidate ? recipe.Weapon : null;
                if(partner==null) continue;
                foreach(var owned in stats.GetComponents<WeaponBase>()) if(owned.Id==partner && owned.Level>0) weight=UnityEngine.Mathf.Max(weight,owned.Level>=4 ? 4 : 3);
            }
            return weight;
        }
        public static string Name(WeaponBase weapon) => ElementEvolution.Active(weapon) ? ElementEvolution.Name(weapon) : Find(weapon)?.Name ?? WeaponCatalog.Find(weapon.Id)?.Name ?? weapon.Id;
        public static string Hint(string id)
        {
            foreach (var recipe in Recipes) if (recipe.Weapon == id)
                return "\n両方Lv4：" + WeaponCatalog.Find(recipe.Partner).Name + " → " + recipe.Name;
            return "";
        }
    }
}
