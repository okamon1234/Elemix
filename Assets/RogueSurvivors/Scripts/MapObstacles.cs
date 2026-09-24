using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class MapObstacles : MonoBehaviour
    {
        public GameObject obstaclePrefab;
        public bool arena;
        readonly Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
        Vector2Int center = new Vector2Int(int.MaxValue, int.MaxValue);
        void Start()
        {
            if (arena && !GetComponent<BossArena>()) gameObject.AddComponent<BossArena>();
            if (arena && obstaclePrefab)
                foreach (var p in new[] { new Vector3(-14, -7, 0), new Vector3(14, -7, 0), new Vector3(-14, 7, 0), new Vector3(14, 7, 0) })
                    Instantiate(obstaclePrefab, p, Quaternion.identity, transform);
        }
        void Update()
        {
            if (arena || !PlayerHealth.Local || !obstaclePrefab) return;
            Vector3 position = PlayerHealth.Local.transform.position;
            var next = new Vector2Int(Mathf.FloorToInt(position.x / 24), Mathf.FloorToInt(position.y / 24));
            if (next == center) return;
            center = next;
            foreach (var key in new List<Vector2Int>(chunks.Keys))
                if (Mathf.Abs(key.x - center.x) > 1 || Mathf.Abs(key.y - center.y) > 1)
                { Destroy(chunks[key]); chunks.Remove(key); }
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
            {
                var key = center + new Vector2Int(x, y);
                if (chunks.ContainsKey(key)) continue;
                var root = new GameObject("遺跡区画 " + key); root.transform.SetParent(transform);
                chunks[key] = root;
                var random = new System.Random(unchecked(key.x * 73856093 ^ key.y * 19349663 ^ 913));
                for (int i = 0; i < 5; i++)
                {
                    var spot = new Vector3(key.x * 24 + 3 + (float)random.NextDouble() * 18, key.y * 24 + 3 + (float)random.NextDouble() * 18, 0);
                    if (spot.sqrMagnitude < 36 || Vector3.Distance(spot, position) < 4) continue;
                    Instantiate(obstaclePrefab, spot, Quaternion.identity, root.transform);
                }
            }
        }
    }
}
