using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public abstract class PhysicalWeapon : WeaponBase
    {
        protected override float Interval => (Id == "dagger" || Id == "gauntlet") ? Mathf.Max(.2f, .62f - Level * .04f) : Id == "hammer" ? Mathf.Max(.85f, 2 - Level * .1f) : Mathf.Max(.65f, 1.55f - Level * .08f);
        protected override bool Attack()
        {
            var target = GetComponent<AutoTargeting>().FindNearest(); if (!target) return false;
            bool evolved = PhysicalEvolution.Active(this);
            float reach = (Id == "dagger" || Id == "gauntlet") ? 1.9f : Id == "shield" ? 2.1f : Id == "hammer" ? 2.6f : Id == "scythe" ? 3.3f : 2.8f;
            if (evolved) reach += .65f;
            Vector2 origin = transform.position, end = target.transform.position;
            if (Vector2.Distance(origin, end) > reach) return false;
            float damage = (Id == "hammer" ? 25 + Level * 8 : Id == "axe" || Id == "scythe" ? 20 + Level * 6 : Id == "sword" || Id == "shield" ? 16 + Level * 5 : 9 + Level * 3) * Stats.DamageMultiplier * PhysicalEvolution.Power(this);
            foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if (!enemy || !enemy.Alive) continue;
                bool hit = (Id == "dagger" || Id == "gauntlet") && !evolved ? enemy == target : Vector2.Distance(origin, enemy.transform.position) <= reach && Vector2.Dot(((Vector2)enemy.transform.position-origin).normalized, (end-origin).normalized) > (Id == "scythe" ? -1.1f : evolved ? -.25f : .25f);
                if (hit) enemy.Damage(damage, (enemy.transform.position-transform.position).normalized, Health);
            }
            if (Id == "shield") Health.GrantShield(Health.Maximum * .06f, 2);
            int visual = Id == "dagger" ? 0 : Id == "axe" ? 1 : Id == "hammer" ? 2 : Id == "sword" ? 3 : Id == "shield" ? 4 : Id == "gauntlet" ? 5 : 6;
            Show(visual, origin, end);
            GetComponent<NetworkPlayerSync>()?.BroadcastEffect(30 + visual, origin, end);
            return true;
        }
        public static void Show(int kind, Vector2 origin, Vector2 end)
        {
            var go = new GameObject("近接武器"); var sprite = go.AddComponent<SpriteRenderer>(); sprite.sprite = WeaponArt.Get(kind); sprite.sortingLayerName = "Projectiles";
            sprite.sharedMaterial = Resources.Load<GameObject>("RogueSurvivors/PlayerBullet").GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            go.transform.position = Vector2.Lerp(origin,end,.65f); go.transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(end.y-origin.y,end.x-origin.x)*Mathf.Rad2Deg);
            Object.Destroy(go,.22f);
            if (kind != 0) EffectsService.Instance?.Burst(end, new Color(.9f, .8f, .55f));
        }
    }
}
