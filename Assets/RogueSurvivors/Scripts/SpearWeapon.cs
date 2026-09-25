using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class SpearWeapon : WeaponBase
    {
        public override string Id => "spear";
        protected override float Interval => Mathf.Max(.45f, 1f - Level * .08f);
        protected override bool Attack()
        {
            var target = GetComponent<AutoTargeting>().FindNearest(); if (!target) return false;
            float reach = 3.2f + Level * .12f + (PhysicalEvolution.Active(this) ? 1.4f : 0);
            if (Vector2.Distance(transform.position, target.transform.position) > reach + (target.IsBoss ? 1 : 0)) return false;
            Vector2 origin = transform.position, heading = ((Vector2)target.transform.position - origin).normalized;
            foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if (!enemy || !enemy.Alive) continue;
                Vector2 offset = (Vector2)enemy.transform.position - origin;
                float along = Vector2.Dot(offset, heading), width = enemy.IsBoss ? 1.4f : .9f;
                if (along >= 0 && along <= reach + (enemy.IsBoss ? 1 : 0) && (offset - heading * along).sqrMagnitude < width * width)
                {
                    enemy.Damage((17 + Level * 7) * Stats.DamageMultiplier * PhysicalEvolution.Power(this), heading, Health);
                    Health.GrantShield(Health.Maximum*.035f,.8f);
                }
            }
            CombatVisuals.Spear(origin, origin + heading * reach); GetComponent<NetworkPlayerSync>()?.BroadcastEffect(2, origin, origin + heading * reach);
            return true;
        }
    }
}
