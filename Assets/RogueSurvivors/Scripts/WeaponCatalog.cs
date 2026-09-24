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
            new WeaponDefinition("bolt", "風の弓", UpgradeKind.Bolt, "風：遠距離射撃\n炎・水・氷・雷を拡散（属性が残る）\nレベル4・7で矢が増加"),
            new WeaponDefinition("orbit", "回転剣", UpgradeKind.Orbit, "物理：周囲を回る剣で攻撃\n2レベルごとに剣が増加"),
            new WeaponDefinition("fireball", "火の玉", UpgradeKind.Fireball, "【炎】風・雷・氷・水と反応\n強化すると爆発範囲も拡大"),
            new WeaponDefinition("lightning", "雷", UpgradeKind.Lightning, "【雷】炎と過負荷、水と感電\n氷と超電導で物理を強化\n強化すると連鎖する数が増加"),
            new WeaponDefinition("spear", "槍", UpgradeKind.Spear, "物理：一直線に並ぶ敵を貫く\n一直線に並んだ敵を貫く"),
            new WeaponDefinition("ice", "氷の杖", UpgradeKind.Ice, "氷：扇状の氷片と氷柱・減速\nLv3/5/8で範囲増加、Lv5から2連射"),
            new WeaponDefinition("water", "水の杖", UpgradeKind.Water, "水：連続する波で範囲攻撃\nLv3/5/8で波が2→3→4→5回に増加"),
            new WeaponDefinition("light", "光の杖", UpgradeKind.Light, "光：敵を貫く光の槍\nLv3/5/8で槍増加。闇と両方Lv4で進化"),
            new WeaponDefinition("dark", "闇の杖", UpgradeKind.Dark, "闇：引き寄せて2段階の崩壊\nLv3/5/8で拡大。光と両方Lv4で進化"),
            new WeaponDefinition("dagger", "短剣", UpgradeKind.Dagger, "物理：近接で短剣を素早く突く"),
            new WeaponDefinition("axe", "斧", UpgradeKind.Axe, "物理：前方を広くなぎ払う"),
            new WeaponDefinition("hammer", "ハンマー", UpgradeKind.Hammer, "物理：遅く重い範囲攻撃"),
            new WeaponDefinition("wood", "木の杖", UpgradeKind.Wood, "木：左右に生える棘の茂み・減速\nLv3/5/8で棘と連続発生数が増加"),
            new WeaponDefinition("earth", "土の杖", UpgradeKind.Earth, "土：岩柱を地面から突き上げる\nLv3/5/8で範囲拡大、Lv5で2連撃\n結晶を拾うと一撃無効（保持1個）"),
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
        public static string UpgradeDescription(WeaponBase weapon)
        {
            string text=Find(weapon.Id)?.Description ?? "";
            if(weapon is ElementWeapon) {
                int next=weapon.Level+1;
                text+="\n威力 "+(15+weapon.Level*5)+"→"+(15+next*5)+"／発動間隔短縮";
                if(StaffCast.Tier(next)>StaffCast.Tier(weapon.Level)) text+="\n★攻撃の段階が上昇！";
            }
            return text;
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
                case "orbit": return (11 + 6 * level) / .28f * .7f * PhysicalEvolution.Power(weapon);
                case "fireball": return (20 + 6 * level) / Mathf.Max(.65f, 1.8f - level * .13f) * .9f;
                case "lightning": return (18 + 7 * level) / Mathf.Max(.7f, 1.8f - level * .1f);
                case "spear": return (17 + 7 * level) / Mathf.Max(.45f, 1.1f - level * .08f) * .65f * PhysicalEvolution.Power(weapon);
                case "dagger": case "gauntlet": return (12+level*4)/Mathf.Max(.18f,.52f-level*.035f)*.75f*PhysicalEvolution.Power(weapon);
                case "axe": case "scythe": return (24+level*7)/Mathf.Max(.5f,1.25f-level*.075f)*.7f*PhysicalEvolution.Power(weapon);
                case "sword": case "shield": return (20+level*6)/Mathf.Max(.5f,1.25f-level*.075f)*.7f*PhysicalEvolution.Power(weapon);
                case "hammer": return (30+level*9)/Mathf.Max(.7f,1.65f-level*.1f)*.7f*PhysicalEvolution.Power(weapon);
                default:
                    float pulses=weapon.Id=="water" || weapon.Id=="wood" ? 1+.45f*(1+StaffCast.Tier((int)level)) : weapon.Id=="dark"?1.85f:level>=5?1.45f:1;
                    return (15+level*5)*pulses/Mathf.Max(.75f,2.1f-level*.12f)*(ElementEvolution.Active(weapon)?1.5f:1);
            }
        }
    }
}
