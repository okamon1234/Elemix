using UnityEngine;
namespace RogueSurvivors
{
    public sealed class EnemyAilment : MonoBehaviour
    {
        float until, frozenUntil, conductiveUntil,shatterAt;
        public float PhysicalMultiplier => Time.time < conductiveUntil ? 1.4f : 1;
        public void Superconduct() { conductiveUntil = Time.time + 5; }
        public bool Frozen => Time.time < frozenUntil && !GetComponent<EnemyHealth>().IsBoss;
        public float SpeedMultiplier => Frozen ? 0 : Time.time < until ? (GetComponent<EnemyHealth>().IsBoss ? .85f : .35f) : 1;
        public bool TryShatter()
        {
            if(Time.time>=frozenUntil || Time.time<shatterAt)return false;
            shatterAt=Time.time+.8f;return true;
        }
        public void Freeze(float duration) { frozenUntil = Time.time + duration; Slow(duration); }
        public void Slow(float duration) { until = Mathf.Max(until, Time.time + duration); }
    }
}
