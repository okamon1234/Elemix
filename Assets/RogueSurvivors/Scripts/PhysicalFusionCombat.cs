using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // 合体1本につき一つの発動時計。素材武器の攻撃とは別に一度だけ発動する。
    public sealed class PhysicalFusionCombat : MonoBehaviour
    {
        PlayerStats stats; PlayerHealth health;
        readonly Dictionary<string,float> next=new Dictionary<string,float>();
        void Awake() {stats=GetComponent<PlayerStats>();health=GetComponent<PlayerHealth>();}
        public static float Interval(int style,int level) => style==2?Mathf.Max(.65f,1.05f-level*.04f):style==5?2.2f:Mathf.Max(.9f,1.7f-level*.065f);
        public static float Damage(int style,int level)
        {
            float value=style==2?17+level*3:style==9?24+level*4:style==0?65+level*10:style==3?85+level*12:style==5?45+level*8:42+level*7;
            return value*(style==0||style==2?1:1.15f)*(style==1||style==5?2.2f:style==8?1.3f:1);
        }
        void Update()
        {
            if(!health || !health.IsLocal || !health.Alive || Time.timeScale==0 || !GameManager.Instance || !GameManager.Instance.IsPlaying)return;
            PhysicalEvolution.Refresh(stats);
            foreach(var id in stats.FusionIds) {
                var recipe=PhysicalEvolution.ById(id);var primary=PhysicalEvolution.Weapon(stats,recipe.Weapon);
                if(!primary.enabled || next.TryGetValue(id,out float at) && Time.time<at)continue;
                var enemy=GetComponent<AutoTargeting>().FindNearest();if(!enemy)continue;
                float range=recipe.Style==0?5:recipe.Style==5?5:12;
                if(Vector2.Distance(transform.position,enemy.transform.position)>range)continue;
                int level=PhysicalEvolution.Level(stats,recipe);
                Vector2 origin=transform.position,target=enemy.transform.position;
                PhysicalAttackPattern.Create(recipe.Style,origin,target,level,Damage(recipe.Style,level)*stats.DamageMultiplier,health);
                GetComponent<NetworkPlayerSync>()?.BroadcastEffect(2000+recipe.Style*10+level,origin,target);
                next[id]=Time.time+Interval(recipe.Style,level);
            }
            // 合体が解除された時計を残さず、再合体した武器を直ちに使えるようにする。
            if(next.Count>stats.FusionIds.Count) {
                var stale=new List<string>();foreach(var id in next.Keys)if(!stats.FusionIds.Contains(id))stale.Add(id);
                foreach(var id in stale)next.Remove(id);
            }
        }
    }
}
