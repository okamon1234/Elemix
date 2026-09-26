using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class AreaProjectile : MonoBehaviour
    {
        Vector2 direction;
        float damage, radius, expires;
        PlayerHealth owner;
        bool cosmetic;
        public void Launch(Vector2 heading, float power, float area, PlayerHealth source, bool visualOnly)
        { direction = heading.normalized; damage = power; radius = area; owner = source; cosmetic = visualOnly; expires = Time.time + 4;
            var sprite=GetComponentInChildren<SpriteRenderer>();var art=BattleArt.Effect(0);
            if(sprite && art){sprite.sprite=art;sprite.color=Color.white;sprite.transform.localScale=Vector3.one*.8f;sprite.sortingLayerName="Projectiles";sprite.sortingOrder=15;} }
        void Update()
        {
            Vector2 previous = transform.position; Vector2 next = previous + direction * (9 * Time.deltaTime);
            transform.position = next;
            if (Time.time >= expires || Physics2D.Linecast(previous, next, LayerMask.GetMask("World"))) { Explode(); return; }
            foreach (var enemy in EnemyHealth.Active) {
                if (!enemy || !enemy.Alive) continue;
                Vector2 offset = (Vector2)enemy.transform.position - previous, step = next - previous;
                float t = step.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(offset, step) / step.sqrMagnitude) : 0;
                if (Vector2.Distance(previous + step * t, enemy.transform.position) < (enemy.IsBoss ? 1.2f : .5f)) { Explode(); return; }
            }
        }
        void Explode()
        {
            PaintedImpact.Show(0,transform.position,radius*1.65f,.42f,0,90); EffectsService.Instance?.Burst(transform.position, new Color(1, .5f, .1f));
            if (!cosmetic) foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active))
                if (enemy && enemy.Alive && Vector2.Distance(enemy.transform.position, transform.position) <= radius + (enemy.IsBoss ? 1 : .3f))
                    enemy.Damage(damage, (enemy.transform.position - transform.position).normalized, owner, CombatElement.Fire);
            Destroy(gameObject);
        }
    }
}
