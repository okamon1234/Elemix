using UnityEngine;
namespace RogueSurvivors
{
    public sealed class MapObstacles : MonoBehaviour
    {
        public GameObject obstaclePrefab;
        public bool arena;
        void Start()
        {
            if(!arena){gameObject.AddComponent<SoloMap>().Initialize(SoloMapCatalog.Selected);return;}
            if(!GetComponent<BossArena>())gameObject.AddComponent<BossArena>();
            if(obstaclePrefab)foreach(var p in new[]{new Vector3(-14,-7),new Vector3(14,-7),new Vector3(-14,7),new Vector3(14,7)})Instantiate(obstaclePrefab,p,Quaternion.identity,transform);
        }
    }
}
