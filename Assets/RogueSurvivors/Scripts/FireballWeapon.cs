using UnityEngine;
namespace RogueSurvivors
{
    public sealed class FireballWeapon : WeaponBase
    {
        public override string Id => "fireball";
        protected override float Interval => Mathf.Max(.65f, 1.8f - Level * .13f);
        protected override bool Attack()
        {
            var target = GetComponent<AutoTargeting>().FindNearest(); if (!target) return false;
            Vector2 direction = (target.transform.position - transform.position).normalized;
            SpawnAt(transform.position, direction, false);
            GetComponent<NetworkPlayerSync>()?.BroadcastEffect(0, transform.position, (Vector2)transform.position + direction);
            return true;
        }
        public void SpawnAt(Vector2 position, Vector2 direction, bool cosmetic)
        {
            var prefab = Resources.Load<AreaProjectile>("RogueSurvivors/Fireball"); if (!prefab) return;
            WeaponCastVisual.Show("fireball",position,direction);
            var projectile = Instantiate(prefab, position, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            projectile.Launch(direction, (20 + Level * 6) * Stats.DamageMultiplier, 1.3f + Level * .12f, Health, cosmetic);
        }
    }
}
