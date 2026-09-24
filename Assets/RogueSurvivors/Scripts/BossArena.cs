using UnityEngine;
namespace RogueSurvivors
{
    public sealed class BossArena : MonoBehaviour
    {
        public const float HalfWidth = 28, HalfHeight = 18;
        public static Vector2 Clamp(Vector2 point, float margin = 2) => new Vector2(Mathf.Clamp(point.x,-HalfWidth+margin,HalfWidth-margin),Mathf.Clamp(point.y,-HalfHeight+margin,HalfHeight-margin));
        void Awake()
        {
            SetWall("North", new Vector2(0,HalfHeight), new Vector2(HalfWidth*2+.4f,.4f));
            SetWall("South", new Vector2(0,-HalfHeight), new Vector2(HalfWidth*2+.4f,.4f));
            SetWall("West", new Vector2(-HalfWidth,0), new Vector2(.4f,HalfHeight*2));
            SetWall("East", new Vector2(HalfWidth,0), new Vector2(.4f,HalfHeight*2));
        }
        static void SetWall(string name, Vector2 position, Vector2 size)
        {
            var wall = GameObject.Find(name); if (!wall) return;
            wall.transform.position = position; wall.transform.localScale = new Vector3(size.x,size.y,1);
        }
    }
}
