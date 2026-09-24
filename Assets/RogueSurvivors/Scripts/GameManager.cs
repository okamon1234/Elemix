using UnityEngine;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    public enum RunMode { Solo, Lobby, Boss, Home }
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public float soloDuration = 180;
        public RunMode Mode { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool Won { get; private set; }
        public PlayerDataData SavedBuild { get; private set; }
        public string SelectedCharacter { get; private set; } = "ranger";
        public bool ChoosingUpgrade { get; set; }
        void OnEnable() { if (!Instance) Instance = this; }
        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            SavedBuild = BuildSaveService.Load();
            SelectedCharacter = CharacterCatalog.Find(PlayerPrefs.GetString("SelectedCharacter", "ranger")).Id;
            soloDuration = Mathf.Clamp(PlayerPrefs.GetFloat("PreparationSeconds", 180), 30, 1800);
        }
        public void Begin(RunMode mode)
        {
            Time.timeScale = 1; Mode = mode; Elapsed = 0; IsPlaying = mode == RunMode.Solo || mode == RunMode.Boss;
            Won = false; ChoosingUpgrade = false;
            Kills = mode == RunMode.Solo ? 0 : SavedBuild?.kills ?? 0;
        }
        void Update()
        {
            if (!IsPlaying || ChoosingUpgrade) return;
            Elapsed += Time.deltaTime;
            if (Mode == RunMode.Solo && Elapsed >= soloDuration) CompleteSolo();
        }
        public void AddKill() => Kills++;
        public void SelectCharacter(string id)
        {
            SelectedCharacter = CharacterCatalog.Find(id).Id;
            PlayerPrefs.SetString("SelectedCharacter", SelectedCharacter); PlayerPrefs.Save();
        }
        public void StartSolo() { Time.timeScale = 1; SceneManager.LoadScene("SoloScene"); }
        public void SetPreparationTime(float seconds)
        {
            soloDuration = Mathf.Clamp(seconds, 30, 1800);
            PlayerPrefs.SetFloat("PreparationSeconds", soloDuration); PlayerPrefs.Save();
        }
        public void SaveCurrentBuild()
        {
            if (!PlayerHealth.Local) return;
            SavedBuild = PlayerHealth.Local.GetComponent<PlayerStats>().Capture(); BuildSaveService.Save(SavedBuild);
        }
        public void OpenLobby() { Time.timeScale = 1; SceneManager.LoadScene("LobbyScene"); }
        public void ReturnHome()
        {
            Time.timeScale = 1;
            if (NetworkManager.InRoom) { NetworkManager.Instance.Leave(); return; }
            SceneManager.LoadScene("HomeScene");
        }
        public void CompleteSolo()
        {
            if (Mode != RunMode.Solo || !PlayerHealth.Local || !PlayerHealth.Local.Alive || !IsPlaying) return;
            SavedBuild = PlayerHealth.Local.GetComponent<PlayerStats>().Capture();
            bool saved = BuildSaveService.Save(SavedBuild);
            IsPlaying = false; ChoosingUpgrade = false; Time.timeScale = 1;
            SceneManager.LoadScene("LobbyScene");
            if (!saved) Debug.LogWarning("Build retained in memory; disk save unavailable.");
        }
        public void Finish(bool won)
        {
            if (!IsPlaying) return;
            IsPlaying = false; Won = won; ChoosingUpgrade = false;
            if (!NetworkManager.InRoom) Time.timeScale = 0;
            HUDController.Instance?.ShowResult(won);
        }
        public void Retry()
        {
            Time.timeScale = 1;
            if (NetworkManager.InRoom) { NetworkManager.Instance.Leave(); return; }
            SceneManager.LoadScene("SoloScene");
        }
        void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1; } }
    }
}
