using UnityEngine;
namespace RogueSurvivors
{
    public sealed class BossDifficulty : MonoBehaviour
    {
        public int TeamLevel { get; private set; } = 1;
        public float Health { get; private set; } = 2400;
        public float DamageScale { get; private set; } = 1;
        public float AttackRate { get; private set; } = 1;
        public void Configure(PlayerStats[] players)
        {
            int total = 0; float dps = 0, durability = 0;
            foreach (var player in players) {
                total += player.Level; durability += player.MaxHealth;
                foreach (var weapon in player.GetComponents<WeaponBase>()) {
                    if (weapon.Level == 0) continue;
                    dps += WeaponCatalog.EstimateBossDps(weapon) * player.DamageMultiplier;
                }
            }
            TeamLevel = Mathf.Max(1, total);
            Health = Mathf.Ceil(1700 + TeamLevel * 205 + dps * 44);
            DamageScale = Mathf.Clamp((1.1f + TeamLevel * .015f) * Mathf.Pow(Mathf.Max(1, durability / Mathf.Max(1, players.Length) / 100), .25f), 1, 8);
            AttackRate = Mathf.Clamp(1.1f + TeamLevel * .009f, 1, 3);
        }
        public void ApplyNetwork(int level, float damage, float frequency)
        { TeamLevel = level; DamageScale = damage; AttackRate = frequency; }
    }
}
