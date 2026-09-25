using UnityEngine;
namespace RogueSurvivors
{
    public static class CombatVisuals
    {
        public static void Lightning(Vector2 start, Vector2 end)
        {
            WeaponCastVisual.Show("lightning",start,end-start);
            var go = new GameObject("雷の軌跡"); var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = Resources.Load<GameObject>("RogueSurvivors/PlayerBullet").GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            line.positionCount = 7; line.startWidth = .13f; line.endWidth = .05f; line.sortingLayerName = "Projectiles";
            line.startColor = new Color(.8f, .9f, 1); line.endColor = new Color(.5f, .6f, 1);
            Vector2 side = new Vector2(-(end - start).y, (end - start).x).normalized;
            for (int i = 0; i < 7; i++) line.SetPosition(i, Vector2.Lerp(start, end, i / 6f) + side * (i == 0 || i == 6 ? 0 : Random.Range(-.2f, .2f)));
            Object.Destroy(go, .16f);
        }
        public static void Spear(Vector2 start, Vector2 end)
        {
            WeaponSwingVisual.Create(7, start, end);
        }
    }
}
