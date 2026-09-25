using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace RogueSurvivors
{
    public sealed class SceneBootstrap : MonoBehaviour
    {
        public RunMode mode;
        public GameObject playerPrefab;
        public EnemyHealth enemyPrefab;
        public GameObject bossPrefab;
        public EffectsService effectsPrefab;
        void Awake()
        {
            if (!GameManager.Instance) new GameObject("GameManager").AddComponent<GameManager>();
            if (!NetworkManager.Instance) new GameObject("NetworkManager").AddComponent<NetworkManager>();
            GameManager.Instance.Begin(mode);
            if (effectsPrefab) Instantiate(effectsPrefab);
            if (!FindFirstObjectByType<EventSystem>())
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
        void Start()
        {
            var canvasGO = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            if (mode == RunMode.Home) { canvasGO.AddComponent<HomeUI>(); return; }
            if (mode == RunMode.Lobby) { canvasGO.AddComponent<LobbyUI>(); return; }
            canvasGO.AddComponent<HUDController>();
            canvasGO.AddComponent<PauseMenu>();
            var obstacles = new GameObject("遺跡の障害物").AddComponent<MapObstacles>();
            obstacles.obstaclePrefab = Resources.Load<GameObject>("RogueSurvivors/RuinPillar"); obstacles.arena = mode == RunMode.Boss;
            if (mode == RunMode.Solo)
            {
                var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                player.GetComponent<PlayerStats>().Restore(CharacterCatalog.CreateBuild(GameManager.Instance.SelectedCharacter, MageLoadout.SelectedWeapon));
                BindLocal(player.GetComponent<PlayerHealth>(), true);
                var spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>(); spawner.prefab = enemyPrefab;
                HUDController.Instance.Toast("武器を育ててボスに挑もう　／　時間が経つほど敵が強くなります");
            }
            else
            {
                var spawner = gameObject.AddComponent<NetworkPlayerSpawner>();
                spawner.offlinePlayer = playerPrefab; spawner.offlineBoss = bossPrefab;
                HUDController.Instance.Toast(NetworkManager.InRoom ? "仲間と協力して番人を倒そう" : "ボス戦の練習");
            }
        }
        public static void BindLocal(PlayerHealth player, bool upgrades)
        {
            PlayerHealth.Local = player; player.IsLocal = true;
            HUDController.Instance.Bind(player);
            if (CameraFollow.Instance) CameraFollow.Instance.target = player.transform;
            if (upgrades) player.gameObject.AddComponent<LevelUpManager>().Bind(player.GetComponent<PlayerStats>());
        }
    }
}
