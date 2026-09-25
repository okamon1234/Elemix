using UnityEngine;
namespace RogueSurvivors
{
    // Cosmetic only: the authority resolves the shared seed's single hit.
    public sealed class BloomFlight : MonoBehaviour
    {
        Transform target; Vector2 origin,side; float age; LineRenderer trail;
        static Material material;
        public static void Show(Transform target)
        {
            for(int i=0;i<3;i++) {
                var fx=new GameObject("超開花の追尾弾").AddComponent<BloomFlight>();
                fx.target=target;fx.origin=(Vector2)target.position+Vector2.up*.8f;
                fx.side=Quaternion.Euler(0,0,i*120)*Vector2.right;
                if(!material)material=new Material(Shader.Find("Sprites/Default"));
                fx.trail=fx.gameObject.AddComponent<LineRenderer>();fx.trail.sharedMaterial=material;
                fx.trail.sortingLayerName="Projectiles";fx.trail.sortingOrder=32;fx.trail.positionCount=8;
                fx.trail.startWidth=.015f;fx.trail.endWidth=.14f;
                fx.trail.startColor=new Color(.4f,1,.2f,0);fx.trail.endColor=new Color(.85f,1,.45f,.85f);
            }
        }
        Vector2 Point(float t) => Vector2.Lerp(origin,target.position,t)+side*Mathf.Sin(t*Mathf.PI)*2.3f;
        void Update()
        {
            age+=Time.deltaTime;if(!target || age>=.32f){Destroy(gameObject);return;}
            float t=age/.32f;
            for(int i=0;i<8;i++)trail.SetPosition(i,Point(Mathf.Clamp01(t-(7-i)*.014f)));
        }
    }
}
