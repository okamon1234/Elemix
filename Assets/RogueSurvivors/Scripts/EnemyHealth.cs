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
        PlayerHealth meleeFinisher,reactionFinisher;
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
        public void ReceiveHit(float amount, Vector2 direction, PlayerHealth source, CombatElement element, bool? sourceBarrier = null)
        {
            if (!Alive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            var bossAI = GetComponent<BossAI>(); if (bossAI && (bossAI.IsTransforming || (bossAI.Phase < 3 && Current <= maximum * .5f))) return;
            var reactions = GetComponent<ElementReaction>();
            if (!reactions) reactions = gameObject.AddComponent<ElementReaction>();
            int impact=0;
            if(element==CombatElement.None && source)amount*=ReactionBalance.PhysicalPower;
            var ailment=GetComponent<EnemyAilment>();
            if(element==CombatElement.None && ailment) {
                amount*=ailment.PhysicalMultiplier;
                if(ailment.TryShatter()){amount+=ReactionBalance.Power(amount,source)*.8f;impact=16;var network=GetComponent<NetworkEnemySync>();if(network && network.IsNetworked)network.BroadcastReaction(16);else reactions.Show(16);}
            }
            var affinity = GetComponent<EnemyAffinity>(); if (affinity) amount *= affinity.Multiplier(element);
            amount = reactions.Resolve(amount, element, source, sourceBarrier);
            if(reactions.LastReaction>0)impact=reactions.LastReaction;
            var parts = GetComponent<BossParts>();
            Vector2 origin = source ? (Vector2)source.transform.position : (Vector2)transform.position - direction;
            bool bypass=false;var reward=GetComponent<BossBreakReward>();
            if(reward && source)amount=reward.Resolve(amount,origin,element==CombatElement.None,false,out bypass);
            if (parts && !bypass && AbsorbImpact(parts,amount * (element == CombatElement.None ? 1.35f : 1),origin,impact)) return;
            if (bossAI && bossAI.Phase < 3) amount = Mathf.Min(amount, Mathf.Max(0, Current - maximum * .5f));
            meleeFinisher=element==CombatElement.None && source && Vector2.Distance(source.transform.position,transform.position)<=4 ? source : null;
            ApplyDamage(amount, direction,impact,source);
        }
        public void ReceiveSecondary(float amount, PlayerHealth source,int reaction=0)
        {
            var sync = GetComponent<NetworkEnemySync>(); if (sync && sync.IsNetworked && !sync.IsAuthority) return;
            if (!Alive || amount <= 0) return;
            var transformingBoss=GetComponent<BossAI>(); if(transformingBoss && transformingBoss.IsTransforming) return;
            var parts = GetComponent<BossParts>();
            bool bypass=false;var reward=GetComponent<BossBreakReward>();
            if(reward && source)amount=reward.Resolve(amount,source.transform.position,false,true,out bypass);
            if (parts && !bypass && AbsorbImpact(parts,amount,source ? (Vector2)source.transform.position : (Vector2)transform.position,reaction)) return;
            var bossAI = GetComponent<BossAI>();
            if (bossAI && bossAI.IsTransforming) return;
            if (bossAI && bossAI.Phase < 3) amount = Mathf.Min(amount, Mathf.Max(0, Current - maximum * .5f));
            meleeFinisher=null;
            ApplyDamage(amount, Vector2.zero,reaction,source);
        }
        // Raw health mutation for authority state and deterministic verification only.
        public void ApplyDamage(float amount, Vector2 direction,int reaction=0,PlayerHealth source=null)
        {
            if (!Alive || amount <= 0) return;
            reactionFinisher=reaction>0?source:null;
            Current = Mathf.Max(0, Current - amount);
            if (!IsBoss) Knockback = direction * 3.5f;
            GetComponent<HitFeedback>()?.Flash();
            if(reaction>0)ReportImpact(amount,reaction);else EffectsService.Instance?.Popup(transform.position, Mathf.CeilToInt(amount).ToString(), Color.white);
            if (!Alive) Die(true);
        }
        bool AbsorbImpact(BossParts parts,float amount,Vector2 origin,int reaction)
        {
            Vector4 before=parts.Health;bool absorbed=parts.Absorb(amount,origin);Vector4 delta=before-parts.Health;
            if(absorbed)ReportImpact(delta.x+delta.y+delta.z+delta.w,reaction);
            return absorbed;
        }
        void ReportImpact(float amount,int reaction)
        {
            if(reaction<=0 || amount<=0)return;
            var sync=GetComponent<NetworkEnemySync>();
            if(sync && sync.IsNetworked)sync.BroadcastReactionDamage(amount,reaction);
            else ReactionFeedback.Damage(transform.position,amount,reaction);
        }
        void Die(bool drops)
        {
            if (died) return;
            died = true;
            if(drops)GetComponent<ReactionDamage>()?.ReleaseSeedOnDeath();
            if(IsBoss)EffectsService.Instance?.Burst(transform.position,new Color(1,.35f,.6f));
            else EffectsService.Instance?.Defeat(transform.position);
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
                    } else if(reactionFinisher && reactionFinisher.IsLocal && reactionFinisher.Alive && Vector2.Distance(reactionFinisher.transform.position,transform.position)<=9)orb.Attract(reactionFinisher);
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
