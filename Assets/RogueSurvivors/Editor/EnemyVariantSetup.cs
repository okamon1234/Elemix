using System.IO;
using UnityEditor;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    public static class EnemyVariantSetup
    {
        const string Root = "Assets/RogueSurvivors/Resources/RogueSurvivors/";
        public static void Apply()
        {
            Create("EnemyRat", EnemyRole.Rat, 18, 2.8f, 9, 2, .65f, "rat");
            Create("EnemyArmored", EnemyRole.Armored, 95, 1.15f, 20, 5, 1.1f, "armored");
            Create("EnemyArcher", EnemyRole.Archer, 42, 1.55f, 12, 4, .9f, "enemy_archer");
            Create("EnemyCharger", EnemyRole.Charger, 65, 1.7f, 24, 5, 1.05f, "charger");
        }
        static void Create(string name, EnemyRole role, float hp, float speed, float damage, int xp, float scale, string art)
        {
            string path = Root + name + ".prefab";
            var go = PrefabUtility.LoadPrefabContents(File.Exists(path) ? path : Root + "Enemy.prefab");
            var health = go.GetComponent<EnemyHealth>(); health.maximum = hp; health.experienceValue = xp;
            var ai = go.GetComponent<EnemyAI>(); ai.speed = speed; ai.contactDamage = damage;
            var combat = go.GetComponent<EnemyCombat>(); if (!combat) combat = go.AddComponent<EnemyCombat>();
            combat.role = role; combat.hostileBullet = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "EnemyBullet.prefab").GetComponent<Bullet>();
            var sprite = go.GetComponentInChildren<SpriteRenderer>(); sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/" + art + ".png");
            sprite.color = Color.white; sprite.transform.localScale = Vector3.one * scale;
            PrefabUtility.SaveAsPrefabAsset(go, path); PrefabUtility.UnloadPrefabContents(go);
        }
    }
}
