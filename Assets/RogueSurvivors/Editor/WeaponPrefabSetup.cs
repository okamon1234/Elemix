using System.IO;
using UnityEditor;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    public static class WeaponPrefabSetup
    {
        const string Root = "Assets/RogueSurvivors/Resources/RogueSurvivors/";
        public static void Apply()
        {
            string fireballPath = Root + "Fireball.prefab";
            if (!File.Exists(fireballPath)) {
                var fireball = new GameObject("Fireball"); var sprite = fireball.AddComponent<SpriteRenderer>();
                sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/fireball.png"); sprite.sortingLayerName = "Projectiles";
                sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "PlayerBullet.prefab").GetComponentInChildren<SpriteRenderer>().sharedMaterial;
                fireball.transform.localScale = Vector3.one * .65f; fireball.AddComponent<AreaProjectile>();
                PrefabUtility.SaveAsPrefabAsset(fireball, fireballPath); Object.DestroyImmediate(fireball);
            }
        }
    }
}
