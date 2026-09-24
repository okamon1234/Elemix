using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public enum CombatElement { None, Fire, Wind, Lightning, Ice, Water, Light, Dark, Wood, Earth }
    public sealed class ElementReaction : MonoBehaviour
    {
        public CombatElement Aura { get; private set; }
        public float Remaining { get; private set; }
        float cooldown;
        public void Restore(int aura, float remaining) { Aura = (CombatElement)aura; Remaining = remaining; }
        void Update() { Remaining = Mathf.Max(0, Remaining - Time.deltaTime); cooldown = Mathf.Max(0, cooldown - Time.deltaTime); if (Remaining == 0) Aura = CombatElement.None; }
        public float Resolve(float amount, CombatElement incoming, PlayerHealth source)
        {
            if (incoming == CombatElement.None) return amount;
            CombatElement spread = Aura == CombatElement.Wind ? incoming : Aura;
            int reaction = Recipe(Aura, incoming);
            if (reaction != 0 && cooldown <= 0)
            {
                float multiplier = reaction == 12 || reaction == 13 ? 1.3f : reaction == 10 ? 2f : reaction == 8 || reaction == 9 || reaction == 11 ? 1f : reaction == 2 ? 2f : reaction == 6 ? 1f : reaction == 3 ? 2.4f : reaction == 4 ? 2.1f : reaction == 7 ? 2.8f : 1.6f;
                Aura = reaction == 13 ? spread : CombatElement.None; Remaining = reaction == 13 ? 4 : 0; cooldown = GetComponent<EnemyHealth>() && GetComponent<EnemyHealth>().IsBoss ? .75f : .4f;
                var sync = GetComponent<NetworkEnemySync>();
                if (sync && sync.IsNetworked) sync.BroadcastReaction(reaction, (int)spread);
                else Show(reaction, spread);
                if (reaction == 5 || reaction == 7 || reaction == 13)
                    foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active))
                        if (enemy && enemy.gameObject != gameObject && enemy.Alive && Vector2.Distance(transform.position, enemy.transform.position) <= 3.5f)
                        {
                            enemy.ReceiveSecondary(amount * (reaction == 13 ? .45f : .8f), source);
                            if (reaction == 13 && enemy && enemy.Alive) {
                                var aura = enemy.GetComponent<ElementReaction>(); if (!aura) aura = enemy.gameObject.AddComponent<ElementReaction>();
                                aura.Restore((int)spread, 4);
                            }
                        }
                if (reaction == 12) { var ailment = GetComponent<EnemyAilment>(); if (!ailment) ailment = gameObject.AddComponent<EnemyAilment>(); ailment.Superconduct(); }
                if (reaction == 6) { var ailment = GetComponent<EnemyAilment>(); if (!ailment) ailment = gameObject.AddComponent<EnemyAilment>(); ailment.Freeze(1.5f); }
                if (reaction == 8 || reaction == 9) {
                    var periodic = GetComponent<ReactionDamage>(); if (!periodic) periodic = gameObject.AddComponent<ReactionDamage>();
                    periodic.Begin(amount, source, reaction == 8);
                }
                if (reaction == 11 && source) {
                    if (sync && sync.IsNetworked) sync.GiveCrystal(source);
                    else CrystalPickup.Spawn((Vector2)transform.position + ((Vector2)source.transform.position-(Vector2)transform.position).normalized*1.9f, source);
                }
                return amount * multiplier;
            }
            if (reaction != 13 || Aura == CombatElement.None) Aura = incoming; else Aura = spread;
            Remaining = 4;
            return amount;
        }
        public static int Recipe(CombatElement a, CombatElement b)
        {
            if (a == b || a == CombatElement.None || b == CombatElement.None) return 0;
            if ((a == CombatElement.Ice && b == CombatElement.Lightning) || (a == CombatElement.Lightning && b == CombatElement.Ice)) return 12;
            if (a == CombatElement.Wind || b == CombatElement.Wind) {
                var other = a == CombatElement.Wind ? b : a;
                if (other == CombatElement.Fire) return 13;
                return other == CombatElement.Water || other == CombatElement.Ice || other == CombatElement.Lightning ? 13 : 0;
            }
            if (a == CombatElement.Earth || b == CombatElement.Earth) {
                var other = a == CombatElement.Earth ? b : a;
                return other == CombatElement.Fire || other == CombatElement.Water || other == CombatElement.Ice || other == CombatElement.Lightning ? 11 : 0;
            }
            if (a == CombatElement.Wood || b == CombatElement.Wood) {
                var other = a == CombatElement.Wood ? b : a;
                return other == CombatElement.Water ? 8 : other == CombatElement.Fire ? 9 : other == CombatElement.Lightning ? 10 : 0;
            }
            if ((a == CombatElement.Light && b == CombatElement.Dark) || (a == CombatElement.Dark && b == CombatElement.Light)) return 7;
            if (a == CombatElement.Fire || b == CombatElement.Fire) {
                var other = a == CombatElement.Fire ? b : a;
                if (other == CombatElement.Wind) return 13;
                if (other == CombatElement.Lightning) return 2;
                if (other == CombatElement.Ice) return 3;
                if (other == CombatElement.Water) return 4;
            }
            if (a == CombatElement.Water || b == CombatElement.Water) {
                var other = a == CombatElement.Water ? b : a;
                if (other == CombatElement.Lightning) return 5;
                if (other == CombatElement.Ice) return 6;
            }
            return 0;
        }
        public void Show(int reaction, CombatElement spread = CombatElement.Wind)
        {
            string[] names = { "", "", "過負荷", "融解", "蒸発", "感電", "凍結", "対消滅", "開花", "燃焼", "激化", "結晶", "超電導", "拡散" };
            if (reaction < 2 || reaction >= names.Length) return;
            Color color = reaction == 6 ? Color.cyan : reaction == 7 ? new Color(.8f, .5f, 1) : new Color(1, .65f, .2f);
            EffectsService.Instance?.Popup(transform.position + Vector3.up, names[reaction], color);
            EffectsService.Instance?.Burst(transform.position, color);
            CombatFx.Reaction(reaction, spread, transform.position);
        }
    }
}
