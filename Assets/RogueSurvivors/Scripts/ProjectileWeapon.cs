using UnityEngine;
namespace RogueSurvivors
{
    public sealed class ProjectileWeapon : WeaponBase
    {
        public Bullet bulletPrefab;
        public override string Id => "bolt";
        protected override float Interval => Mathf.Max(.16f, .65f - Level * .055f);
        protected override bool Attack()
        {
            var target = GetComponent<AutoTargeting>().FindNearest();
            if (!target || !bulletPrefab) return false;
            var direction = (target.transform.position - transform.position).normalized;
            int count = 1 + (Level - 1) / 3;
            var volley=new BulletVolley();
            for (int i = 0; i < count; i++)
            {
                var spread = Quaternion.Euler(0, 0, (i - (count - 1) * .5f) * 12) * direction;
                var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                bullet.Volley=volley;
                bullet.Launch(spread, (12 + 4 * Level) * Stats.DamageMultiplier, 12, Level / 4, false, Health);
                GetComponent<NetworkPlayerSync>()?.BroadcastShot(spread, Level / 4);
            }
            EffectsService.Instance?.Play("Shoot", .16f);
            return true;
        }
    }
}
