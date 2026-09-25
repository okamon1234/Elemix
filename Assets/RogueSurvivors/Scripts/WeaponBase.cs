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
            var before=new HashSet<string>(Stats.FusionIds);
            bool elementalBefore=ElementEvolution.Active(this);
            if(!PhysicalEvolution.UpgradeFusion(this)) SetLevel(level+1);
            PhysicalEvolution.Refresh(Stats);
            foreach(var id in Stats.FusionIds) if(!before.Contains(id)) {
                HUDController.Instance?.Toast("合体！　"+PhysicalEvolution.ById(id).Name+" ／ 武器枠が1つ空いた！");
                FusionAssemblyFx.Show(Stats,PhysicalEvolution.ById(id));
                EffectsService.Instance?.Play("Level");
            }
            if(!elementalBefore && ElementEvolution.Active(this)) HUDController.Instance?.Toast("武器進化！　"+ElementEvolution.Name(this));
        }
        protected virtual void Update()
        {
            if (PhysicalEvolution.Active(this) || level == 0 || !Health.Alive || !Health.IsLocal || Time.timeScale == 0) return;
            if (GameManager.Instance && !GameManager.Instance.IsPlaying) return;
            if (Time.time >= nextAttack && Attack()) nextAttack = Time.time + Interval;
        }
        protected abstract float Interval { get; }
        protected abstract bool Attack();
    }
}
