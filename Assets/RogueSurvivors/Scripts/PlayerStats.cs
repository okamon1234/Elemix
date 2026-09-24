using System;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class PlayerStats : MonoBehaviour
    {
        public int ElementWeaponCount { get { int count = 0; foreach (var weapon in GetComponents<WeaponBase>()) if (weapon.Level > 0 && !WeaponCatalog.IsPhysical(weapon.Id)) count++; return count; } }
        public bool CanAcquire(WeaponBase weapon) => weapon && (weapon.Level > 0 || (WeaponCount < 6 && (WeaponCatalog.IsPhysical(weapon.Id) || ElementWeaponCount < 4)));
        public int WeaponCount { get { int count = 0; foreach (var weapon in GetComponents<WeaponBase>()) if (weapon.Level > 0) count++; return count; } }
        void Awake()
        {
            if (!GetComponent<BossRecovery>()) gameObject.AddComponent<BossRecovery>();
            if (!GetComponent<DaggerWeapon>()) gameObject.AddComponent<DaggerWeapon>();
            if (!GetComponent<AxeWeapon>()) gameObject.AddComponent<AxeWeapon>();
            if (!GetComponent<HammerWeapon>()) gameObject.AddComponent<HammerWeapon>();
            if (!GetComponent<WoodWeapon>()) gameObject.AddComponent<WoodWeapon>();
            if (!GetComponent<EarthWeapon>()) gameObject.AddComponent<EarthWeapon>();
            if (!GetComponent<SwordWeapon>()) gameObject.AddComponent<SwordWeapon>();
            if (!GetComponent<ShieldWeapon>()) gameObject.AddComponent<ShieldWeapon>();
            if (!GetComponent<GauntletWeapon>()) gameObject.AddComponent<GauntletWeapon>();
            if (!GetComponent<ScytheWeapon>()) gameObject.AddComponent<ScytheWeapon>();
            if (!GetComponent<IceWeapon>()) gameObject.AddComponent<IceWeapon>();
            if (!GetComponent<WaterWeapon>()) gameObject.AddComponent<WaterWeapon>();
            if (!GetComponent<LightWeapon>()) gameObject.AddComponent<LightWeapon>();
            if (!GetComponent<DarkWeapon>()) gameObject.AddComponent<DarkWeapon>();
        }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public string CharacterId { get; private set; } = "ranger";
        public float Regeneration { get; private set; }
        public float DamageReduction { get; private set; }
        public int RequiredExperience => 8 + (Level - 1) * 5;
        public float MoveSpeed { get; private set; } = 5;
        public float DamageMultiplier { get; private set; } = 1;
        public float MaxHealth { get; private set; } = 100;
        public float PickupRadius { get; private set; } = 2.4f;
        public event Action LeveledUp;
        [SerializeField] System.Collections.Generic.List<UpgradeRecord> upgradeHistory = new System.Collections.Generic.List<UpgradeRecord>();
        public void RecordUpgrade(UpgradeKind kind)
        {
            var weapon = WeaponCatalog.Get(this, kind);
            float value = weapon ? weapon.Level :
                kind == UpgradeKind.Power ? DamageMultiplier : kind == UpgradeKind.Speed ? MoveSpeed : kind == UpgradeKind.Health ? MaxHealth : kind == UpgradeKind.Regeneration ? Regeneration : PickupRadius;
            upgradeHistory.Add(new UpgradeRecord(kind, value));
        }
        public int LoseRecentLevels(int count)
        {
            int lost = Mathf.Min(count, Level - 1);
            for (int i = 0; i < lost; i++) {
                if (upgradeHistory.Count > 0) {
                    var record = upgradeHistory[upgradeHistory.Count - 1]; upgradeHistory.RemoveAt(upgradeHistory.Count - 1);
                    var recordedWeapon = WeaponCatalog.Get(this, record.kind);
                    if (recordedWeapon) { recordedWeapon.SetLevel((int)record.previousValue); continue; }
                    switch (record.kind) {
                        case UpgradeKind.Bolt: GetComponent<ProjectileWeapon>().SetLevel((int)record.previousValue); break;
                        case UpgradeKind.Orbit: GetComponent<OrbitWeapon>().SetLevel((int)record.previousValue); break;
                        case UpgradeKind.Power: DamageMultiplier = record.previousValue; break;
                        case UpgradeKind.Speed: MoveSpeed = record.previousValue; break;
                        case UpgradeKind.Health: MaxHealth = record.previousValue; break;
                        case UpgradeKind.Regeneration: Regeneration = record.previousValue; break;
                        case UpgradeKind.Pickup: PickupRadius = record.previousValue; break;
                    }
                } else {
                    // Legacy saves did not record choice order. Preserve the character's starting kit.
                    var initial = CharacterCatalog.CreateBuild(CharacterId); WeaponBase strongest = null;
                    foreach (var weapon in GetComponents<WeaponBase>()) {
                        int minimum = initial.weapons.Find(w => w.id == weapon.Id)?.level ?? 0;
                        if (weapon.Level > minimum && (!strongest || weapon.Level > strongest.Level)) strongest = weapon;
                    }
                    if (strongest) strongest.SetLevel(strongest.Level - 1);
                    else if (DamageMultiplier > initial.damageMultiplier) DamageMultiplier = Mathf.Max(initial.damageMultiplier, DamageMultiplier - .2f);
                    else if (MaxHealth > initial.maxHealth) MaxHealth = Mathf.Max(initial.maxHealth, MaxHealth - 20);
                    else if (MoveSpeed > initial.moveSpeed) MoveSpeed = Mathf.Max(initial.moveSpeed, MoveSpeed - .4f);
                    else PickupRadius = Mathf.Max(initial.pickupRadius, PickupRadius - .6f);
                }
            }
            Level -= lost; Experience = 0; return lost;
        }
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            Experience += amount;
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                LeveledUp?.Invoke();
            }
        }
        public void UpgradeRegeneration() => Regeneration = Mathf.Min(2, Regeneration + .35f);
        public void UpgradeDamage() => DamageMultiplier = Mathf.Min(100, DamageMultiplier + 0.2f);
        public void UpgradeSpeed() => MoveSpeed = Mathf.Min(20, MoveSpeed + 0.4f);
        public void UpgradePickup() => PickupRadius = Mathf.Min(20, PickupRadius + 0.6f);
        public void UpgradeHealth()
        {
            MaxHealth = Mathf.Min(10000, MaxHealth + 20);
            GetComponent<PlayerHealth>().Heal(20);
        }
        public PlayerDataData Capture()
        {
            var result = new PlayerDataData { level = Level, experience = Experience, moveSpeed = MoveSpeed,
                damageMultiplier = DamageMultiplier, maxHealth = MaxHealth, pickupRadius = PickupRadius,
                characterId = CharacterId, regeneration = Regeneration, damageReduction = DamageReduction,
                kills = GameManager.Instance ? GameManager.Instance.Kills : 0 };
            result.upgradeHistory = new System.Collections.Generic.List<UpgradeRecord>(upgradeHistory);
            foreach (var weapon in GetComponents<WeaponBase>())
                if (weapon.Level > 0) result.weapons.Add(new WeaponSaveData(weapon.Id, weapon.Level));
            return result;
        }
        public void Restore(PlayerDataData data)
        {
            if (data == null || !data.IsValid()) return;
            Level = data.level; Experience = data.experience; MoveSpeed = data.moveSpeed;
            DamageMultiplier = data.damageMultiplier; MaxHealth = data.maxHealth; PickupRadius = data.pickupRadius;
            CharacterId = CharacterCatalog.Find(data.characterId).Id; Regeneration = Mathf.Min(2, data.regeneration); DamageReduction = data.damageReduction;
            upgradeHistory = data.upgradeHistory == null ? new System.Collections.Generic.List<UpgradeRecord>() : new System.Collections.Generic.List<UpgradeRecord>(data.upgradeHistory);
            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite) {
                var portrait = Resources.Load<Sprite>("RogueSurvivors/Art/" + CharacterId);
                if (portrait) sprite.sprite = portrait;
                sprite.color = Color.white; GetComponent<HitFeedback>()?.RefreshColor();
            }
            foreach (var weapon in GetComponents<WeaponBase>()) weapon.SetLevel(0);
            foreach (var saved in data.weapons)
                foreach (var weapon in GetComponents<WeaponBase>())
                    if (weapon.Id == saved.id) weapon.SetLevel(saved.level);
            GetComponent<PlayerHealth>().ResetHealth();
        }
    }
}
