using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class MapGround : MonoBehaviour
    {
        static readonly Material[] materials=new Material[2];Mesh mesh;
        static Material Material(SoloMapKind kind)
        {
            int i=(int)kind;if(!materials[i]){materials[i]=new Material(Shader.Find("Sprites/Default"));materials[i].mainTexture=MapArt.Ground(kind);}return materials[i];
        }
        public static void Create(Transform parent,SoloMapKind kind,bool road=false,bool vertical=false)
        {
            var go=new GameObject(road?"古い街道":"地面");go.transform.SetParent(parent,false);
            var ground=go.AddComponent<MapGround>();var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            if(!road) {
                vertices.AddRange(new[]{new Vector3(-12,-12),new Vector3(12,-12),new Vector3(12,12),new Vector3(-12,12)});
                uv.AddRange(new[]{Vector2.zero,new Vector2(3,0),new Vector2(3,3),new Vector2(0,3)});
                for(int i=0;i<4;i++)colors.Add(new Color(.84f,.88f,.84f));triangles.AddRange(new[]{0,1,2,0,2,3});
            } else {
                float[] bands={-3.1f,-2.2f,2.2f,3.1f};
                for(int x=0;x<=6;x++)for(int y=0;y<4;y++) {
                    float along=-12+x*4,across=bands[y];
                    vertices.Add(vertical?new Vector3(across,along):new Vector3(along,across));uv.Add(new Vector2(x*.5f,y/3f));
                    bool edge=y==0||y==3;colors.Add(kind==SoloMapKind.Meadow?new Color(.85f,.68f,.46f,edge?0:.65f):new Color(.65f,.69f,.68f,edge?0:.5f));
                    if(x<6 && y<3){int a=x*4+y;triangles.AddRange(new[]{a,a+4,a+5,a,a+5,a+1});}
                }
            }
            ground.mesh=new Mesh {name="地面区画",vertices=vertices.ToArray(),uv=uv.ToArray(),colors=colors.ToArray(),triangles=triangles.ToArray()};ground.mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=ground.mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Material(road?SoloMapKind.Ruins:kind);renderer.sortingLayerName="Background";renderer.sortingOrder=road?-90:-100;
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
