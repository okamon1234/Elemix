#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    // 実際の敵生成・物理・ドロップ・3択を通す、再現可能な自動プレイ比較。
    public sealed class CombatBalanceProbe : MonoBehaviour
    {
        [Serializable] public sealed class Row { public string character; public int seed, level, kills, deaths, experience; public float seconds; }
        [Serializable] sealed class Report { public List<Row> runs = new List<Row>(); public string error; }
        readonly Report report = new Report();
        PlayerHealth player; PlayerStats stats; Rigidbody2D body; LevelUpManager levels;
        Row row; bool physical; byte[] save, backup; float oldMaximumDelta;
        readonly FieldInfo offers = typeof(LevelUpManager).GetField("offered", BindingFlags.Instance | BindingFlags.NonPublic);
        void Awake() { DontDestroyOnLoad(gameObject); oldMaximumDelta = Time.maximumDeltaTime; save = Read(BuildSaveService.PathName); backup = Read(BuildSaveService.PathName+".bak"); Application.logMessageReceived += Log; }
        static byte[] Read(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;
        void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception) report.error = message+"\n"+trace; }
        IEnumerator Start()
        {
            foreach (int seed in new[] { 12345, 54321 }) foreach (string character in new[] { "ranger", "warden", "knight", "lancer" }) {
                Time.timeScale=1; UnityEngine.Random.InitState(seed); SceneManager.LoadScene("SoloScene");
                yield return null; yield return null;
                player=PlayerHealth.Local; stats=player.GetComponent<PlayerStats>(); stats.Restore(CharacterCatalog.CreateBuild(character));
                player.GetComponent<PlayerMovement>().enabled=false; body=player.GetComponent<Rigidbody2D>(); levels=player.GetComponent<LevelUpManager>();
                physical=character!="ranger"; GameManager.Instance.soloDuration=1800;
                row=new Row { character=character, seed=seed }; player.Died+=CountDeath;
                Time.maximumDeltaTime=.2f;
                while (GameManager.Instance.Elapsed<120 && string.IsNullOrEmpty(report.error)) { Time.timeScale=GameManager.Instance.ChoosingUpgrade ? 0 : 6; yield return null; }
                row.level=stats.Level; row.experience=stats.Experience; row.kills=GameManager.Instance.Kills; row.seconds=GameManager.Instance.Elapsed;
                player.Died-=CountDeath; report.runs.Add(row); row=null;
                File.WriteAllText("Logs/RogueBalanceResult.json",JsonUtility.ToJson(report,true));
                if (!string.IsNullOrEmpty(report.error)) break;
            }
            Restore(); UnityEditor.EditorApplication.isPlaying=false;
        }
        void CountDeath() { if(row!=null) row.deaths++; }
        void Update()
        {
            if (!levels || row==null) return;
            var list=offers.GetValue(levels) as List<UpgradeOption>; if(list==null || list.Count==0) return;
            int choice=0; float best=-999;
            for(int i=0;i<list.Count;i++) {
                var w=WeaponCatalog.Get(stats,list[i].Kind);
                float score=w ? (WeaponCatalog.IsPhysical(w.Id)==physical?20:0)+(w.Level>0?10:0)-w.Level*.1f : list[i].Kind==UpgradeKind.Power?15:list[i].Kind==UpgradeKind.Regeneration?8:3;
                if(score>best) { best=score; choice=i; }
            }
            levels.Choose(choice);
        }
        void FixedUpdate()
        {
            if(!player || !player.Alive || row==null || GameManager.Instance.ChoosingUpgrade) { if(body) body.linearVelocity=Vector2.zero; return; }
            Vector2 origin=body.position, direction=Vector2.zero; float best=64; ExpOrb nearest=null;
            foreach(var orb in FindObjectsByType<ExpOrb>(FindObjectsSortMode.None)) {
                float d=((Vector2)orb.transform.position-origin).sqrMagnitude; if(d<best) {best=d;nearest=orb;}
            }
            if(nearest) direction=((Vector2)nearest.transform.position-origin).normalized;
            else {
                var enemy=player.GetComponent<AutoTargeting>().FindNearest();
                if(enemy) { Vector2 delta=(Vector2)enemy.transform.position-origin; float desired=physical?1.45f:3.5f; direction=delta.magnitude>desired?delta.normalized:delta.magnitude<desired-.5f?-delta.normalized:new Vector2(-delta.y,delta.x).normalized; }
            }
            body.linearVelocity=ObstacleAvoidance.Steer(origin,direction,.35f,1)*stats.MoveSpeed;
        }
        void Restore()
        {
            Time.timeScale=1; Time.maximumDeltaTime=oldMaximumDelta;
            Put(BuildSaveService.PathName,save); Put(BuildSaveService.PathName+".bak",backup);
        }
        static void Put(string path,byte[] bytes) { if(bytes!=null) File.WriteAllBytes(path,bytes); else if(File.Exists(path)) File.Delete(path); }
        void OnDestroy() { Application.logMessageReceived-=Log; Restore(); }
    }
}
#endif
