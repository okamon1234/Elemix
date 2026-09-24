using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Bullet : MonoBehaviour
    {
        readonly HashSet<int> hit = new HashSet<int>();
        Vector2 direction;
        float damage, speed, expires;
        int pierce;
        bool hostile;
        bool cosmetic;
        PlayerHealth owner;
        Rigidbody2D body;
        public void Launch(Vector2 heading, float power, float velocity, int penetration, bool enemy, PlayerHealth source = null, bool visualOnly = false)
        {
            body = GetComponent<Rigidbody2D>(); direction = heading.normalized;
            damage = power; speed = velocity; pierce = penetration; hostile = enemy; owner = source; cosmetic = visualOnly;
            expires = Time.time + (enemy ? 7 : 2.2f);
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            gameObject.layer = LayerMask.NameToLayer(enemy ? "EnemyBullet" : "PlayerBullet");
        }
        void FixedUpdate()
        {
            if (!body) return;
            body.linearVelocity = direction * speed;
            if (Time.time >= expires) Destroy(gameObject);
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("World")) { Destroy(gameObject); return; }
            if (hostile)
            {
                var player = other.GetComponent<PlayerHealth>();
                if (!player || !player.IsLocal || !player.Alive) return;
                player.Damage(damage); Destroy(gameObject);
            }
            else
            {
                var enemy = other.GetComponent<EnemyHealth>();
                if (!enemy || !enemy.Alive || !hit.Add(enemy.GetInstanceID())) return;
                if (!cosmetic) enemy.Damage(damage, direction, owner, CombatElement.Wind);
                if (pierce-- <= 0) Destroy(gameObject);
            }
        }
    }
}
