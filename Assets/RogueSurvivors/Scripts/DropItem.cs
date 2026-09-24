using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public enum DropKind { Heal, Magnet, Nova }
    public sealed class DropItem : MonoBehaviour
    {
        public DropKind kind;
        PlayerHealth target;
        bool used;
        public void SetKind(DropKind value)
        {
            kind = value;
            var sprite = GetComponent<SpriteRenderer>();
            var icon = Resources.Load<Sprite>("RogueSurvivors/Art/" + (kind == DropKind.Heal ? "potion" : kind == DropKind.Magnet ? "magnet" : "bomb"));
            if (icon) sprite.sprite = icon; sprite.color = Color.white;
        }
        void Start() => SetKind(kind);
        public void Attract(PlayerHealth player) => target = player;
        void Update()
        {
            if (!target || !target.Alive || used) return;
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, 9 * Time.deltaTime);
            if ((transform.position - target.transform.position).sqrMagnitude < .3f) Use(target);
        }
        public void Use(PlayerHealth player)
        {
            if (used || !player || !player.Alive) return;
            used = true;
            if (kind == DropKind.Heal) player.Heal(player.Maximum);
            if (kind == DropKind.Magnet)
                foreach (var orb in ExpOrb.Active) if (orb) orb.Attract(player);
            if (kind == DropKind.Nova)
            {
                foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active))
                {
                    if (!enemy || enemy.IsBoss) continue;
                    Vector3 view = Camera.main.WorldToViewportPoint(enemy.transform.position);
                    if (view.z > 0 && view.x >= 0 && view.x <= 1 && view.y >= 0 && view.y <= 1)
                        enemy.Damage(enemy.Current, Vector2.zero, player);
                }
                CameraFollow.Instance?.Shake(.25f);
            }
            EffectsService.Instance?.Burst(transform.position, GetComponent<SpriteRenderer>().color);
            EffectsService.Instance?.Play("Level");
            HUDController.Instance?.Toast(kind == DropKind.Heal ? "HP全回復" : kind == DropKind.Magnet ? "経験値を引き寄せた" : "画面内の敵を一掃した");
            Destroy(gameObject);
        }
    }
}
