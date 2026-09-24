using System.Collections;
using System.Linq;
using UnityEngine;
#if ROGUE_FUSION
using Fusion;
#endif
namespace RogueSurvivors
{
    public sealed class NetworkPlayerSpawner : MonoBehaviour
    {
        public GameObject offlinePlayer;
        public GameObject offlineBoss;
        IEnumerator Start()
        {
#if ROGUE_FUSION
            if (NetworkManager.InRoom)
            {
                var runner = NetworkManager.Instance.Runner;
                while (runner && runner.IsRunning && runner.SceneManager.IsBusy) yield return null;
                if (!runner || !runner.IsRunning) yield break;
                var playerPrefab = Resources.Load<NetworkObject>("RogueSurvivors/FusionPlayer");
                var bossPrefab = Resources.Load<NetworkObject>("RogueSurvivors/FusionBoss");
                if (!playerPrefab || !bossPrefab)
                {
                    HUDController.Instance.Toast("通信Prefabがありません。ホームに戻って初期設定を確認してください。");
                    yield break;
                }
                int slot = runner.LocalPlayer.PlayerId % 4;
                var build = GameManager.Instance.SavedBuild ?? DefaultBuild();
                runner.Spawn(playerPrefab, new Vector3(-7.5f + slot * 5, -9, 0), Quaternion.identity, runner.LocalPlayer,
                    (r, spawned) => spawned.GetComponent<NetworkPlayerSync>().ConfigureBuild(build));
                if (NetworkManager.IsMaster) {
                    while (runner && runner.IsRunning) {
                        var peers = FindObjectsByType<NetworkPlayerSync>(FindObjectsSortMode.None);
                        if (peers.Length >= NetworkManager.Instance.PlayerCount && peers.All(p => p.BuildReady)) break;
                        yield return null;
                    }
                    if (runner && runner.IsRunning) runner.Spawn(bossPrefab, new Vector3(0, 5, 0), Quaternion.identity);
                }
                yield break;
            }
#endif
            var player = Instantiate(offlinePlayer, new Vector3(0, -9, 0), Quaternion.identity);
            player.GetComponent<PlayerStats>().Restore(GameManager.Instance.SavedBuild ?? DefaultBuild());
            SceneBootstrap.BindLocal(player.GetComponent<PlayerHealth>(), false);
            var boss = Instantiate(offlineBoss, new Vector3(0, 5, 0), Quaternion.identity);
            boss.GetComponent<BossAI>().ConfigureDifficulty(new[] { player.GetComponent<PlayerStats>() });
            yield return null;
        }
        public static PlayerDataData DefaultBuild()
        {
            var build = CharacterCatalog.CreateBuild(GameManager.Instance ? GameManager.Instance.SelectedCharacter : "ranger");
            build.level = 5; build.damageMultiplier += .4f; build.maxHealth += 40;
            foreach (var weapon in build.weapons) weapon.level = Mathf.Max(weapon.level, 3);
            return build;
        }
    }
}
