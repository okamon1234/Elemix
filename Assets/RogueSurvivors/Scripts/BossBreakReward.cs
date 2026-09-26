using UnityEngine;
namespace RogueSurvivors
{
    // 破壊の報酬は権威側だけで計算し、残り時間と予算を同期する。
    public sealed class BossBreakReward : MonoBehaviour
    {
        public const float StaggerSeconds=1.1f, WindowSeconds=4, Reach=3.6f, BudgetFraction=.035f;
        public int Part { get; private set; }=-1;
        public float Remaining { get; private set; }
        public float Stagger { get; private set; }
        public float Budget { get; private set; }
        BossAI boss;BossParts parts;EnemyHealth health;
        void Awake(){boss=GetComponent<BossAI>();parts=GetComponent<BossParts>();health=GetComponent<EnemyHealth>();}
        bool PhasePending=>boss.Phase<(health.Current<=health.maximum*.5f?3:parts.BrokenCount>0?2:1);
        public bool Open=>Part>=0 && Remaining>0 && Budget>0 && Stagger<=0 && health.Alive && !boss.IsTransforming && !PhasePending;
        public void Begin(int part)
        {
            var sync=GetComponent<NetworkEnemySync>();if(sync && !sync.IsAuthority)return;
            if(part<0 || part>3 || Part==part)return;
            Part=part;Remaining=WindowSeconds;Stagger=StaggerSeconds;Budget=health.maximum*BudgetFraction;
            boss.BeginBreakStagger();
        }
        public void Tick(float seconds)
        {
            if(!health.Alive){Clear();return;}
            if(Stagger>0){Stagger=Mathf.Max(0,Stagger-seconds);return;}
            if(!boss.IsTransforming && !PhasePending)Remaining=Mathf.Max(0,Remaining-seconds);
        }
        void FixedUpdate()
        {
            var sync=GetComponent<NetworkEnemySync>();
            if((!sync || sync.IsAuthority) && GameManager.Instance && GameManager.Instance.IsPlaying)Tick(Time.fixedDeltaTime);
        }
        public bool InPosition(Vector2 origin)=>Open && parts.PartFrom(origin)==Part && Vector2.Distance(origin,transform.position)<=Reach;
        public float Resolve(float amount,Vector2 origin,bool physical,bool secondary,out bool bypassArmor)
        {
            bypassArmor=false;if(!InPosition(origin))return amount;
            float multiplier=secondary?1.15f:physical?1.6f:1.3f;
            float allowed=Mathf.Max(0,health.Current-(boss.Phase<3?health.maximum*.5f:0));
            if(!parts.Exposed){
                bypassArmor=true;float dealt=Mathf.Min(amount*multiplier,Budget,allowed);Budget-=dealt;return dealt;
            }
            float extra=Mathf.Min(amount*(multiplier-1),Budget,Mathf.Max(0,allowed-amount));Budget-=extra;
            return amount+extra;
        }
        public Vector4 Capture()=>new Vector4(Part+1,Remaining,Stagger,Budget);
        public void Restore(Vector4 state)
        {
            int next=Mathf.Clamp(Mathf.RoundToInt(state.x)-1,-1,3);
            if(next!=Part && state.z>0)GetComponent<BossAttackDirector>()?.StopAllCoroutines();
            Part=next;Remaining=Mathf.Clamp(state.y,0,WindowSeconds);Stagger=Mathf.Clamp(state.z,0,StaggerSeconds);Budget=Mathf.Clamp(state.w,0,health.maximum*BudgetFraction);
        }
        public void Clear(){Part=-1;Remaining=Stagger=Budget=0;}
    }
}
