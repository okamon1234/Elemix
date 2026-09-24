using UnityEngine;
namespace RogueSurvivors
{
    // 塗りのある結晶・葉・岩・光片。共有マテリアルと頂点色で発光の芯と陰影を描く。
    public sealed class CombatMote : MonoBehaviour
    {
        static Material material;
        Mesh mesh; MeshRenderer meshRenderer; MaterialPropertyBlock properties;
        Vector2 velocity; float age, life, spin; Vector3 scale;
        public static void Create(Vector2 point, Vector2 velocity, Color tint, Vector2 size, float life=.55f, int shape=0, float spin=0)
        {
            var fx=new GameObject("属性の破片").AddComponent<CombatMote>();
            fx.velocity=velocity; fx.life=life; fx.spin=spin; fx.transform.position=point;
            fx.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg-90);
            fx.scale=new Vector3(size.x,size.y,1); fx.transform.localScale=fx.scale;
            Vector3[] edge=shape==1?new[]{new Vector3(0,1),new Vector3(.7f,.25f),new Vector3(.3f,-.6f),new Vector3(0,-1),new Vector3(-.7f,-.1f),new Vector3(-.5f,.5f)}:
                shape==2?new[]{new Vector3(-.65f,.6f),new Vector3(.1f,1),new Vector3(.8f,.3f),new Vector3(.7f,-.8f),new Vector3(-.5f,-1),new Vector3(-.9f,-.2f)}:
                shape==3?new[]{Vector3.up,new Vector3(.18f,.18f),Vector3.right,new Vector3(.18f,-.18f),Vector3.down,new Vector3(-.18f,-.18f),Vector3.left,new Vector3(-.18f,.18f)}:
                new[]{Vector3.up,new Vector3(.48f,0),Vector3.down,new Vector3(-.48f,0)};
            var vertices=new Vector3[edge.Length+1]; var colors=new Color[vertices.Length]; var triangles=new int[edge.Length*3];
            colors[0]=Color.Lerp(tint,Color.white,.8f);
            for(int i=0;i<edge.Length;i++) { vertices[i+1]=edge[i]; colors[i+1]=Color.Lerp(tint,new Color(tint.r*.25f,tint.g*.25f,tint.b*.4f,tint.a),i%2==0?.05f:.4f); triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=(i+1)%edge.Length+1; }
            fx.mesh=new Mesh {name="戦闘用の面",vertices=vertices,colors=colors,triangles=triangles}; fx.mesh.RecalculateBounds();
            fx.gameObject.AddComponent<MeshFilter>().sharedMesh=fx.mesh;
            if(!material) material=new Material(Shader.Find("Sprites/Default"));
            fx.meshRenderer=fx.gameObject.AddComponent<MeshRenderer>(); fx.meshRenderer.sharedMaterial=material;
            fx.meshRenderer.sortingLayerName="Projectiles"; fx.meshRenderer.sortingOrder=16; fx.properties=new MaterialPropertyBlock();
        }
        void Update()
        {
            age+=Time.deltaTime; if(age>=life) {Destroy(gameObject);return;}
            float t=age/life; transform.position+=(Vector3)velocity*Time.deltaTime; transform.Rotate(0,0,spin*Time.deltaTime);
            transform.localScale=scale*(Mathf.Min(1,t*12+.25f)*(1-t*.55f));
            properties.SetColor("_Color",new Color(1,1,1,Mathf.Min(1,(1-t)*3))); meshRenderer.SetPropertyBlock(properties);
        }
        void OnDestroy() {if(mesh) Destroy(mesh);}
    }
}
