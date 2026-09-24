using UnityEngine;
namespace RogueSurvivors
{
    public sealed class EnemyAffinity : MonoBehaviour
    {
        public CombatElement Element { get; private set; }
        public void Initialize(CombatElement element)
        {
            Element = element;
            var sprite = GetComponentInChildren<SpriteRenderer>(); if (sprite) sprite.color = ElementWeapon.ColorFor(element);
            GetComponent<HitFeedback>()?.RefreshColor();
            var aura = GetComponent<ElementReaction>(); if (!aura) aura = gameObject.AddComponent<ElementReaction>();
            aura.Restore((int)element, 4);
        }
        float nextAura;
        void Update()
        {
            if (Time.time < nextAura) return;
            nextAura = Time.time + 6;
            var reaction = GetComponent<ElementReaction>();
            if (reaction && reaction.Aura == CombatElement.None) reaction.Restore((int)Element, 4);
        }
        public float Multiplier(CombatElement incoming) => incoming != CombatElement.None && incoming == Element ? .8f : 1;
    }
}
