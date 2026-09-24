using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public abstract class ElementWeapon : WeaponBase
    {
        protected abstract CombatElement Element { get; }
        protected override float Interval => Mathf.Max(.75f, 2.1f - Level * .12f);
        public static Color ColorFor(CombatElement element)
        {
            switch (element) {
                case CombatElement.Fire: return new Color(1, .45f, .2f);
                case CombatElement.Wind: return new Color(.35f, 1, .65f);
                case CombatElement.Ice: return new Color(.65f, 1, 1);
                case CombatElement.Water: return new Color(.2f, .6f, 1);
                case CombatElement.Light: return new Color(1, .95f, .55f);
                case CombatElement.Wood: return new Color(.45f,.85f,.25f);
                case CombatElement.Earth: return new Color(.85f,.65f,.3f);
                case CombatElement.Dark: return new Color(.75f, .35f, 1);
                default: return new Color(.7f, .7f, 1);
            }
        }
        protected override bool Attack()
        {
            var target = GetComponent<AutoTargeting>().FindNearest(); if (!target) return false;
            Vector2 origin = transform.position, end = target.transform.position;
            if (Vector2.Distance(origin, end) > 9) return false;
            float radius = Element == CombatElement.Water ? 1.5f + Level * .08f : Element == CombatElement.Dark ? 1.9f : Element == CombatElement.Wood ? 1.25f : Element == CombatElement.Earth ? 1.7f : .55f;
            Vector2 heading = (end - origin).normalized;
            foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active)) {
                if (!enemy || !enemy.Alive) continue;
                Vector2 offset = (Vector2)enemy.transform.position - origin;
                bool hit = Vector2.Distance(end, enemy.transform.position) <= radius;
                if (Element == CombatElement.Light || Element == CombatElement.Wind)
                    hit = Vector2.Dot(offset, heading) >= 0 && Vector2.Dot(offset, heading) <= 9 && Mathf.Abs(offset.x * heading.y - offset.y * heading.x) <= .6f + Level * .05f;
                if (!hit) continue;
                enemy.Damage((12 + Level * 4) * Stats.DamageMultiplier, heading, Health, Element);
                if (Element == CombatElement.Ice && !enemy.IsBoss) { var status = enemy.GetComponent<EnemyAilment>(); if (!status) status = enemy.gameObject.AddComponent<EnemyAilment>(); status.Slow(1); }
            }
            Show(Element, origin, end);
            GetComponent<NetworkPlayerSync>()?.BroadcastEffect(10 + (int)Element, origin, end);
            return true;
        }
        public static void Show(CombatElement element, Vector2 origin, Vector2 end)
        {
            var go = new GameObject("属性魔法 " + element); var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = Resources.Load<GameObject>("RogueSurvivors/PlayerBullet").GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            line.sortingLayerName = "Projectiles"; line.startColor = line.endColor = ColorFor(element);
            line.startWidth = element == CombatElement.Light ? .3f : .14f; line.endWidth = .06f;
            line.positionCount = 3; line.SetPosition(0, origin); line.SetPosition(1, (origin + end) / 2); line.SetPosition(2, end);
            Object.Destroy(go, .22f); EffectsService.Instance?.Burst(end, ColorFor(element));
        }
    }
}
