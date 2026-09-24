using UnityEngine;
namespace RogueSurvivors
{
    public enum UpgradeKind { Bolt, Orbit, Power, Speed, Health, Pickup, Fireball, Lightning, Spear, Wind, Ice, Water, Light, Dark, Dagger, Axe, Hammer, Wood, Earth, Sword, Shield, Gauntlet, Scythe, Regeneration }
    [System.Serializable]
    public sealed class UpgradeOption
    {
        [SerializeField] UpgradeKind kind;
        [SerializeField] string title;
        [SerializeField] string description;
        public UpgradeKind Kind => kind;
        public string Title => title;
        public string Description => description;
        public UpgradeOption(UpgradeKind kind, string title, string description) { this.kind = kind; this.title = title; this.description = description; }
        public void Apply(PlayerStats stats)
        {
            var weapon = WeaponCatalog.Get(stats, Kind); if (weapon) { if (stats.CanAcquire(weapon)) weapon.Upgrade(); return; }
            switch (Kind)
            {
                case UpgradeKind.Bolt: stats.GetComponent<ProjectileWeapon>().Upgrade(); break;
                case UpgradeKind.Orbit: stats.GetComponent<OrbitWeapon>().Upgrade(); break;
                case UpgradeKind.Power: stats.UpgradeDamage(); break;
                case UpgradeKind.Speed: stats.UpgradeSpeed(); break;
                case UpgradeKind.Health: stats.UpgradeHealth(); break;
                case UpgradeKind.Regeneration: stats.UpgradeRegeneration(); break;
                case UpgradeKind.Pickup: stats.UpgradePickup(); break;
            }
        }
    }
}
