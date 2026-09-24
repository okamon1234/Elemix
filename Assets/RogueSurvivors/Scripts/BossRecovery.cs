using UnityEngine;
namespace RogueSurvivors
{
    public sealed class BossRecovery : MonoBehaviour
    {
        public int Charges { get; private set; } = 3;
        public float ReadyAt { get; private set; }
        PlayerHealth health;
        public string Status => "応急手当 " + Charges + "/3" + (Charges==0?"（使い切り）":Time.time<ReadyAt?"　再使用まで"+Mathf.CeilToInt(ReadyAt-Time.time)+"秒":"　HP45%以下・6秒被弾なしで発動");
        void Start() => health=GetComponent<PlayerHealth>();
        void Update()
        {
            if(!health || !health.IsLocal || !health.Alive || !GameManager.Instance || !GameManager.Instance.IsPlaying || GameManager.Instance.Mode!=RunMode.Boss) return;
            if(Charges<=0 || Time.time<ReadyAt || health.Current>health.Maximum*.45f || Time.time-health.LastDamageTime<6) return;
            Charges--; ReadyAt=Time.time+35; health.Heal(health.Maximum*.18f);
            EffectsService.Instance?.Burst(transform.position,new Color(.3f,1,.5f));
            HUDController.Instance?.Toast("応急手当：HP18%回復（残り"+Charges+"回）");
        }
    }
}
