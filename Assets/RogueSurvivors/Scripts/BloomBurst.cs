using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    // 敵が先に倒れても種を失わず、次フレームに一度だけ追撃する。再帰爆発を防ぐ。
    public sealed class BloomBurst : MonoBehaviour
    {
        public static void Schedule(Vector2 point,EnemyHealth target,float power,int stage,PlayerHealth owner)
        {
            var burst=new GameObject("開花の追撃").AddComponent<BloomBurst>();
            burst.transform.position=point;burst.StartCoroutine(burst.Detonate(target,power,stage,owner));
        }
        IEnumerator Detonate(EnemyHealth target,float power,int stage,PlayerHealth owner)
        {
            yield return null;
            if(GameManager.Instance && !GameManager.Instance.IsPlaying){Destroy(gameObject);yield break;}
            int reaction=stage==2?14:stage==3?15:8;
            Vector2 point=transform.position;
            if(stage==2 && (!target || !target.Alive)) {
                float nearest=36;
                foreach(var candidate in EnemyHealth.Active)if(candidate && candidate.Alive) {
                    float distance=((Vector2)candidate.transform.position-point).sqrMagnitude;
                    if(distance<nearest){nearest=distance;target=candidate;}
                }
            }
            if(stage==2 && target && target.Alive)point=target.transform.position;
            var sync=target?target.GetComponent<NetworkEnemySync>():null;
            if(sync && sync.IsNetworked)sync.BroadcastReaction(reaction);
            else {ReactionFeedback.Show(point,reaction);CombatFx.Reaction(reaction,CombatElement.Wood,point);}
            if(stage==2) {if(target && target.Alive)target.ReceiveSecondary(power*4.5f,owner,reaction);}
            else foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active))
                if(enemy && enemy.Alive && Vector2.Distance(point,enemy.transform.position)<=(stage==3?4.2f:3.2f))enemy.ReceiveSecondary(power*(stage==3?3.2f:2.1f),owner,reaction);
            Destroy(gameObject);
        }
    }
}
