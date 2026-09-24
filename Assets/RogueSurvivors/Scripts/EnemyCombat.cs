using UnityEngine;
namespace RogueSurvivors
{
    public enum EnemyRole { Skeleton, Rat, Armored, Archer, Charger }
    public sealed class EnemyCombat : MonoBehaviour
    {
        public EnemyRole role;
        public Bullet hostileBullet;
        public float attackMultiplier = 1;
        float nextAttack, windupEnd, dashEnd;
        Vector2 dashDirection;
        public bool OverrideMovement(Vector2 desired, float distance, out Vector2 velocity)
        {
            velocity = Vector2.zero;
            var player = PlayerHealth.Local;
            if (!player || !player.Alive) return false;
            if (role == EnemyRole.Archer) {
                if (distance < 11 && Time.time >= nextAttack && hostileBullet) {
                    nextAttack = Time.time + Mathf.Max(.9f, 2.5f / Mathf.Sqrt(attackMultiplier));
                    Instantiate(hostileBullet, transform.position + (Vector3)desired * .5f, Quaternion.identity).Launch(desired, 10 * attackMultiplier, 4.2f, 0, true);
                    EffectsService.Instance?.Burst(transform.position, new Color(.85f, .55f, .25f));
                }
                if (distance < 5) { velocity = -desired * 1.8f; return true; }
                if (distance < 8) { velocity = new Vector2(-desired.y, desired.x) * .65f; return true; }
            }
            if (role == EnemyRole.Charger) {
                if (Time.time < windupEnd) return true;
                if (Time.time < dashEnd) { velocity = dashDirection * 7; return true; }
                if (distance < 10 && Time.time >= nextAttack) {
                    dashDirection = desired; windupEnd = Time.time + .7f; dashEnd = windupEnd + .65f;
                    EffectsService.Instance?.Popup(transform.position + Vector3.up * .6f, "!", new Color(1, .75f, .25f));
                    nextAttack = dashEnd + 2.5f; GetComponent<HitFeedback>()?.Flash(); return true;
                }
            }
            return false;
        }
    }
}
