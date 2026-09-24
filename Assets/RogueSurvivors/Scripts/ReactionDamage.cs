using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class ReactionDamage : MonoBehaviour
    {
        float burnDamage, burnAt, bloomDamage, bloomAt;
        int burnTicks;
        PlayerHealth burnSource, bloomSource;
        public void Begin(float damage, PlayerHealth source, bool bloom)
        {
            if (bloom) { if (bloomDamage > 0) return; bloomDamage = damage; bloomSource = source; bloomAt = Time.time + .7f; }
            else { burnDamage = Mathf.Max(burnDamage, damage * .3f); burnSource = source; burnTicks = 3; burnAt = Time.time + .6f; }
        }
        void Update()
        {
            var sync = GetComponent<NetworkEnemySync>(); if (sync && sync.IsNetworked && !sync.IsAuthority) return;
            var health = GetComponent<EnemyHealth>(); if (!health || !health.Alive) return;
            if (burnTicks > 0 && Time.time >= burnAt) { burnTicks--; burnAt = Time.time + .6f; health.ReceiveSecondary(burnDamage, burnSource); }
            if (bloomDamage > 0 && Time.time >= bloomAt) {
                float damage = bloomDamage; bloomDamage = 0;
                EffectsService.Instance?.Burst(transform.position, new Color(.4f,1,.3f));
                foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active))
                    if (enemy && enemy.Alive && Vector2.Distance(transform.position,enemy.transform.position) < 2.8f) enemy.ReceiveSecondary(damage, bloomSource);
            }
        }
    }
}
