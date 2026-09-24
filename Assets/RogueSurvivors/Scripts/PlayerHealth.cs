using System;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Local { get; set; }
        public float Current { get; private set; }
        public float Maximum => GetComponent<PlayerStats>().MaxHealth;
        public bool Alive => Current > 0;
        public bool IsLocal { get; set; } = true;
        public event Action Died;
        float invulnerableUntil, shield, shieldUntil;
        public float LastDamageTime { get; private set; }
        public bool HasBarrier { get; private set; }
        public void GrantBarrier() { if (!IsLocal || !Alive || HasBarrier) return; HasBarrier=true; HUDController.Instance?.Toast("結晶バリア獲得：次の一撃を無効化"); CombatFx.Ring(transform.position,.8f,new Color(1,.85f,.25f)); }
        public void SetRemoteBarrier(bool active) => HasBarrier=active;
        public float Shield => Time.time < shieldUntil ? shield : 0;
        public void GrantShield(float amount, float duration) { if (!IsLocal || !Alive) return; shield = Mathf.Min(Maximum * .2f, Mathf.Max(Shield, amount)); shieldUntil = Mathf.Max(shieldUntil, Time.time + duration); }

        void OnEnable()
        {
            if (Local) return;
            var sync = GetComponent<NetworkPlayerSync>();
            if (sync && NetworkManager.InRoom && !sync.IsLocalAuthority) return;
            if (IsLocal) Local = this;
        }
        void Awake() { ResetHealth(); if(!GetComponent<CrystalBarrier>()) gameObject.AddComponent<CrystalBarrier>(); }
        public void ResetHealth() { LastDamageTime = Time.time; Current = Maximum; HasBarrier=false; invulnerableUntil = 0; shield = 0; shieldUntil = 0; }
        public void GrantInvulnerability(float seconds) => invulnerableUntil = Time.time + seconds;
        public void Heal(float amount) { if (Alive) Current = Mathf.Min(Maximum, Current + Mathf.Max(0, amount)); }
        public void SetRemoteHealth(float current) => Current = Mathf.Clamp(current, 0, Maximum);
        public void Damage(float amount)
        {
            if (!IsLocal || !Alive || Time.time < invulnerableUntil || amount <= 0) return;
            if (GameManager.Instance && !GameManager.Instance.IsPlaying) return;
            LastDamageTime = Time.time;
            if(HasBarrier) { HasBarrier=false; invulnerableUntil=Time.time+.65f; CombatFx.Reaction(11,CombatElement.Earth,transform.position); EffectsService.Instance?.Popup(transform.position,"防御",new Color(1,.9f,.35f)); return; }
            amount *= 1 - GetComponent<PlayerStats>().DamageReduction;
            float absorbed = Mathf.Min(Shield, amount); shield = Mathf.Max(0, Shield - absorbed); amount -= absorbed;
            Current = Mathf.Max(0, Current - amount); invulnerableUntil = Time.time + 0.65f;
            GetComponent<HitFeedback>()?.Flash();
            EffectsService.Instance?.Popup(transform.position, "-" + Mathf.CeilToInt(amount), new Color(1, .3f, .4f));
            EffectsService.Instance?.Play("Hurt");
            CameraFollow.Instance?.Shake(.16f);
            if (!Alive)
            {
                Died?.Invoke();
                if (!GetComponent<PlayerRespawn>()) GameManager.Instance?.Finish(false);
            }
        }
        void OnDestroy() { if (Local == this) Local = null; }
    }
}
