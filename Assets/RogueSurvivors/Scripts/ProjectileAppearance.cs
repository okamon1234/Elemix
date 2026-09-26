using UnityEngine;
namespace RogueSurvivors
{
    // 軌跡は弾の子に付け、弾が消えたら残像も消す。
    public sealed class ProjectileAppearance : MonoBehaviour
    {
        static Material material;
        TrailRenderer trail;SpriteRenderer body;
        public void Configure(bool hostile,bool arrow=false)
        {
            body=GetComponentInChildren<SpriteRenderer>();if(!body)return;
            var art=BattleArt.Effect(hostile?(arrow?10:11):4);if(!art)return;
            body.sprite=art;body.color=Color.white;body.transform.localScale=Vector3.one*(hostile?(arrow?1.0f:.65f):.6f);
            body.sortingLayerName="Projectiles";body.sortingOrder=hostile?160:12;
            if(!trail) {
                var go=new GameObject("飛翔の軌跡");go.transform.SetParent(transform,false);trail=go.AddComponent<TrailRenderer>();
                if(!material)material=new Material(Shader.Find("Sprites/Default"));trail.sharedMaterial=material;
                trail.time=.13f;trail.minVertexDistance=.12f;trail.startWidth=.12f;trail.endWidth=0;trail.sortingLayerName="Projectiles";
            }
            trail.sortingOrder=hostile?150:11;trail.startColor=hostile?new Color(1,.3f,.15f,.8f):new Color(.6f,1,.85f,.35f);trail.endColor=new Color(1,1,1,0);
        }
    }
}
