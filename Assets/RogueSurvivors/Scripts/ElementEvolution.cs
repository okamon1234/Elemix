namespace RogueSurvivors
{
    public static class ElementEvolution
    {
        public static bool Active(WeaponBase weapon)
        {
            if(!weapon || weapon.Level<4 || (weapon.Id!="light" && weapon.Id!="dark")) return false;
            foreach(var other in weapon.GetComponents<WeaponBase>()) if(other.Id==(weapon.Id=="light"?"dark":"light") && other.Level>=4) return true;
            return false;
        }
        public static string Name(WeaponBase weapon) => Active(weapon) ? weapon.Id=="light"?"光の大槍":"重力の杖" : WeaponCatalog.Find(weapon.Id)?.Name ?? weapon.Id;
    }
}
