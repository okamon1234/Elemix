using System;
using System.Collections.Generic;
namespace RogueSurvivors
{
    [Serializable] public sealed class WeaponSaveData
    {
        public string id;
        public int level;
        public WeaponSaveData(string id, int level) { this.id = id; this.level = level; }
    }
    [Serializable] public sealed class PlayerDataData
    {
        public int version = 1;
        public int level = 1;
        public int experience;
        public string characterId = "ranger";
        public string startingMageWeapon = "fireball";
        public float regeneration;
        public float damageReduction;
        public float moveSpeed = 5;
        public float damageMultiplier = 1;
        public float maxHealth = 100;
        public float pickupRadius = 2.4f;
        public int kills;
        public List<WeaponSaveData> weapons = new List<WeaponSaveData>();
        public List<string> physicalFusions = new List<string>();
        public List<UpgradeRecord> upgradeHistory = new List<UpgradeRecord>();
        public PlayerDataData NetworkCopy()
        {
            return new PlayerDataData { version = version, level = level, experience = experience, characterId = characterId, startingMageWeapon = MageLoadout.ValidWeapon(startingMageWeapon),
                regeneration = regeneration, damageReduction = damageReduction, moveSpeed = moveSpeed, damageMultiplier = damageMultiplier,
                maxHealth = maxHealth, pickupRadius = pickupRadius, kills = kills, weapons = new List<WeaponSaveData>(weapons), physicalFusions=physicalFusions==null?new List<string>():new List<string>(physicalFusions) };
        }
        public bool IsValid()
        {
            if (weapons == null || weapons.Count > WeaponCatalog.All.Length) return false;
            if (upgradeHistory != null && (upgradeHistory.Count > 1000 || upgradeHistory.Exists(r => r == null || !Enum.IsDefined(typeof(UpgradeKind), r.kind) || float.IsNaN(r.previousValue) || float.IsInfinity(r.previousValue)))) return false;
            if(physicalFusions!=null && (physicalFusions.Count>5 || physicalFusions.Exists(id=>PhysicalEvolution.ById(id)==null)))return false;
            if(upgradeHistory!=null) foreach(var record in upgradeHistory) {
                if(record.hasPhysicalSnapshot) {
                    if(record.physicalWeapons==null)return false;
                    if(record.physicalWeapons.Count>12)return false;
                    var snapshotIds=new HashSet<string>();
                    foreach(var w in record.physicalWeapons) if(w==null||!WeaponCatalog.IsPhysical(w.id)||w.level<1||w.level>8||!snapshotIds.Add(w.id))return false;
                }
                if(record.physicalFusions!=null && (record.physicalFusions.Count>5 || record.physicalFusions.Exists(id=>PhysicalEvolution.ById(id)==null)))return false;
            }
            var ids = new HashSet<string>();
            foreach (var weapon in weapons)
                if (weapon == null || WeaponCatalog.Find(weapon.id) == null || weapon.level < 1 || weapon.level > 8 || !ids.Add(weapon.id)) return false;
            return version == 1 && level >= 1 && level <= 1000 && experience >= 0 && regeneration >= 0 && regeneration <= 20 && damageReduction >= 0 && damageReduction <= .8f &&
                moveSpeed >= 1 && moveSpeed <= 20 && damageMultiplier >= 0.1f && damageMultiplier <= 100 &&
                maxHealth >= 1 && maxHealth <= 10000 && pickupRadius >= 0.5f && pickupRadius <= 20 && weapons != null;
        }
    }
}
