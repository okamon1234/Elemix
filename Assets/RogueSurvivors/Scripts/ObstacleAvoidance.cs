using UnityEngine;
namespace RogueSurvivors
{
    public static class ObstacleAvoidance
    {
        static readonly int[] Angles = { 45, 90, 135, 180 };
        public static Vector2 Steer(Vector2 position, Vector2 desired, float radius, int side)
        {
            int mask = LayerMask.GetMask("World");
            if (!Physics2D.CircleCast(position, radius, desired, 1.2f, mask)) return desired;
            foreach (int angle in Angles)
            {
                Vector2 direction = Quaternion.Euler(0, 0, angle * side) * desired;
                if (!Physics2D.CircleCast(position, radius, direction, 1.2f, mask)) return direction;
            }
            return -desired;
        }
    }
}
