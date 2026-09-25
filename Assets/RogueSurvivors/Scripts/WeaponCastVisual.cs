using UnityEngine;
namespace RogueSurvivors
{
    // The visible casting implement; all damage and cooldowns remain in the weapon classes.
    public sealed class WeaponCastVisual : MonoBehaviour
    {
        SpriteRenderer body;Vector2 from,heading;float age;
        public static void Show(string id,Vector2 origin,Vector2 direction)
        {
            var art=ArsenalArt.Weapon(id);if(!art)return;
            var fx=new GameObject("武器の発動姿勢").AddComponent<WeaponCastVisual>();
            fx.from=origin;fx.heading=direction.sqrMagnitude>.01f?direction.normalized:Vector2.right;
            fx.body=fx.gameObject.AddComponent<SpriteRenderer>();fx.body.sprite=art;
            fx.body.sortingLayerName="Projectiles";fx.body.sortingOrder=18;
            fx.transform.localScale=Vector3.one*.8f;
            fx.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(fx.heading.y,fx.heading.x)*Mathf.Rad2Deg);
            fx.transform.position=origin+fx.heading*.6f;
        }
        void Update()
        {
            age+=Time.deltaTime;if(age>=.32f){Destroy(gameObject);return;}
            transform.position=from+heading*(.6f+Mathf.Sin(age/.32f*Mathf.PI)*.2f);
            body.color=new Color(1,1,1,.75f*Mathf.Clamp01((.32f-age)/.1f));
        }
    }
}
