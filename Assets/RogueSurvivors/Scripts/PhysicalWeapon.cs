using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public abstract class PhysicalWeapon : WeaponBase
    {
        protected override float Interval => (Id == "dagger" || Id == "gauntlet") ? Mathf.Max(.18f, .52f - Level * .035f) : Id == "hammer" ? Mathf.Max(.7f, 1.65f - Level * .10f) : Mathf.Max(.5f, 1.25f - Level * .075f);
        protected override bool Attack()
        {
            var target = GetComponent<AutoTargeting>().FindNearest(); if (!target) return false;
            bool evolved = PhysicalEvolution.Active(this);
            float reach = (Id == "dagger" || Id == "gauntlet") ? 2.15f : Id == "shield" ? 2.4f : Id == "hammer" ? 2.9f : Id == "scythe" ? 3.3f : 2.8f;
            if (evolved) reach += .65f;
            Vector2 origin = transform.position, end = target.transform.position;
            if (Vector2.Distance(origin, end) > reach) return false;
            float damage = (Id == "hammer" ? 30 + Level * 9 : Id == "axe" || Id == "scythe" ? 24 + Level * 7 : Id == "sword" || Id == "shield" ? 20 + Level * 6 : 12 + Level * 4) * Stats.DamageMultiplier * PhysicalEvolution.Power(this);
            foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if (!enemy || !enemy.Alive) continue;
                bool hit = Vector2.Distance(origin, enemy.transform.position) <= reach && Vector2.Dot(((Vector2)enemy.transform.position-origin).normalized, (end-origin).normalized) > (Id == "scythe" ? -1.1f : evolved ? -.25f : .25f);
                if (hit) { enemy.Damage(damage, (enemy.transform.position-transform.position).normalized, Health); Health.GrantShield(Health.Maximum * .035f, .8f); }
            }
            if (Id == "shield") Health.GrantShield(Health.Maximum * .06f, 2);
            int visual = Id == "dagger" ? 0 : Id == "axe" ? 1 : Id == "hammer" ? 2 : Id == "sword" ? 3 : Id == "shield" ? 4 : Id == "gauntlet" ? 5 : 6;
            Show(visual, origin, end);
            GetComponent<NetworkPlayerSync>()?.BroadcastEffect(30 + visual, origin, end);
            return true;
        }
        public static void Show(int kind, Vector2 origin, Vector2 end)
        {
            WeaponSwingVisual.Create(kind, origin, end);
        }
    }
}
