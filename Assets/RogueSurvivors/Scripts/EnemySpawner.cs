using UnityEngine;
namespace RogueSurvivors
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        public EnemyHealth prefab;
        public int maximumEnemies = 160;
        float nextSpawn;
        EnemyHealth[] variants;
        public static float HealthGrowth(float elapsed) => 1 + elapsed * .018f + Mathf.Pow(elapsed / 70, 1.5f);
        void Awake()
        {
            variants = new[] { Resources.Load<EnemyHealth>("RogueSurvivors/EnemyRat"), Resources.Load<EnemyHealth>("RogueSurvivors/EnemyArmored"),
                Resources.Load<EnemyHealth>("RogueSurvivors/EnemyArcher"), Resources.Load<EnemyHealth>("RogueSurvivors/EnemyCharger") };
        }
        public int Wave => 1 + Mathf.FloorToInt(GameManager.Instance.Elapsed / 30);
        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.IsPlaying || gm.Mode != RunMode.Solo || !PlayerHealth.Local || Time.time < nextSpawn) return;
            nextSpawn = Time.time + Mathf.Max(.16f, .85f - Wave * .08f);
            for (int i = 0; i < Mathf.Min(5, 1 + Wave / 2) && EnemyHealth.Active.Count < maximumEnemies; i++) Spawn();
        }
        public EnemyHealth Spawn()
        {
            Camera camera = Camera.main;
            float height = camera.orthographicSize + 1.8f, width = height * camera.aspect;
            Vector2 position;
            float edge = Random.value;
            if (edge < .25f) position = new Vector2(-width, Random.Range(-height, height));
            else if (edge < .5f) position = new Vector2(width, Random.Range(-height, height));
            else if (edge < .75f) position = new Vector2(Random.Range(-width, width), height);
            else position = new Vector2(Random.Range(-width, width), -height);
            float elapsed = GameManager.Instance.Elapsed;
            EnemyHealth chosen = prefab;
            float roll = Random.value;
            int kind = roll < .22f ? 0 : roll < .42f && elapsed >= 35 ? 1 : roll < .61f && elapsed >= 55 ? 2 : roll < .79f && elapsed >= 80 ? 3 : -1;
            if (kind >= 0 && variants[kind]) chosen = variants[kind];
            Vector2 world = (Vector2)camera.transform.position + position;
            for (int attempt = 0; attempt < 8 && Physics2D.OverlapCircle(world, .6f, LayerMask.GetMask("World")); attempt++) world += Random.insideUnitCircle * 2;
            if(SoloMap.Instance)world=SoloMap.Instance.FindOpenSpot(world);
            var enemy = Instantiate(chosen, world, Quaternion.identity);
            enemy.Scale(HealthGrowth(elapsed));
            if (elapsed >= 60 && Random.value < .18f) {
                enemy.gameObject.AddComponent<EnemyAffinity>().Initialize((CombatElement)Random.Range(1, 10));
                enemy.Scale(1.12f); enemy.experienceValue *= 2;
            }
            var ai = enemy.GetComponent<EnemyAI>();
            ai.speed = Mathf.Min(4.1f, ai.speed * (1 + elapsed * .0018f));
            ai.contactDamage *= 1 + elapsed / 150;
            var combat = enemy.GetComponent<EnemyCombat>(); if (combat) combat.attackMultiplier = 1 + elapsed / 150;
            return enemy;
        }
    }
}
