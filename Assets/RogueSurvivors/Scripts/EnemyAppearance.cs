using UnityEngine;
namespace RogueSurvivors
{
    // 見た目だけを更新し、当たり判定と移動速度は変更しない。
    public sealed class EnemyAppearance : MonoBehaviour
    {
        SpriteRenderer body; Rigidbody2D physicsBody; EnemyCombat combat; EnemyHealth health;
        Vector3 basePosition; float clock;
        public EnemyRole Role => combat ? combat.role : EnemyRole.Skeleton;
        void Start()
        {
            body=GetComponentInChildren<SpriteRenderer>(); physicsBody=GetComponent<Rigidbody2D>();
            combat=GetComponent<EnemyCombat>(); health=GetComponent<EnemyHealth>();
            if(!body)return;
            basePosition=body.transform.localPosition;
            var animator=body.GetComponent<Animator>(); if(animator)animator.enabled=false;
            ApplyFrame(0);
        }
        void ApplyFrame(int frame)
        {
            var art=BattleArt.Enemy(Role,frame);
            if(!art || !body)return;
            body.sprite=art; body.transform.localScale=Vector3.one;
        }
        void LateUpdate()
        {
            if(!body || !health || !health.Alive)return;
            var ailment=GetComponent<EnemyAilment>(); bool frozen=ailment && ailment.Frozen;
            Vector2 velocity=physicsBody ? physicsBody.linearVelocity : Vector2.zero;
            bool moving=velocity.sqrMagnitude>.04f && !frozen;
            clock+=moving?Time.deltaTime*(Role==EnemyRole.Rat?11:Role==EnemyRole.Armored?5:7):0;
            ApplyFrame(moving?1+(Mathf.FloorToInt(clock)%2):0);
            if(Mathf.Abs(velocity.x)>.1f)body.flipX=velocity.x<0;
            bool charging=combat && combat.Dashing;
            body.transform.localPosition=basePosition+Vector3.up*(moving?Mathf.Abs(Mathf.Sin(clock*Mathf.PI))*.028f:0);
            body.transform.localRotation=Quaternion.Euler(0,0,charging?(body.flipX?8:-8):0);
        }
    }
}
