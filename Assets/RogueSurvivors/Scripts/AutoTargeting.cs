using UnityEngine;
namespace RogueSurvivors
{
    public sealed class AutoTargeting : MonoBehaviour
    {
        public float range = 18;
        public EnemyHealth FindNearest()
        {
            EnemyHealth best = null; float distance = range * range;
            foreach (var enemy in EnemyHealth.Active)
            {
                if (!enemy || !enemy.Alive) continue;
                float candidate = (enemy.transform.position - transform.position).sqrMagnitude;
                if (candidate < distance) { best = enemy; distance = candidate; }
            }
            return best;
        }
    }
}
