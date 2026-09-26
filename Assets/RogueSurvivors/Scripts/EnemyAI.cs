using UnityEngine;
namespace RogueSurvivors
{
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public sealed class EnemyAI : MonoBehaviour
    {
        public float speed = 1.8f;
        public float contactDamage = 12;
        Rigidbody2D body;
        EnemyHealth health;
        int avoidanceSide;
        EnemyCombat combat;
        void Awake() { body = GetComponent<Rigidbody2D>(); health = GetComponent<EnemyHealth>(); combat = GetComponent<EnemyCombat>(); avoidanceSide = Random.value < .5f ? -1 : 1; if(!GetComponent<EnemyAppearance>())gameObject.AddComponent<EnemyAppearance>(); }
        void FixedUpdate()
        {
            var target = PlayerHealth.Local;
            if (!target || !target.Alive || !health.Alive || !GameManager.Instance || !GameManager.Instance.IsPlaying)
            { body.linearVelocity = Vector2.zero; return; }
            if (GetComponent<EnemyAilment>() && GetComponent<EnemyAilment>().Frozen) { body.linearVelocity = Vector2.zero; return; }
            float slow = GetComponent<EnemyAilment>() ? GetComponent<EnemyAilment>().SpeedMultiplier : 1;
            Vector2 heading = ((Vector2)(target.transform.position - transform.position)).normalized;
            if (combat && combat.OverrideMovement(heading, Vector2.Distance(target.transform.position, transform.position), out var special)) {
                body.linearVelocity = special.sqrMagnitude > 0 ? ObstacleAvoidance.Steer(body.position, special.normalized, .37f, avoidanceSide) * special.magnitude * slow + health.Knockback : Vector2.zero;
                return;
            }
            body.linearVelocity = ObstacleAvoidance.Steer(body.position, heading, .37f, avoidanceSide) * speed * slow + health.Knockback;
        }
        void OnCollisionStay2D(Collision2D hit) { if (!GetComponent<EnemyAilment>() || !GetComponent<EnemyAilment>().Frozen) hit.gameObject.GetComponent<PlayerHealth>()?.Damage(contactDamage); }
        void OnCollisionEnter2D(Collision2D hit) { if (!GetComponent<EnemyAilment>() || !GetComponent<EnemyAilment>().Frozen) hit.gameObject.GetComponent<PlayerHealth>()?.Damage(contactDamage); }
    }
}
