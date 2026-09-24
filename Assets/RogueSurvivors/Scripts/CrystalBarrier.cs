using UnityEngine;
namespace RogueSurvivors
{
    public sealed class CrystalBarrier : MonoBehaviour
    {
        PlayerHealth owner; LineRenderer outline; float pulse;
        void Awake()
        {
            owner=GetComponent<PlayerHealth>(); var go=new GameObject("結晶バリア・一撃防御"); go.transform.SetParent(transform,false);
            outline=go.AddComponent<LineRenderer>(); outline.sharedMaterial=new Material(Shader.Find("Sprites/Default")); outline.useWorldSpace=false;
            outline.loop=true; outline.positionCount=6; outline.startWidth=outline.endWidth=.11f; outline.sortingLayerName="Projectiles";
            for(int i=0;i<6;i++) { float a=i*Mathf.PI/3; outline.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*.8f); }
        }
        void Update()
        {
            outline.enabled=owner && owner.Alive && owner.HasBarrier; if(!outline.enabled) return;
            pulse+=Time.deltaTime; outline.transform.localRotation=Quaternion.Euler(0,0,pulse*22);
            outline.startColor=outline.endColor=Color.Lerp(new Color(1,.72f,.15f),Color.white,.35f+.2f*Mathf.Sin(pulse*4));
        }
        void OnDestroy() { if(outline && outline.sharedMaterial) Destroy(outline.sharedMaterial); }
    }
}
