using UnityEngine;
namespace RogueSurvivors
{
    public enum BossState { Pursue, Windup, Dash, Recover, Transforming }
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public sealed class BossAI : MonoBehaviour
    {
        public Bullet hostileBullet;
        public Transform warning;
        public BossState State { get; private set; }
        public float Remaining { get; private set; } = 2.5f;
        public Vector2 Heading { get; private set; }
        public Vector2 AimPoint { get; private set; }
        public int AttackIndex { get; private set; }
        public BossKind Kind { get; private set; }
        public bool PhaseTwo { get; private set; }
        public BossAttack PendingAttack { get; private set; }
        public int ChargesLeft { get; private set; }
        public bool IsTransforming => State == BossState.Transforming;
        public string DisplayName => BossCatalog.Names[(int)Kind];
        public string ActionLabel => IsTransforming ? "形態変化中・本体無敵" : State==BossState.Windup ? "予告："+BossCatalog.AttackName(PendingAttack) : PhaseTwo ? BossCatalog.PhaseNames[(int)Kind] : "第1形態";
        Rigidbody2D body;
        EnemyHealth health;
        NetworkEnemySync sync;
        BossDifficulty difficulty;
        BossParts parts;
        BossAttackDirector attacks;
        bool configured;
        float AttackRate => (difficulty ? difficulty.AttackRate : 1) * (PhaseTwo?1.2f:1) * (1+parts.BrokenCount*.06f);
        float DamageScale => (difficulty ? difficulty.DamageScale : 1) * (PhaseTwo?1.15f:1);
        public int BrokenMask { get { int mask=0; if(parts.Maximum>0) for(int i=0;i<4;i++) if(parts.Health[i]<=0) mask|=1<<i; return mask; } }
        void Awake()
        {
            parts=GetComponent<BossParts>(); if(!parts) parts=gameObject.AddComponent<BossParts>();
            if(!GetComponent<ElementReaction>()) gameObject.AddComponent<ElementReaction>();
            body=GetComponent<Rigidbody2D>(); health=GetComponent<EnemyHealth>(); sync=GetComponent<NetworkEnemySync>(); difficulty=GetComponent<BossDifficulty>();
            attacks=GetComponent<BossAttackDirector>(); if(!attacks) attacks=gameObject.AddComponent<BossAttackDirector>();
            if(!GetComponent<BossAppearance>()) gameObject.AddComponent<BossAppearance>();
        }
        public void ConfigureDifficulty(PlayerStats[] players)
        {
            if(configured) return; configured=true; Kind=BossCatalog.Choose();
            if(difficulty) { difficulty.Configure(players); health.Scale(difficulty.Health/health.maximum); }
            parts.Configure(health.maximum);
        }
        public void RestoreState(int state,float remaining,Vector2 heading,int attack)
        { State=(BossState)state; Remaining=remaining; Heading=heading; AttackIndex=attack; }
        public void RestoreEncounter(int kind,bool phase,int attack,Vector2 aim,int charges)
        { Kind=(BossKind)Mathf.Clamp(kind,0,3); PhaseTwo=phase; PendingAttack=(BossAttack)Mathf.Clamp(attack,0,15); AimPoint=aim; ChargesLeft=charges; }
        void Update()
        {
            if(warning) {
                bool dash=PendingAttack==BossAttack.Charge || PendingAttack==BossAttack.TripleCharge;
                warning.gameObject.SetActive(State==BossState.Windup && dash && health.Alive);
                warning.rotation=Quaternion.Euler(0,0,Mathf.Atan2(Heading.y,Heading.x)*Mathf.Rad2Deg);
                float warningLength=(PhaseTwo?18:14)*.65f*((BrokenMask&8)!=0?.8f:1)+1.4f;
                warning.localScale=new Vector3(warningLength,2.8f,1);
                warning.position=transform.position+(Vector3)Heading*(warningLength*.5f);
            }
        }
        void FixedUpdate()
        {
            if(sync && !sync.IsAuthority) return;
            if(!health.Alive || !GameManager.Instance || !GameManager.Instance.IsPlaying) { body.linearVelocity=Vector2.zero; return; }
            if(!PhaseTwo && health.Current<=health.maximum*.5f) {
                PhaseTwo=true; State=BossState.Transforming; Remaining=2.4f; ChargesLeft=0; body.linearVelocity=Vector2.zero;
                attacks.StopAllCoroutines(); return;
            }
            PlayerHealth target=null; float best=float.MaxValue;
            foreach(var candidate in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None)) {
                if(!candidate.Alive) continue;
                float distance=(candidate.transform.position-transform.position).sqrMagnitude;
                if(distance<best) { best=distance; target=candidate; }
            }
            if(!target) { body.linearVelocity=Vector2.zero; return; }
            Remaining-=Time.fixedDeltaTime*(State==BossState.Pursue || State==BossState.Recover?AttackRate:1);
            switch(State) {
                case BossState.Transforming:
                    body.linearVelocity=Vector2.zero;
                    if(Remaining<=0) { AttackIndex=0; BeginAttack(target); }
                    break;
                case BossState.Pursue:
                    Vector2 desired=((Vector2)(target.transform.position-transform.position)).normalized;
                    float speed=(Kind==BossKind.Spider?2.8f:Kind==BossKind.Golem?1.8f:2.2f)*(PhaseTwo?1.25f:1)*((BrokenMask&8)!=0?.8f:1);
                    body.linearVelocity=ObstacleAvoidance.Steer(body.position,desired,1.12f,1)*speed;
                    if(Remaining<=0) BeginAttack(target);
                    break;
                case BossState.Windup:
                    body.linearVelocity=Vector2.zero;
                    if(Remaining<=0) {
                        if(PendingAttack==BossAttack.Charge || PendingAttack==BossAttack.TripleCharge) { State=BossState.Dash; Remaining=.65f; }
                        else {
                            if(sync && sync.IsNetworked) sync.BroadcastAttack((int)PendingAttack,body.position,AimPoint,PhaseTwo,20*DamageScale,BrokenMask);
                            else attacks.Execute(PendingAttack,body.position,AimPoint,PhaseTwo,20*DamageScale,BrokenMask);
                            State=BossState.Recover; Remaining=PendingAttack==BossAttack.Spiral?2.6f:1.4f;
                        }
                    } break;
                case BossState.Dash:
                    body.linearVelocity=Heading*(PhaseTwo?18:14)*((BrokenMask&8)!=0?.8f:1);
                    if(Remaining<=0) {
                        ChargesLeft--;
                        if(ChargesLeft>0) { Aim(target); State=BossState.Windup; Remaining=.8f; }
                        else { State=BossState.Recover; Remaining=1.5f; }
                    } break;
                case BossState.Recover:
                    body.linearVelocity=Vector2.zero;
                    if(Remaining<=0) { State=BossState.Pursue; Remaining=PhaseTwo?1.5f:2.2f; }
                    break;
            }
            if(GetComponent<EnemyAilment>()) body.linearVelocity*=GetComponent<EnemyAilment>().SpeedMultiplier;
            body.position=BossArena.Clamp(body.position);
        }
        void Aim(PlayerHealth target) { AimPoint=BossArena.Clamp(target.transform.position); Heading=(AimPoint-body.position).normalized; if(Heading.sqrMagnitude<.1f) Heading=Vector2.down; }
        void BeginAttack(PlayerHealth target)
        {
            PendingAttack=BossCatalog.Attack(Kind,PhaseTwo,AttackIndex++); Aim(target);
            ChargesLeft=PendingAttack==BossAttack.TripleCharge?3:1;
            State=BossState.Windup; Remaining=Mathf.Max(.85f,1.25f/Mathf.Sqrt(AttackRate)); body.linearVelocity=Vector2.zero;
        }
        public void SpawnVolley(float angle,int count,float speed,float damage)
        {
            for(int i=0;i<count;i++) {
                float theta=(angle+i*360f/count)*Mathf.Deg2Rad; Vector2 direction=new Vector2(Mathf.Cos(theta),Mathf.Sin(theta));
                Instantiate(hostileBullet,transform.position+(Vector3)direction*1.6f,Quaternion.identity).Launch(direction,damage,speed,0,true);
            }
        }
        void OnCollisionStay2D(Collision2D collision) { if(!IsTransforming) collision.gameObject.GetComponent<PlayerHealth>()?.Damage((State==BossState.Dash?30:18)*DamageScale*((BrokenMask&1)!=0?.8f:1)); }
        void OnCollisionEnter2D(Collision2D collision) => OnCollisionStay2D(collision);
    }
}
