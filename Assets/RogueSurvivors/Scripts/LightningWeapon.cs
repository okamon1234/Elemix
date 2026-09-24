using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class LightningWeapon : WeaponBase
    {
        public override string Id => "lightning";
        protected override float Interval => Mathf.Max(.7f, 1.8f - Level * .1f);
        protected override bool Attack()
        {
            var hit = new HashSet<EnemyHealth>(); Vector2 origin = transform.position; bool attacked = false;
            for (int i = 0; i < 1 + Level / 2; i++) {
                EnemyHealth target = null; float closest = i == 0 ? 10 : 4;
                foreach (var enemy in EnemyHealth.Active) {
                    if (!enemy || !enemy.Alive || hit.Contains(enemy)) continue;
                    float distance = Vector2.Distance(enemy.transform.position, origin);
                    if (distance < closest) { closest = distance; target = enemy; }
                }
                if (!target) break;
                Vector2 end = target.transform.position; hit.Add(target);
                target.Damage((18 + Level * 7) * Stats.DamageMultiplier, Vector2.zero, Health, CombatElement.Lightning);
                CombatVisuals.Lightning(origin, end); GetComponent<NetworkPlayerSync>()?.BroadcastEffect(1, origin, end);
                origin = end; attacked = true;
            }
            return attacked;
        }
    }
}
