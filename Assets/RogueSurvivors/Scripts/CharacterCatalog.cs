using UnityEngine;
namespace RogueSurvivors
{
    public sealed class CharacterDefinition
    {
        public readonly string Id, Name, Description;
        public readonly Color Color;
        public CharacterDefinition(string id, string name, string description, Color color)
        { Id = id; Name = name; Description = description; Color = color; }
    }
    public static class CharacterCatalog
    {
        public static readonly CharacterDefinition[] All = {
            new CharacterDefinition("ranger", "アーチャー", "初期武器：風の弓 Lv.2\n能力：移動速度＋0.6\n属性武器が出やすい", new Color(.25f, .85f, 1)),
            new CharacterDefinition("warden", "ウォリアー", "初期武器：回転剣 Lv.2\n能力：取得範囲＋0.8\n物理武器が出やすい", new Color(.75f, .55f, 1)),
            new CharacterDefinition("knight", "ナイト", "初期武器：剣・盾 Lv1\n5秒無傷で毎秒HP1回復\n被ダメージ10%減\n物理武器が出やすい", new Color(1, .65f, .3f)),
            new CharacterDefinition("mage", "メイジ", "火の玉 レベル2\n攻撃力＋10%\n属性武器が出やすい", new Color(1, .55f, .25f)),
            new CharacterDefinition("lancer", "ランサー", "槍 レベル2\n移動＋0.4／ダメージ5%減\n物理武器が出やすい", new Color(.55f, .8f, 1))
        };
        public static CharacterDefinition Find(string id)
        {
            foreach (var character in All) if (character.Id == id) return character;
            return All[0];
        }
        public static PlayerDataData CreateBuild(string id)
        {
            var selected = Find(id); var build = new PlayerDataData { characterId = selected.Id };
            switch (selected.Id)
            {
                case "warden":
                    build.maxHealth = 110; build.pickupRadius = 3.2f;
                    build.weapons.Add(new WeaponSaveData("orbit", 2)); break;
                case "knight":
                    build.maxHealth = 150; build.moveSpeed = 4.2f; build.regeneration = 1; build.damageReduction = .1f;
                    build.weapons.Add(new WeaponSaveData("sword", 1)); build.weapons.Add(new WeaponSaveData("shield", 1)); break;
                case "mage":
                    build.maxHealth = 80; build.moveSpeed = 5.2f; build.damageMultiplier = 1.1f;
                    build.weapons.Add(new WeaponSaveData("fireball", 2)); break;
                case "lancer":
                    build.maxHealth = 120; build.moveSpeed = 5.4f; build.damageReduction = .05f;
                    build.weapons.Add(new WeaponSaveData("spear", 2)); break;
                default:
                    build.maxHealth = 90; build.moveSpeed = 5.6f;
                    build.weapons.Add(new WeaponSaveData("bolt", 2)); break;
            }
            return build;
        }
    }
}
