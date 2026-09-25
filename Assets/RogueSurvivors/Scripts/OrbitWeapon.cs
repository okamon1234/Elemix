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
        protected override float Interval => .28f;
        protected override void Update()
        {
            int count = Level == 0 || !Health.Alive || PhysicalEvolution.Active(this) ? 0 : 1 + Level / 2;
            while (blades.Count < count) {
                var blade=Instantiate(bladePrefab,transform).transform;blades.Add(blade);
                var renderer=blade.GetComponentInChildren<SpriteRenderer>();var art=ArsenalArt.Weapon("orbit");
                if(renderer && art) {renderer.sprite=art;renderer.color=new Color(1,1,1,.76f);}
            }
            while (blades.Count > count) { Destroy(blades[blades.Count - 1].gameObject); blades.RemoveAt(blades.Count - 1); }
            angle += Time.deltaTime * (110 + Level * 10);
            for (int i = 0; i < blades.Count; i++)
            {
                float theta = (angle + i * 360f / blades.Count) * Mathf.Deg2Rad;
                blades[i].localPosition = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0) * (PhysicalEvolution.Active(this) ? 2.65f : 2.05f);
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
                    if (BladeDistance(enemy.transform.position, blade.position) < (enemy.IsBoss ? 1.3f : .75f))
                    {
                        enemy.Damage((11 + Level * 6) * Stats.DamageMultiplier * PhysicalEvolution.Power(this),
                            (enemy.transform.position - transform.position).normalized, Health);
                        Health.GrantShield(Health.Maximum * .035f, .8f);
                        break;
                    }
            }
            return true;
        }
        float BladeDistance(Vector2 point,Vector2 tip)
        {
            // 刃の先端だけでなく根元から判定し、懐に潜った敵に永久に当たらない穴をなくす。
            Vector2 start=Vector2.Lerp(transform.position,tip,.3f),delta=tip-start;
            float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(point-start,delta)/delta.sqrMagnitude):0;
            return Vector2.Distance(point,start+delta*t);
        }
    }
}
