using UnityEngine;
namespace RogueSurvivors
{
    public sealed class WeaponDefinition
    {
        public readonly string Id, Name, Description;
        public readonly UpgradeKind Kind;
        public WeaponDefinition(string id, string name, UpgradeKind kind, string description) { Id = id; Name = name; Kind = kind; Description = description; }
    }
    public static class WeaponCatalog
    {
        public static readonly WeaponDefinition[] All = {
            new WeaponDefinition("bolt", "風の弓", UpgradeKind.Bolt, "風：遠距離射撃・炎と大爆発\n水・氷・雷を拡散\nレベル4・7で矢が増加"),
            new WeaponDefinition("orbit", "回転剣", UpgradeKind.Orbit, "物理：周囲を回る剣で攻撃\n2レベルごとに剣が増加"),
            new WeaponDefinition("fireball", "火の玉", UpgradeKind.Fireball, "【炎】風・雷・氷・水と反応\n強化すると爆発範囲も拡大"),
            new WeaponDefinition("lightning", "雷", UpgradeKind.Lightning, "【雷】炎と過負荷、水と感電\n氷と超電導で物理を強化\n強化すると連鎖する数が増加"),
            new WeaponDefinition("spear", "槍", UpgradeKind.Spear, "物理：一直線に並ぶ敵を貫く\n一直線に並んだ敵を貫く"),
            new WeaponDefinition("ice", "氷の杖", UpgradeKind.Ice, "氷：敵を減速させる\n水と凍結、炎と融解、雷と超電導"),
            new WeaponDefinition("water", "水の杖", UpgradeKind.Water, "水：命中地点の周囲を攻撃\n炎と蒸発、雷と感電、氷と凍結"),
            new WeaponDefinition("light", "光の杖", UpgradeKind.Light, "光：直線を貫く光線\n闇と反応して対消滅"),
            new WeaponDefinition("dark", "闇の杖", UpgradeKind.Dark, "闇：敵の周囲に闇の波動\n光と反応して対消滅"),
            new WeaponDefinition("dagger", "短剣", UpgradeKind.Dagger, "物理：近接で短剣を素早く突く"),
            new WeaponDefinition("axe", "斧", UpgradeKind.Axe, "物理：前方を広くなぎ払う"),
            new WeaponDefinition("hammer", "ハンマー", UpgradeKind.Hammer, "物理：遅く重い範囲攻撃"),
            new WeaponDefinition("wood", "木の杖", UpgradeKind.Wood, "木：敵の周囲に種を放つ\n水と開花、炎と燃焼、雷と激化"),
            new WeaponDefinition("earth", "土の杖", UpgradeKind.Earth, "土：岩の衝撃で範囲攻撃\n炎・水・氷・雷と結晶シールド"),
            new WeaponDefinition("sword", "剣", UpgradeKind.Sword, "物理：前方を素早く斬る"),
            new WeaponDefinition("shield", "盾", UpgradeKind.Shield, "物理：近くの敵へ体当たり\n攻撃が命中すると小さな盾を獲得"),
            new WeaponDefinition("gauntlet", "ナックル", UpgradeKind.Gauntlet, "物理：短い間隔で打撃を連発"),
            new WeaponDefinition("scythe", "鎌", UpgradeKind.Scythe, "物理：周囲の敵を広く刈り払う")
        };
        public static bool IsPhysical(string id) => id == "orbit" || id == "spear" || id == "dagger" || id == "axe" || id == "hammer" || id == "sword" || id == "shield" || id == "gauntlet" || id == "scythe";
        public static float OfferWeight(string characterId, string weaponId)
        {
            bool ranged = characterId == "ranger" || characterId == "mage";
            return IsPhysical(weaponId) != ranged ? 3 : 1;
        }
        public static WeaponDefinition Find(string id) { foreach (var definition in All) if (definition.Id == id) return definition; return null; }
        public static WeaponBase Get(PlayerStats stats, UpgradeKind kind)
        {
            foreach (var definition in All) if (definition.Kind == kind)
                foreach (var weapon in stats.GetComponents<WeaponBase>()) if (weapon.Id == definition.Id) return weapon;
            return null;
        }
        public static float EstimateBossDps(WeaponBase weapon)
        {
            float level = weapon.Level; if (level <= 0) return 0;
            switch (weapon.Id) {
                case "bolt": return (12 + 4 * level) * (1 + Mathf.Floor((level - 1) / 3) * .55f) / Mathf.Max(.16f, .65f - level * .055f);
                case "orbit": return (8 + 5 * level) / .38f * .6f;
                case "fireball": return (20 + 6 * level) / Mathf.Max(.65f, 1.8f - level * .13f) * .9f;
                case "lightning": return (18 + 7 * level) / Mathf.Max(.7f, 1.8f - level * .1f);
                case "spear": return (14 + 6 * level) / Mathf.Max(.5f, 1.2f - level * .08f) * .65f;
                case "dagger": return (9 + level * 3) / Mathf.Max(.2f, .62f - level * .04f);
                case "axe": return (20 + level * 6) / Mathf.Max(.65f, 1.55f - level * .08f) * .7f;
                case "hammer": return (25 + level * 8) / Mathf.Max(.85f, 2 - level * .1f) * .7f;
                default: return (12 + level * 4) / Mathf.Max(.75f, 2.1f - level * .12f);
            }
        }
    }
}
