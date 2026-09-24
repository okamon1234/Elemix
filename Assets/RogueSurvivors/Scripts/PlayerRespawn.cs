using System.Collections;
using UnityEngine;
namespace RogueSurvivors
{
    [RequireComponent(typeof(PlayerHealth), typeof(PlayerStats))]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        PlayerHealth health;
        bool recovering;
        void Awake() => health = GetComponent<PlayerHealth>();
        void OnEnable() { if (!health) health = GetComponent<PlayerHealth>(); health.Died += OnDeath; }
        void OnDisable() { if (health) health.Died -= OnDeath; }
        void OnDeath() { if (!recovering && health.IsLocal) StartCoroutine(Recover()); }
        IEnumerator Recover()
        {
            recovering = true;
            GetComponent<LevelUpManager>()?.CancelChoices();
            int lost = GetComponent<PlayerStats>().LoseRecentLevels(3);
            HUDController.Instance?.Toast("倒れました。レベル−" + lost + "／直近の強化を失いました。3秒後に復活します。");
            EffectsService.Instance?.Burst(transform.position, new Color(.9f, .3f, .3f));
            GetComponent<NetworkPlayerSync>()?.PublishBuild();
            if (GameManager.Instance.Mode == RunMode.Boss) GameManager.Instance.SaveCurrentBuild();
            yield return new WaitForSeconds(3);
            if (GameManager.Instance && GameManager.Instance.IsPlaying) {
                Vector2 position = FindSafeSpot();
                var body = GetComponent<Rigidbody2D>(); body.position = position; body.linearVelocity = Vector2.zero;
                health.ResetHealth(); health.GrantInvulnerability(3);
                GetComponent<NetworkPlayerSync>()?.PublishBuild();
                HUDController.Instance?.Toast("復活しました。3秒間はダメージを受けません。");
            }
            recovering = false;
        }
        Vector2 FindSafeSpot()
        {
            Vector2 origin = GameManager.Instance.Mode == RunMode.Boss ? Vector2.zero : (Vector2)transform.position;
            Vector2 best = origin; float bestDistance = -1;
            for (int i = 0; i < 24; i++) {
                Vector2 p = origin + Random.insideUnitCircle * 6;
                if (GameManager.Instance.Mode == RunMode.Boss) p = BossArena.Clamp(p, 3);
                if (Physics2D.OverlapCircle(p, .5f, LayerMask.GetMask("World"))) continue;
                float nearest = 1000;
                foreach (var enemy in EnemyHealth.Active) if (enemy && enemy.Alive) nearest = Mathf.Min(nearest, Vector2.Distance(enemy.transform.position, p));
                if (nearest > bestDistance) { bestDistance = nearest; best = p; }
            }
            return best;
        }
    }
}
