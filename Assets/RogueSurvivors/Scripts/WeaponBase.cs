using UnityEngine;
using System.Collections.Generic;
namespace RogueSurvivors
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField, Range(0, 8)] int level;
        public int Level => level;
        public abstract string Id { get; }
        protected PlayerStats Stats { get; private set; }
        protected PlayerHealth Health { get; private set; }
        float nextAttack;
        protected virtual void Awake() { Stats = GetComponent<PlayerStats>(); Health = GetComponent<PlayerHealth>(); }
        public void SetLevel(int value) { level = Mathf.Clamp(value, 0, 8); nextAttack = 0; }
        public void Upgrade()
        {
            var before = new HashSet<string>();
            foreach (var weapon in GetComponents<WeaponBase>()) if (PhysicalEvolution.Active(weapon)) before.Add(weapon.Id);
            SetLevel(level + 1);
            foreach (var weapon in GetComponents<WeaponBase>()) if (PhysicalEvolution.Active(weapon) && !before.Contains(weapon.Id))
                HUDController.Instance?.Toast("武器進化！　" + PhysicalEvolution.Name(weapon));
        }
        protected virtual void Update()
        {
            if (level == 0 || !Health.Alive || !Health.IsLocal || Time.timeScale == 0) return;
            if (GameManager.Instance && !GameManager.Instance.IsPlaying) return;
            if (Time.time >= nextAttack && Attack()) nextAttack = Time.time + Interval;
        }
        protected abstract float Interval { get; }
        protected abstract bool Attack();
    }
}
