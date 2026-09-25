using UnityEngine;
namespace RogueSurvivors
{
    public static class MageLoadout
    {
        public static readonly string[] Weapons = { "fireball", "water", "ice", "lightning", "bolt", "wood", "earth", "light", "dark" };
        public static string ValidWeapon(string id) { foreach(var weapon in Weapons) if(weapon==id) return id; return "fireball"; }
        public static string SelectedWeapon {
            get => ValidWeapon(PlayerPrefs.GetString("Rogue.MageWeapon","fireball"));
            set { PlayerPrefs.SetString("Rogue.MageWeapon",ValidWeapon(value)); PlayerPrefs.Save(); }
        }
    }
}
