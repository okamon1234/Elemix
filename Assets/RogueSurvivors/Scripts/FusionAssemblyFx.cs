using UnityEngine;
namespace RogueSurvivors
{
    // 素材2本が中央へ集まり、新しい武器1本へ置き換わる短い合体演出。
    public sealed class FusionAssemblyFx : MonoBehaviour
    {
        SpriteRenderer left,right,result;float age;bool joined;
        public static void Show(PlayerStats player,PhysicalEvolution.Recipe recipe)
        {
            var fx=new GameObject("武器の合体").AddComponent<FusionAssemblyFx>();fx.transform.SetParent(player.transform,false);fx.transform.localPosition=Vector3.up*2;
            fx.left=fx.Make("素材1",SourceArt(recipe.Weapon));fx.right=fx.Make("素材2",SourceArt(recipe.Partner));fx.result=fx.Make("合体した武器",PhysicalFusionArt.Get(recipe.Style));fx.result.enabled=false;
        }
        static Sprite SourceArt(string id)
        {
            if(id=="orbit")return Resources.Load<Sprite>("RogueSurvivors/Art/blade");
            if(id=="spear"||id=="crossbow")return Resources.Load<Sprite>("RogueSurvivors/Art/spear");
            int kind=id=="dagger"?0:id=="axe"?1:id=="hammer"?2:id=="sword"?3:id=="shield"?4:id=="gauntlet"?5:6;
            return WeaponArt.Get(kind);
        }
        SpriteRenderer Make(string name,Sprite art)
        {
            var r=new GameObject(name).AddComponent<SpriteRenderer>();r.transform.SetParent(transform,false);r.sprite=art;r.sortingLayerName="Projectiles";r.sortingOrder=30;return r;
        }
        void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/.35f);
            left.transform.localPosition=Vector3.left*(1-t)*1.2f;right.transform.localPosition=Vector3.right*(1-t)*1.2f;
            if(age>=.35f&&!joined){joined=true;left.enabled=right.enabled=false;result.enabled=true;
                for(int i=0;i<10;i++){Vector2 d=Quaternion.Euler(0,0,i*36)*Vector2.right;CombatMote.Create(transform.position,d*3,new Color(.9f,.84f,.67f),new Vector2(.08f,.4f),.35f);}}
            if(joined){float finish=(age-.35f)/.65f;result.transform.localScale=Vector3.one*(1.4f-finish*.4f);result.color=new Color(1,1,1,Mathf.Min(1,(1-finish)*3));}
            if(age>=1)Destroy(gameObject);
        }
    }
}
