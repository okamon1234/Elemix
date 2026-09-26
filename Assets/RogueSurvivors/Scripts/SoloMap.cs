using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class SoloMap : MonoBehaviour
    {
        public static SoloMap Instance {get;private set;}
        public SoloMapKind Kind {get;private set;}
        public int ChunkCount=>chunks.Count;
        public readonly List<Rect> ObstacleBounds=new List<Rect>();
        readonly Dictionary<Vector2Int,GameObject> chunks=new Dictionary<Vector2Int,GameObject>();
        readonly Dictionary<Vector2Int,List<Rect>> boxes=new Dictionary<Vector2Int,List<Rect>>();
        Vector2Int center=new Vector2Int(int.MaxValue,int.MaxValue);bool ready;
        public void Initialize(SoloMapKind kind)
        {
            Instance=this;Kind=kind;ready=true;
            foreach(var root in chunks.Values){root.SetActive(false);Destroy(root);}chunks.Clear();boxes.Clear();center=new Vector2Int(int.MaxValue,int.MaxValue);
            var old=GameObject.Find("Grid Floor");if(old)old.SetActive(false);
            RefreshAround(PlayerHealth.Local?(Vector2)PlayerHealth.Local.transform.position:Vector2.zero);
        }
        void Update(){if(ready && PlayerHealth.Local)RefreshAround(PlayerHealth.Local.transform.position);}
        public void RefreshAround(Vector2 position)
        {
            var next=SoloMapLayout.Key(position);if(next==center)return;center=next;
            foreach(var key in new List<Vector2Int>(chunks.Keys))if(Mathf.Abs(key.x-center.x)>1 || Mathf.Abs(key.y-center.y)>1){chunks[key].SetActive(false);Destroy(chunks[key]);chunks.Remove(key);boxes.Remove(key);}
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++) {
                var key=center+new Vector2Int(x,y);if(chunks.ContainsKey(key))continue;
                var root=new GameObject(SoloMapCatalog.Name(Kind)+"区画 "+key);root.transform.SetParent(transform);root.transform.position=(Vector2)key*24;chunks[key]=root;boxes[key]=new List<Rect>();
                MapGround.Create(root.transform,Kind);MapGround.Create(root.transform,Kind,true);MapGround.Create(root.transform,Kind,true,true);
                foreach(var placement in SoloMapLayout.Generate(Kind,key)) {
                    var prop=new GameObject("地形 "+placement.Art);prop.transform.SetParent(root.transform);prop.transform.position=placement.Position;
                    var visual=new GameObject("外観");visual.transform.SetParent(prop.transform,false);visual.transform.localScale=Vector3.one*placement.Size;
                    var sprite=visual.AddComponent<SpriteRenderer>();sprite.sprite=MapArt.Prop(placement.Art);sprite.sortingLayerName="Background";sprite.sortingOrder=placement.Solid?5:0;
                    prop.layer=LayerMask.NameToLayer("World");
                    foreach(var rect in SoloMapLayout.Bounds(placement)){var collider=prop.AddComponent<BoxCollider2D>();collider.offset=rect.center-placement.Position;collider.size=rect.size;boxes[key].Add(rect);}
                }
            }
            ObstacleBounds.Clear();foreach(var list in boxes.Values)ObstacleBounds.AddRange(list);Physics2D.SyncTransforms();
        }
        public Vector2 FindOpenSpot(Vector2 wanted,float radius=.6f)
        {
            if(!Physics2D.OverlapCircle(wanted,radius,LayerMask.GetMask("World")))return wanted;
            for(int ring=1;ring<=8;ring++)for(int i=0;i<16;i++) {
                float angle=i*Mathf.PI/8;Vector2 p=wanted+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*ring;
                if(!Physics2D.OverlapCircle(p,radius,LayerMask.GetMask("World")))return p;
            }
            return (Vector2)SoloMapLayout.Key(wanted)*24;
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
