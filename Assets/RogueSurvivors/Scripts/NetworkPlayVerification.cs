#if ROGUE_FUSION && (UNITY_EDITOR || DEVELOPMENT_BUILD)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace RogueSurvivors
{
    // Explicit command-line verification only; inactive during normal play.
    public sealed class NetworkPlayVerification : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool success; public string role; public List<string> checks = new List<string>(); public string error; }
        Report report = new Report();
        string room, reportPath;
        float deadline;
        bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--rogue-net-test");
            if (index < 0 || index + 1 >= args.Length) return;
            var go = new GameObject("Network verification"); DontDestroyOnLoad(go);
            var test = go.AddComponent<NetworkPlayVerification>(); test.report.role = args[index + 1];
            BossCatalog.VerificationSelection = test.report.role == "host" ? BossKind.Dragon : BossKind.Spider;
            int roomIndex = Array.IndexOf(args, "--rogue-room"), pathIndex = Array.IndexOf(args, "--rogue-report");
            test.room = roomIndex >= 0 && roomIndex + 1 < args.Length ? args[roomIndex + 1] : "verification";
            test.reportPath = pathIndex >= 0 && pathIndex + 1 < args.Length ? args[pathIndex + 1] : Path.Combine(Application.persistentDataPath, "NetworkVerification.json");
            test.deadline = Time.realtimeSinceStartup + 135;
            Application.logMessageReceived += test.Log;
            test.StartCoroutine(test.Run());
        }
        void Log(string text, string stack, LogType type)
        { if (!finished && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Finish(false, text + "\n" + stack); }
        void Update() { if (!finished && Time.realtimeSinceStartup > deadline) Finish(false, "Timeout: " + (NetworkManager.Instance ? NetworkManager.Instance.Status : "No NetworkManager")); }
        void Pass(string name) { report.checks.Add(name); Debug.Log("NETWORK_CHECK: " + name); }
        IEnumerator Run()
        {
            yield return null; yield return null;
            var task = NetworkManager.Instance.JoinAsync(room, false);
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted || !task.Result) { Finish(false, NetworkManager.Instance.Status); yield break; }
            Pass("Fusion Shared cloud connection");
            while (NetworkManager.Instance.PlayerCount < 2) yield return null;
            Pass("Two independent clients joined the same private room");
            if (report.role == "host") NetworkManager.Instance.StartArena();
            while (!PlayerHealth.Local || !FindFirstObjectByType<BossAI>() || FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).Length < 2) yield return null;
            Pass("Synchronized arena load and player/boss spawns");
            var local = PlayerHealth.Local;
            local.GrantInvulnerability(120);
            foreach (var weapon in local.GetComponents<WeaponBase>()) weapon.enabled = false;
            local.GetComponent<PlayerMovement>().enabled = false;
            var boss = FindFirstObjectByType<BossAI>();
            if (NetworkManager.IsMaster) boss.enabled = false;
            foreach (var projectile in FindObjectsByType<Bullet>(FindObjectsSortMode.None)) Destroy(projectile.gameObject);
            foreach (var projectile in FindObjectsByType<AreaProjectile>(FindObjectsSortMode.None)) Destroy(projectile.gameObject);
            var remote = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).First(p => p != local);
            Vector3 before = remote.transform.position;
            Vector3 localBefore = local.transform.position;
            float until = Time.realtimeSinceStartup + 4;
            bool moved = false;
            while (Time.realtimeSinceStartup < until) {
                if (!local || !remote) { Finish(false, "Peer left during movement verification"); yield break; }
                local.GetComponent<Rigidbody2D>().linearVelocity = Vector2.right;
                moved |= Vector3.Distance(before, remote.transform.position) > .3f;
                yield return null;
            }
            local.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            if (!moved) { Finish(false, "Remote NetworkTransform did not move; local " + localBefore + " -> " + local.transform.position + "; remote " + before + " -> " + remote.transform.position); yield break; }
            Pass("Remote player movement replicated");
            if(report.role=="guest") {
                var fusionStats=local.GetComponent<PlayerStats>();
                var mage=CharacterCatalog.CreateBuild("mage","wood");mage.level=fusionStats.Level;fusionStats.Restore(mage);
                foreach(var w in local.GetComponents<WeaponBase>())w.SetLevel(0);
                fusionStats.GetComponent<DaggerWeapon>().SetLevel(4);fusionStats.GetComponent<GauntletWeapon>().SetLevel(4);
                local.GetComponent<NetworkPlayerSync>().PublishBuild();
                while(!FindFirstObjectByType<PhysicalMissile>())yield return null;
                Pass("Combined physical weapon effect RPC rendered on guest");
            } else {
                while(!remote.GetComponent<PlayerStats>().FusionIds.Contains("knifegloves"))yield return null;
                if(remote.GetComponent<PlayerStats>().WeaponCount!=1){Finish(false,"Fusion did not replicate as one weapon slot");yield break;}
                Pass("Two ingredients replicated as a single fused weapon");
                if(remote.GetComponent<PlayerStats>().StartingMageWeapon!="wood") {Finish(false,"Mage starting choice did not replicate");yield break;}
                Pass("Mage initial wood element replicated independently from current weapons");
                local.GetComponent<NetworkPlayerSync>().BroadcastEffect(2024,local.transform.position,local.transform.position+Vector3.right*5);
            }
            yield return new WaitForSecondsRealtime(.8f);
            var health = boss.GetComponent<EnemyHealth>();
            int totalLevel = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None).Sum(p => p.Level);
            if (boss.GetComponent<BossDifficulty>().TeamLevel != totalLevel) { Finish(false, "Boss team level does not match player builds"); yield break; }
            Pass("Boss difficulty uses both player builds");
            if (boss.Kind != BossKind.Dragon) { Finish(false, "Host boss selection was not replicated: " + boss.Kind); yield break; }
            Pass("Host selected dragon overrides guest boss selection");
            var parts = boss.GetComponent<BossParts>();
            local.GetComponent<PhysicalFusionCombat>().enabled=false;
            var ping=local.GetComponent<PlayerPartPing>();
            ping.SetPart(parts,report.role=="host"?2:1);
            while(remote.GetComponent<PlayerPartPing>().VisiblePart(parts)!=(report.role=="host"?1:2))yield return null;
            Pass("Independent part pings from both players replicated");
            var originalRemoteExpiry=remote.GetComponent<NetworkPlayerSync>().PingExpiry;
            yield return new WaitForSecondsRealtime(.75f);
            if(report.role=="host"){
                ping.SetPart(parts,-1);yield return new WaitForSecondsRealtime(.5f);
                if(remote.GetComponent<PlayerPartPing>().VisiblePart(parts)!=1){Finish(false,"Clearing host ping erased guest ping");yield break;}
            } else {
                while(remote.GetComponent<PlayerPartPing>().VisiblePart(parts)!=-1)yield return null;
                if(ping.VisiblePart(parts)!=1){Finish(false,"Other player clear removed local ping");yield break;}
            }
            Pass("Clearing one player's ping preserves the other player's target");
            yield return new WaitForSecondsRealtime(8.2f);
            if(!originalRemoteExpiry.ExpiredOrNotRunning(local.GetComponent<NetworkPlayerSync>().Runner) || ping.VisiblePart(parts)!=-1){Finish(false,"Part ping did not expire");yield break;}
            Pass("Part pings expire on both clients after eight seconds");
            ping.SetPart(parts,0);
            while(remote.GetComponent<PlayerPartPing>().VisiblePart(parts)!=0)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            if (report.role == "guest") {
                while (boss.GetComponent<NetworkEnemySync>().ElementAura != 0) yield return null;
                yield return new WaitForSecondsRealtime(.5f);
                health.Damage(10, Vector2.zero, local, CombatElement.Fire);
                yield return new WaitForSecondsRealtime(.15f);
                health.Damage(10, Vector2.zero, local, CombatElement.Earth);
                float pickupDeadline=Time.realtimeSinceStartup+3;
                while(!FindFirstObjectByType<CrystalPickup>() && Time.realtimeSinceStartup<pickupDeadline) yield return null;
                var crystal=FindFirstObjectByType<CrystalPickup>();
                if(!crystal) {Finish(false,"Crystal pickup was not replicated");yield break;}
                local.GetComponent<Rigidbody2D>().position=crystal.transform.position;
                float shieldDeadline = Time.realtimeSinceStartup + 3;
                while (!local.HasBarrier && Time.realtimeSinceStartup < shieldDeadline) yield return null;
                if (!local.HasBarrier) { Finish(false, "Crystal shield did not reach guest owner"); yield break; }
                Pass("Earth reaction shield replicated to guest owner");
                yield return new WaitForSecondsRealtime(1);
                health.Damage(10,Vector2.zero,local,CombatElement.Water);
                yield return new WaitForSeconds(.2f);
                health.Damage(10,Vector2.zero,local,CombatElement.Wind);
                health.Damage(10,Vector2.zero,local,CombatElement.Earth);
                yield return new WaitForSeconds(.3f);
                if(boss.GetComponent<NetworkEnemySync>().ElementAura!=(int)CombatElement.Water || FindFirstObjectByType<CrystalPickup>()) {Finish(false,"Protected guest wind/earth consumed water or created crystal");yield break;}
                Pass("Guest wind and protected earth preserve water without another crystal");

                yield return new WaitForSecondsRealtime(.2f);
                health.Damage(10,Vector2.zero,local,CombatElement.Wood);
                while(boss.GetComponent<NetworkEnemySync>().BloomState.z!=1)yield return null;
                Pass("Guest created a seed on authority and received its synchronized state");
                yield return new WaitForSecondsRealtime(.8f);
                health.Damage(10,Vector2.zero,local,CombatElement.Lightning);
                while(boss.GetComponent<NetworkEnemySync>().BloomState.z!=2)yield return null;
                Pass("Guest third element triggered synchronized hyperbloom");
                while(boss.GetComponent<NetworkEnemySync>().BloomState.z!=0)yield return null;
                Pass("Consumed seed cleared on guest without duplicate detonation");
                ping.SetPart(parts,0);yield return new WaitForSecondsRealtime(.5f);
                for (int part = 0; part < 4; part++) {
                    float angle = part * Mathf.PI / 2;
                    local.GetComponent<Rigidbody2D>().position = (Vector2)boss.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3;
                    yield return new WaitForSecondsRealtime(.4f);
                    yield return new WaitForSecondsRealtime(part==1?3:0);
                    while(boss.IsTransforming) yield return null;
                    int hits = 0;
                    while (parts.Health[part] > 0 && hits++ < 200) {
                        float partBefore = parts.Health[part];
                        health.Damage(200, Vector2.zero, local, hits % 2 == 0 ? CombatElement.Wind : CombatElement.Fire);
                        float partDeadline = Time.realtimeSinceStartup + 3;
                        while (parts.Health[part] == partBefore && Time.realtimeSinceStartup < partDeadline) yield return null;
                        if (parts.Health[part] == partBefore) { Finish(false, "Part damage not acknowledged: " + part); yield break; }
                        if (!parts.Exposed && health.Current < health.maximum*(1-BossBreakReward.BudgetFraction)-.1f) { Finish(false, "Early core reward exceeded its budget"); yield break; }
                    }
                    if(part==0){
                        while(boss.Phase<2 || boss.IsTransforming || !boss.GetComponent<BossBreakReward>().Open)yield return null;
                        float beforeReward=health.Current;
                        health.Damage(10,Vector2.zero,local);
                        while(health.Current>=beforeReward)yield return null;
                        while(boss.GetComponent<NetworkEnemySync>().BreakRewardState.w>=health.maximum*BossBreakReward.BudgetFraction)yield return null;
                        Pass("Guest close-range weak point hit damages protected core through authority");
                        if(ping.VisiblePart(parts)!=-1 || remote.GetComponent<PlayerPartPing>().VisiblePart(parts)!=-1){Finish(false,"Broken part ping stayed visible");yield break;}
                        Pass("Destroyed part automatically hides both players' pings");
                    }
                }
            }
            if(report.role=="host") {
                while(boss.GetComponent<NetworkEnemySync>().BloomState.z!=1) yield return null;
                Pass("Authority created shared seed from guest hits");
                while(boss.GetComponent<NetworkEnemySync>().BloomState.z!=2) yield return null;
                Pass("Authority accepted guest third element once");
                ping.SetPart(parts,0);
                while(parts.BrokenCount==0) yield return null;
                boss.enabled=true; yield return new WaitForSecondsRealtime(1.3f);
                if(boss.Phase!=2 || !boss.IsTransforming) {Finish(false,"First part did not trigger phase two");yield break;}
                Pass("First broken part triggers phase two while core remains full");
                yield return new WaitForSecondsRealtime(2.5f);
                while(health.Current>=health.maximum)yield return null;
                if(boss.GetComponent<BossBreakReward>().Budget>=health.maximum*BossBreakReward.BudgetFraction){Finish(false,"Weak point budget did not decrease");yield break;}
                Pass("Authority accepts guest weak point hit and shares consumed reward budget");
                boss.enabled=false; boss.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            }
            while (!parts.Exposed) yield return null;
            if (health.Current < health.maximum*(1-BossBreakReward.BudgetFraction)-.1f) { Finish(false, "Unexpected core damage outside first reward window"); yield break; }
            Pass("Four armor parts and bounded first-break core reward replicated");
            if(parts.BreakOrder!=(1|(2<<3)|(3<<6)|(4<<9))) {Finish(false,"Break order was not replicated");yield break;}
            Pass("Exact part break order replicated to both clients");
            float beforeHit = health.Current;
            if (report.role == "guest") { yield return new WaitForSecondsRealtime(.75f); health.Damage(20, Vector2.zero, local); }
            while (health.Current >= beforeHit) yield return null;
            Pass("Guest attack reached boss authority and HP replicated");
            if (report.role == "host") {
                boss.GetComponent<NetworkEnemySync>().BroadcastVolley(0, 14, 1, 1);
                boss.GetComponent<NetworkEnemySync>().BroadcastAttack((int)BossAttack.LightningLanes, boss.transform.position, Vector2.zero, 2, 1, 0);
                yield return new WaitForSecondsRealtime(1.2f);
                health.ApplyDamage(health.maximum*.55f, Vector2.zero);
                boss.enabled=true;
                yield return new WaitForSecondsRealtime(1.3f);
                if (!boss.IsTransforming || boss.Phase<3) { Finish(false,"Authority did not enter transformation"); yield break; }
                Pass("Authority enters dragon third phase");
                yield return new WaitForSecondsRealtime(2.6f);
                if (boss.PendingAttack != BossAttackRoutes.Choose(boss.Kind,3,0,parts.BreakOrder)) { Finish(false,"Dragon third phase did not select selected break route"); yield break; }
                Pass("Third phase changes boss attack pattern");
                yield return new WaitForSecondsRealtime(3);
                health.ApplyDamage(health.maximum + 1, Vector2.zero);
            }
            else {
                while (!FindObjectsByType<Bullet>(FindObjectsSortMode.None).Any(b => b.gameObject.layer == LayerMask.NameToLayer("EnemyBullet"))) yield return null;
                Pass("Boss volley RPC rendered on guest");
                while (FindObjectsByType<BossHazard>(FindObjectsSortMode.None).Length < 5) yield return null;
                Pass("Five lightning hazard telegraphs created on guest by attack RPC");
                while (boss.Phase<3) yield return null;
                if (boss.Kind != BossKind.Dragon) { Finish(false,"Wrong boss type during phase change"); yield break; }
                Pass("Boss identity and third phase replicated on guest");
                while (boss.PendingAttack != BossAttackRoutes.Choose(boss.Kind,3,0,parts.BreakOrder)) yield return null;
                Pass("Third phase selected break route attack state replicated on guest");
            }
            while (!GameManager.Instance.Won) yield return null;
            Pass("Victory state replicated and result displayed");
            yield return new WaitForSecondsRealtime(2);
            var leave = NetworkManager.Instance.LeaveAsync("HomeScene");
            while (!leave.IsCompleted) yield return null;
            Pass("Runner shutdown and return to home");
            Finish(true, null);
        }
        void Finish(bool success, string error)
        {
            if (finished) return;
            finished = true; report.success = success; report.error = error;
            BossCatalog.VerificationSelection = null;
            Application.logMessageReceived -= Log;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            Application.Quit(success ? 0 : 1);
        }
    }
}
#endif
