using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class OrbitWeapon : WeaponBase
    {
        public GameObject bladePrefab;
        public override string Id => "orbit";
        readonly List<Transform> blades = new List<Transform>();
        float angle;
        protected override float Interval => .38f;
        protected override void Update()
        {
            int count = Level == 0 || !Health.Alive ? 0 : 1 + Level / 2;
            while (blades.Count < count) blades.Add(Instantiate(bladePrefab, transform).transform);
            while (blades.Count > count) { Destroy(blades[blades.Count - 1].gameObject); blades.RemoveAt(blades.Count - 1); }
            angle += Time.deltaTime * (110 + Level * 10);
            for (int i = 0; i < blades.Count; i++)
            {
                float theta = (angle + i * 360f / blades.Count) * Mathf.Deg2Rad;
                blades[i].localPosition = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0) * (PhysicalEvolution.Active(this) ? 2.4f : 1.8f);
                blades[i].localRotation = Quaternion.Euler(0, 0, angle * 2);
            }
            base.Update();
        }
        protected override bool Attack()
        {
            foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active))
            {
                if (!enemy || !enemy.Alive) continue;
                foreach (var blade in blades)
                    if ((enemy.transform.position - blade.position).sqrMagnitude < (enemy.IsBoss ? 2.8f : .85f))
                    {
                        enemy.Damage((8 + Level * 5) * Stats.DamageMultiplier * PhysicalEvolution.Power(this),
                            (enemy.transform.position - transform.position).normalized, Health);
                        break;
                    }
            }
            return true;
        }
    }
}
