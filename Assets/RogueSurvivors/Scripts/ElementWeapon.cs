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
            bool evolved=ElementEvolution.Active(this);
            StaffCast.Create(Element,origin,end,Level,(15+Level*5)*Stats.DamageMultiplier,Health,evolved);
            GetComponent<NetworkPlayerSync>()?.BroadcastEffect(1000+(int)Element*100+Level*2+(evolved?1:0),origin,end);
            return true;
        }
        public static void Show(CombatElement element, Vector2 origin, Vector2 end)
        {
            StaffCast.Create(element,origin,end,1,0,null);
        }
    }
}
