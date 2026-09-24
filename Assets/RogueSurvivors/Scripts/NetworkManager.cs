using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ROGUE_FUSION
using Fusion;
using Fusion.Photon.Realtime;
#endif
namespace RogueSurvivors
{
    public sealed class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }
        public static string FusionAppId => NetworkConfiguration.AppId;
        public string Status { get; private set; } = "部屋コードを決めて参加してください。最大4人で協力できます。";
        public bool Busy { get; private set; }
        bool sessionStarted;
        bool leaving;
#if ROGUE_FUSION
        public NetworkRunner Runner { get; private set; }
#endif
        public static bool InRoom
        {
            get {
#if ROGUE_FUSION
                return Instance && Instance.Runner && Instance.Runner.IsRunning;
#else
                return false;
#endif
            }
        }
        public static bool IsMaster
        {
            get {
#if ROGUE_FUSION
                return !InRoom || Instance.Runner.GameMode == GameMode.Single || Instance.Runner.IsSharedModeMasterClient;
#else
                return true;
#endif
            }
        }
        public static bool FusionAvailable
        {
            get {
#if ROGUE_FUSION
                return true;
#else
                return false;
#endif
            }
        }
        public int PlayerCount
        {
            get {
#if ROGUE_FUSION
                return InRoom ? Runner.ActivePlayers.Count() : 0;
#else
                return 0;
#endif
            }
        }
        public string RoomSummary
        {
            get {
#if ROGUE_FUSION
                if (InRoom) return Runner.SessionInfo.Name + "　／　参加者 " + PlayerCount + " / 4 人";
#endif
                return "まだ部屋に参加していません";
            }
        }
        void OnEnable() { if (!Instance) Instance = this; }
        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }
        void Update()
        {
            if (sessionStarted && !InRoom && !Busy && !leaving)
            {
                sessionStarted = false; Status = "通信が切断されました。もう一度部屋に参加してください。";
                Time.timeScale = 1;
                if (SceneManager.GetActiveScene().name != "LobbyScene") SceneManager.LoadScene("LobbyScene");
            }
        }
        public void Connect() => Status = FusionAvailable ? "接続の準備ができています。部屋コードを入力して「部屋を作成／参加」を押してください。" : "Fusion 2の導入と初期設定が必要です。現在はソロとボス練習を利用できます。";
        public async void Join(string roomName) => await JoinAsync(roomName, false);
        public async Task<bool> JoinAsync(string roomName, bool singlePlayerTest)
        {
            if (Busy || InRoom) { Status = "すでに接続中か、部屋に参加しています。"; return false; }
            roomName = (roomName ?? "").Trim();
            if (roomName.Length < 3 || roomName.Length > 24) { Status = "部屋コードは3～24文字で入力してください。"; return false; }
#if ROGUE_FUSION
            Busy = true; leaving = false; Status = "部屋に接続しています…";
            try
            {
                if (Runner) { await Runner.Shutdown(); if (Runner) Destroy(Runner.gameObject); }
                var runnerGO = new GameObject("Fusion Runner"); DontDestroyOnLoad(runnerGO);
                Runner = runnerGO.AddComponent<NetworkRunner>();
                var sceneManager = runnerGO.AddComponent<NetworkSceneManagerDefault>();
                var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
                if (!string.IsNullOrWhiteSpace(FusionAppId)) appSettings.AppIdFusion = FusionAppId; appSettings.AppVersion = "Survivors-Fusion2-v3-bosses";
                appSettings.FixedRegion = "jp"; appSettings.UseNameServer = true;
                int lobbyIndex = SceneUtility.GetBuildIndexByScenePath("Assets/RogueSurvivors/Scenes/LobbyScene.unity");
                if (lobbyIndex < 0) throw new InvalidOperationException("LobbySceneがビルド対象に登録されていません。");
                var scenes = new NetworkSceneInfo(); scenes.AddSceneRef(SceneRef.FromIndex(lobbyIndex), LoadSceneMode.Single);
                var result = await Runner.StartGame(new StartGameArgs {
                    GameMode = singlePlayerTest ? GameMode.Single : GameMode.Shared,
                    SessionName = roomName, PlayerCount = 4, IsOpen = true, IsVisible = false,
                    CustomPhotonAppSettings = appSettings, Scene = scenes, SceneManager = sceneManager });
                if (!result.Ok)
                {
                    Status = "接続できませんでした（" + result.ShutdownReason + "）。通信環境とFusion用App IDを確認してください。";
                    if (Runner) { await Runner.Shutdown(); if (Runner) Destroy(Runner.gameObject); }
                    Runner = null; return false;
                }
                sessionStarted = true;
                Status = "参加しました。全員が揃ったら部屋主がボス戦を開始してください。";
                return true;
            }
            catch (Exception error)
            {
                Status = "接続処理に失敗しました。設定と通信環境を確認してください。";
                Debug.LogException(error);
                if (Runner) { await Runner.Shutdown(); if (Runner) Destroy(Runner.gameObject); }
                Runner = null; return false;
            }
            finally { Busy = false; }
#else
            await Task.CompletedTask; Connect(); return false;
#endif
        }
        public void StartArena()
        {
#if ROGUE_FUSION
            if (!InRoom || !IsMaster || Busy) { Status = "ボス戦を開始できるのは部屋主だけです。"; return; }
            if (!Resources.Load<NetworkObject>("RogueSurvivors/FusionPlayer") || !Resources.Load<NetworkObject>("RogueSurvivors/FusionBoss"))
            { Status = "ネットワーク用Prefabが未生成です。初期設定を実行してください。"; return; }
            if (Runner.GameMode != GameMode.Single) { Runner.SessionInfo.IsOpen = false; Runner.SessionInfo.IsVisible = false; }
            int index = SceneUtility.GetBuildIndexByScenePath("Assets/RogueSurvivors/Scenes/MultiBossScene.unity");
            Runner.LoadScene(SceneRef.FromIndex(index), LoadSceneMode.Single);
#else
            Connect();
#endif
        }
        public void Practice()
        {
            if (InRoom || Busy) { Status = "部屋から退出してから練習を開始してください。"; return; }
            SceneManager.LoadScene("MultiBossScene");
        }
        public async void Leave() => await LeaveAsync("HomeScene");
        public async Task LeaveAsync(string destination)
        {
            if (leaving) return;
            leaving = true; Busy = true; sessionStarted = false; Time.timeScale = 1;
            try
            {
#if ROGUE_FUSION
                if (Runner) { await Runner.Shutdown(); if (Runner) Destroy(Runner.gameObject); Runner = null; }
#endif
                Status = "部屋から退出しました。";
                SceneManager.LoadScene(destination);
            }
            catch (Exception error) { Debug.LogException(error); Status = "退出処理を完了できませんでした。"; }
            finally { Busy = false; leaving = false; }
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}

