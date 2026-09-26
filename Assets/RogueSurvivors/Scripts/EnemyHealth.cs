using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        public static readonly HashSet<EnemyHealth> Active = new HashSet<EnemyHealth>();
        public float maximum = 28;
        public bool IsBoss;
        public ExpOrb orbPrefab;
        public DropItem itemPrefab;
        public int experienceValue = 2;
        public float Current { get; private set; }
        public bool Alive => Current > 0;
        public Vector2 Knockback { get; private set; }
        bool died;
        PlayerHealth meleeFinisher;
        void Awake() => Current = maximum;
        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);
        public void Scale(float multiplier) { maximum *= multiplier; Current = maximum; }
        void Update() => Knockback = Vector2.Lerp(Knockback, Vector2.zero, Time.deltaTime * 10);
        public void SetSynchronizedHealth(float current, float max)
        {
            maximum = max; Current = current;
            if (Current <= 0 && !died) Die(false);
        }
        public void Damage(float amount, Vector2 direction, PlayerHealth source = null, CombatElement element = CombatElement.None)
        {
            if (!Alive || amount <= 0) return;
            var sync = GetComponent<NetworkEnemySync>();
            if (sync && sync.IsNetworked) { sync.RequestDamage(amount, source, element); return; }
            ReceiveHit(amount, direction, source, element);
        }
        public void ReceiveHit(float amount, Vector2 direction, PlayerHealth source, CombatElement element)
        {
            if (!Alive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            var bossAI = GetComponent<BossAI>(); if (bossAI && (bossAI.IsTransforming || (bossAI.Phase < 3 && Current <= maximum * .5f))) return;
            var reactions = GetComponent<ElementReaction>();
            if (!reactions) reactions = gameObject.AddComponent<ElementReaction>();
            if (element == CombatElement.None && GetComponent<EnemyAilment>()) amount *= GetComponent<EnemyAilment>().PhysicalMultiplier;
            var affinity = GetComponent<EnemyAffinity>(); if (affinity) amount *= affinity.Multiplier(element);
            amount = reactions.Resolve(amount, element, source);
            var parts = GetComponent<BossParts>();
            Vector2 origin = source ? (Vector2)source.transform.position : (Vector2)transform.position - direction;
            bool bypass=false;var reward=GetComponent<BossBreakReward>();
            if(reward && source)amount=reward.Resolve(amount,origin,element==CombatElement.None,false,out bypass);
            if (parts && !bypass && parts.Absorb(amount * (element == CombatElement.None ? 1.35f : 1), origin)) return;
            if (bossAI && bossAI.Phase < 3) amount = Mathf.Min(amount, Mathf.Max(0, Current - maximum * .5f));
            meleeFinisher=element==CombatElement.None && source && Vector2.Distance(source.transform.position,transform.position)<=4 ? source : null;
            ApplyDamage(amount, direction);
        }
        public void ReceiveSecondary(float amount, PlayerHealth source)
        {
            var sync = GetComponent<NetworkEnemySync>(); if (sync && sync.IsNetworked && !sync.IsAuthority) return;
            if (!Alive || amount <= 0) return;
            var transformingBoss=GetComponent<BossAI>(); if(transformingBoss && transformingBoss.IsTransforming) return;
            var parts = GetComponent<BossParts>();
            bool bypass=false;var reward=GetComponent<BossBreakReward>();
            if(reward && source)amount=reward.Resolve(amount,source.transform.position,false,true,out bypass);
            if (parts && !bypass && parts.Absorb(amount, source ? (Vector2)source.transform.position : (Vector2)transform.position)) return;
            var bossAI = GetComponent<BossAI>();
            if (bossAI && bossAI.IsTransforming) return;
            if (bossAI && bossAI.Phase < 3) amount = Mathf.Min(amount, Mathf.Max(0, Current - maximum * .5f));
            meleeFinisher=null;
            ApplyDamage(amount, Vector2.zero);
        }
        // Raw health mutation for authority state and deterministic verification only.
        public void ApplyDamage(float amount, Vector2 direction)
        {
            if (!Alive || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            if (!IsBoss) Knockback = direction * 3.5f;
            GetComponent<HitFeedback>()?.Flash();
            EffectsService.Instance?.Popup(transform.position, Mathf.CeilToInt(amount).ToString(), Color.white);
            if (!Alive) Die(true);
        }
        void Die(bool drops)
        {
            if (died) return;
            died = true;
            EffectsService.Instance?.Burst(transform.position, IsBoss ? new Color(1, .35f, .6f) : new Color(.6f, .4f, 1));
            if (IsBoss)
            {
                GetComponent<NetworkEnemySync>()?.AnnounceDefeat();
                GameManager.Instance?.Finish(true);
                return;
            }
            if (drops)
            {
                GameManager.Instance?.AddKill();
                if (orbPrefab) {
                    var orb=Instantiate(orbPrefab,transform.position,Quaternion.identity); orb.value=experienceValue;
                    if(meleeFinisher && meleeFinisher.IsLocal && meleeFinisher.Alive) {
                        var rewards=meleeFinisher.GetComponent<MeleeRewards>(); if(!rewards) rewards=meleeFinisher.gameObject.AddComponent<MeleeRewards>();
                        orb.value=rewards.Reward(experienceValue); orb.Attract(meleeFinisher);
                    }
                }
                if (itemPrefab && Random.value < .055f)
                {
                    var item = Instantiate(itemPrefab, transform.position + Vector3.right * .3f, Quaternion.identity);
                    item.SetKind((DropKind)Random.Range(0, 3));
                }
            }
            Destroy(gameObject);
        }
    }
}
