#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    // 同じ武器強化回数・能力値で、装甲DPS、核DPS、実ボス戦を比較する。
    public sealed class BossBalanceProbe : MonoBehaviour
    {
        [Serializable] public sealed class Row {public string build,boss,test;public int points,slots,fusions,broken;public float seconds,armorDps,coreDps,damageTaken,hpRemaining,armorTime,bossMaximum,weakPointDamage;public bool won,alive;}
        [Serializable] sealed class Report {public List<Row> runs=new List<Row>();public string error;}
        readonly Report report=new Report();PlayerHealth player;PlayerStats stats;BossAI boss;BossParts parts;EnemyHealth enemy;Rigidbody2D body;
        bool live,physical;byte[] save,backup;float oldDelta;
        void Awake(){DontDestroyOnLoad(gameObject);oldDelta=Time.maximumDeltaTime;save=Read(BuildSaveService.PathName);backup=Read(BuildSaveService.PathName+".bak");Application.logMessageReceived+=Log;}
        static byte[] Read(string path)=>File.Exists(path)?File.ReadAllBytes(path):null;
        void Log(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)report.error=text+"\n"+stack;}
        public static PlayerDataData Build(string name,int points)
        {
            var data=new PlayerDataData{level=points,maxHealth=300,moveSpeed=5,damageMultiplier=1,pickupRadius=2.4f};
            if(name=="physical") {
                string[] choices={"greatsword","knifegloves","cycloneaxe","flail","chainscythe"};int pairs=Mathf.Min(5,points/8);
                for(int i=0;i<pairs;i++){var r=PhysicalEvolution.ById(choices[i]);data.physicalFusions.Add(r.Id);data.weapons.Add(new WeaponSaveData(r.Weapon,4));data.weapons.Add(new WeaponSaveData(r.Partner,4));}
                int left=points-pairs*8;
                if(left>0){data.weapons.Add(new WeaponSaveData("crossbow",1));left--;}
                for(int i=0;left>0;i++,left--)data.weapons[i%(pairs*2)].level++;
            } else {
                string[] ids=name=="hyperbloom"?new[]{"water","wood","lightning","bolt"}:name=="burgeon"?new[]{"water","wood","fireball","bolt"}:name=="reaction"?new[]{"fireball","water","ice","bolt"}:new[]{"light","dark","water","lightning"};
                int used=0;foreach(var id in ids){int level=Mathf.Min(8,points/4);data.weapons.Add(new WeaponSaveData(id,level));used+=level;}
                if(points>used){int level=Mathf.Min(8,points-used);data.weapons.Add(new WeaponSaveData("crossbow",level));used+=level;}
                if(points>used)data.weapons.Add(new WeaponSaveData("dagger",points-used));
            }
            return data;
        }
        IEnumerator Load(string build,int points,int kind)
        {
            live=false;Time.timeScale=1;SceneManager.LoadScene("MultiBossScene");yield return null;yield return null;yield return null;
            player=PlayerHealth.Local;stats=player.GetComponent<PlayerStats>();stats.Restore(Build(build,points));physical=build=="physical";
            player.GetComponent<PlayerMovement>().enabled=false;player.GetComponent<PlayerRespawn>().enabled=false;body=player.GetComponent<Rigidbody2D>();
            boss=FindFirstObjectByType<BossAI>();enemy=boss.GetComponent<EnemyHealth>();parts=boss.GetComponent<BossParts>();
            boss.enabled=false;boss.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;boss.GetComponent<Rigidbody2D>().position=Vector2.zero;
            boss.GetComponent<BossDifficulty>().Configure(new[]{stats});float maximum=boss.GetComponent<BossDifficulty>().Health;enemy.SetSynchronizedHealth(maximum,maximum);parts.Restore(Vector4.one*maximum*.12f,maximum*.12f);
            boss.RestoreEncounter(kind,1,0,Vector2.zero,0);boss.RestoreState((int)BossState.Pursue,2,Vector2.down,0);
            body.position=new Vector2(physical?4:5.2f,0);body.linearVelocity=Vector2.zero;
            Time.maximumDeltaTime=.1f;Time.timeScale=4;yield return null;
        }
        IEnumerator Start()
        {
            foreach(int points in new[]{16,32,48})foreach(string build in new[]{"physical","reaction","lightdark","hyperbloom","burgeon"}) {
                yield return Load(build,points,0);player.GrantInvulnerability(60);body.position=new Vector2(2.8f,0);
                enemy.SetSynchronizedHealth(1000000,1000000);parts.Restore(Vector4.one*1000000,1000000);
                float start=Time.time;yield return new WaitForSeconds(8);
                var row=new Row{test="stationary",build=build,boss="Golem",points=points,slots=stats.WeaponCount,fusions=stats.FusionIds.Count};
                row.armorDps=(4000000-Sum(parts.Health))/(Time.time-start);parts.Restore(Vector4.zero,1);boss.RestoreEncounter(0,3,0,Vector2.zero,0);
                float before=enemy.Current;start=Time.time;yield return new WaitForSeconds(8);row.coreDps=(before-enemy.Current)/(Time.time-start);row.seconds=16;report.runs.Add(row);Write();
                if(!string.IsNullOrEmpty(report.error)){Restore();UnityEditor.EditorApplication.isPlaying=false;yield break;}
            }
            foreach(int kind in new[]{0,1,2,3})foreach(string build in new[]{"physical","reaction","lightdark","hyperbloom","burgeon"}) {
                UnityEngine.Random.InitState(24680+kind);yield return Load(build,48,kind);boss.enabled=true;live=true;
                var row=new Row{test="live",build=build,boss=((BossKind)kind).ToString(),points=48,slots=stats.WeaponCount,fusions=stats.FusionIds.Count,bossMaximum=enemy.maximum,armorTime=-1};
                float start=Time.time,lastHP=player.Current,lastBudget=0;int lastPart=-1;
                while(Time.time-start<120 && player.Alive && GameManager.Instance.IsPlaying && string.IsNullOrEmpty(report.error)) {
                    if(player.Current<lastHP)row.damageTaken+=lastHP-player.Current;lastHP=player.Current;
                    if(parts.Exposed&&row.armorTime<0)row.armorTime=Time.time-start;
                    var reward=boss.GetComponent<BossBreakReward>();
                    if(reward.Part!=lastPart){lastPart=reward.Part;lastBudget=enemy.maximum*BossBreakReward.BudgetFraction;}
                    if(reward.Part>=0){row.weakPointDamage+=Mathf.Max(0,lastBudget-reward.Budget);lastBudget=reward.Budget;}
                    yield return null;
                }
                row.seconds=Time.time-start;row.alive=player.Alive;row.won=GameManager.Instance.Won;row.broken=parts?parts.BrokenCount:4;row.hpRemaining=enemy?enemy.Current:0;
                report.runs.Add(row);Write();live=false;if(!string.IsNullOrEmpty(report.error))break;
            }
            Restore();UnityEditor.EditorApplication.isPlaying=false;
        }
        static float Sum(Vector4 v)=>v.x+v.y+v.z+v.w;
        void FixedUpdate()
        {
            if(!live||!player||!player.Alive||!boss||!enemy.Alive)return;
            Vector2 center=boss.transform.position,position=body.position;int part=0;while(part<4&&parts.Health[part]<=0)part++;
            float angle=part<4?part*Mathf.PI*.5f:Mathf.Atan2(position.y-center.y,position.x-center.x)+.25f;
            Vector2 goal=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(physical?4:5.2f);
            var opportunity=boss.GetComponent<BossBreakReward>();
            if(opportunity.Open){angle=opportunity.Part*Mathf.PI*.5f;goal=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*3;}
            Vector2 wanted=(goal-position).normalized,bestDirection=wanted;float best=float.MaxValue;
            var mechanics=FindObjectsByType<BossMechanic>(FindObjectsSortMode.None);var hazards=FindObjectsByType<BossHazard>(FindObjectsSortMode.None);var bullets=FindObjectsByType<Bullet>(FindObjectsSortMode.None);
            for(int i=0;i<12;i++) {
                Vector2 direction=Quaternion.Euler(0,0,i*30)*wanted,candidate=position+direction*1.1f;float score=Vector2.Distance(candidate,goal);
                if(Vector2.Distance(candidate,center)<2)score+=12;
                foreach(var mechanic in mechanics)if(mechanic.Threatens(candidate))score+=12;
                foreach(var hazard in hazards)if(hazard.Contains(candidate))score+=8;
                foreach(var bullet in bullets)if(bullet.gameObject.layer==LayerMask.NameToLayer("EnemyBullet") && Vector2.Distance(candidate,bullet.transform.position)<.9f)score+=5;
                if(boss.State==BossState.Windup||boss.State==BossState.Dash){Vector2 offset=candidate-center;if(Vector2.Dot(offset,boss.Heading)>0 && Mathf.Abs(offset.x*boss.Heading.y-offset.y*boss.Heading.x)<1.7f)score+=8;}
                if(score<best){best=score;bestDirection=direction;}
            }
            if(Vector2.Distance(position,goal)<.3f && best<1.3f)bestDirection=Vector2.zero;
            body.linearVelocity=ObstacleAvoidance.Steer(position,bestDirection,.4f,1)*stats.MoveSpeed;
        }
        void Write()=>File.WriteAllText("Logs/RogueBossBalanceResult.json",JsonUtility.ToJson(report,true));
        static void Put(string path,byte[] bytes){if(bytes!=null)File.WriteAllBytes(path,bytes);else if(File.Exists(path))File.Delete(path);}
        void Restore(){Time.timeScale=1;Time.maximumDeltaTime=oldDelta;Put(BuildSaveService.PathName,save);Put(BuildSaveService.PathName+".bak",backup);}
        void OnDestroy(){Application.logMessageReceived-=Log;Restore();}
    }
}
#endif
