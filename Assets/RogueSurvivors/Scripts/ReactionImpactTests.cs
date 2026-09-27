#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
namespace RogueSurvivors
{
    public static class ReactionImpactTests
    {
        static EnemyHealth Target(Vector2 point,float hp=5000)
        {
            var go=new GameObject("反応火力の検証対象");go.transform.position=point;var enemy=go.AddComponent<EnemyHealth>();enemy.SetSynchronizedHealth(hp,hp);return enemy;
        }
        public static IEnumerator Run(Action<bool,string> check,PlayerHealth player)
        {
            Vector2 center=new Vector2(4000,4000);
            var target=Target(center);var nearby=Target(center+Vector2.right*2);var distant=Target(center+Vector2.right*6);
            target.Damage(10,Vector2.zero,player,CombatElement.Fire);float before=target.Current;
            target.Damage(10,Vector2.zero,player,CombatElement.Lightning);
            check(before-target.Current>30 && nearby.Current<nearby.maximum && distant.Current==distant.maximum,"Overload has a strong primary hit and bounded area damage");
            UnityEngine.Object.Destroy(target.gameObject);UnityEngine.Object.Destroy(nearby.gameObject);UnityEngine.Object.Destroy(distant.gameObject);yield return null;
            target=Target(center);target.Damage(10,Vector2.zero,player,CombatElement.Water);before=target.Current;
            target.Damage(1,Vector2.zero,player,CombatElement.Fire);
            check(before-target.Current>25,"Weak multi-hit pulse still triggers a meaningful vaporize");
            yield return new WaitForSeconds(.2f);
            target.Damage(10,Vector2.zero,player,CombatElement.Water);before=target.Current;target.Damage(10,Vector2.zero,player,CombatElement.Fire);
            check(before-target.Current>30,"Normal target can react again after a short pause");UnityEngine.Object.Destroy(target.gameObject);yield return null;
            target=Target(center);target.Damage(10,Vector2.zero,player,CombatElement.Water);target.Damage(10,Vector2.zero,player,CombatElement.Ice);
            yield return new WaitForSeconds(1.7f);
            check(target.GetComponent<EnemyAilment>().Frozen,"Freeze lasts long enough for a follow-up after 1.7 seconds");
            before=target.Current;target.Damage(20,Vector2.zero,player);float first=before-target.Current;before=target.Current;target.Damage(20,Vector2.zero,player);
            check(first>40 && before-target.Current<first,"Physical follow-up shatters ice without repeating the bonus in one frame");
            UnityEngine.Object.Destroy(target.gameObject);yield return null;
            target=Target(center);var burn=target.gameObject.AddComponent<ReactionDamage>();
            before=target.Current;
            for(int i=0;i<6;i++){burn.Begin(20,player,false);yield return new WaitForSeconds(.2f);}
            check(before-target.Current>=25,"Repeated burning applications do not postpone damage ticks indefinitely");
            UnityEngine.Object.Destroy(target.gameObject);yield return null;
            target=Target(center,1);nearby=Target(center+Vector2.right);var bloom=target.gameObject.AddComponent<ReactionDamage>();bloom.Begin(20,player,true);
            target.ApplyDamage(2,Vector2.zero);yield return null;yield return null;
            check(nearby.maximum-nearby.Current>=40,"Seed detonates around a defeated mob instead of vanishing");
            UnityEngine.Object.Destroy(nearby.gameObject);yield return null;
            target=Target(center,1);nearby=Target(center+Vector2.right);bloom=target.gameObject.AddComponent<ReactionDamage>();bloom.Begin(20,player,true);bloom.TryCatalyze(CombatElement.Lightning,20,player);
            target.ApplyDamage(2,Vector2.zero);yield return null;yield return null;
            check(nearby.maximum-nearby.Current>=89,"Hyperbloom retargets a nearby living enemy when its original target dies");
            UnityEngine.Object.Destroy(nearby.gameObject);yield return null;
            Vector2 dropPoint=(Vector2)player.transform.position+Vector2.right*6;
            target=Target(dropPoint,1);target.orbPrefab=Resources.Load<ExpOrb>("RogueSurvivors/ExpOrb");
            check(target.orbPrefab,"Reaction experience test loads the real orb prefab");
            target.ReceiveSecondary(2,player,2);ExpOrb reward=null;
            foreach(var orb in ExpOrb.Active)if(orb && Vector2.Distance(orb.transform.position,dropPoint)<.1f){reward=orb;break;}
            yield return new WaitForSeconds(.15f);
            check(reward && Vector2.Distance(reward.transform.position,player.transform.position)<5.9f,"Nearby reaction kills immediately attract experience without increasing its value");
            if(reward)UnityEngine.Object.Destroy(reward.gameObject);

        }
    }
}
#endif
