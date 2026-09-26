#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    public sealed class RogueSmokeTests : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool success; public List<string> checks = new List<string>(); public string error; }
        readonly Report report = new Report();
        Keyboard keyboard;
        InputSettings previousInputSettings;
        byte[] previousSave, previousBackup;
        bool saveTouched;
        string runtimeError;
        void Awake()
        {
            DontDestroyOnLoad(gameObject); Application.logMessageReceived += Log;
            previousInputSettings = InputSystem.settings;
            InputSystem.settings = Instantiate(previousInputSettings);
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        void Log(string condition, string trace, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) runtimeError = condition + "\n" + trace; }
        IEnumerator Start()
        {
            yield return null; yield return null;
            var test = Tests();
            while (true)
            {
                object current;
                try { if (!test.MoveNext()) break; current = test.Current; }
                catch (Exception error) { report.error = error.ToString(); break; }
                yield return current;
            }
            if (!string.IsNullOrEmpty(runtimeError)) report.error = (report.error ?? "") + "\nRuntime error: " + runtimeError;
            report.success = string.IsNullOrEmpty(report.error);
            RestoreSave();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            Time.timeScale = 1;
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/RogueSmokeResult.json", JsonUtility.ToJson(report, true));
            UnityEditor.EditorApplication.isPlaying = false;
        }
        void Check(bool condition, string label)
        { if (!condition) throw new InvalidOperationException(label); report.checks.Add(label); Debug.Log("ROGUE_CHECK: " + label); }
        static bool MapNavigable(SoloMapKind kind,Vector2Int key)
        {
            const int n=33;var blocked=new bool[n,n];var visited=new bool[n,n];var bounds=new List<Rect>();
            foreach(var prop in SoloMapLayout.Generate(kind,key))foreach(var rect in SoloMapLayout.Bounds(prop))bounds.Add(new Rect(rect.xMin-.45f,rect.yMin-.45f,rect.width+.9f,rect.height+.9f));
            int total=0;for(int y=0;y<n;y++)for(int x=0;x<n;x++) {
                Vector2 point=(Vector2)key*24+new Vector2(-12+x*.75f,-12+y*.75f);
                foreach(var rect in bounds)if(rect.Contains(point)){blocked[x,y]=true;break;}
                if(!blocked[x,y])total++;
            }
            var queue=new Queue<Vector2Int>();queue.Enqueue(new Vector2Int(16,16));visited[16,16]=true;int count=0;
            while(queue.Count>0) {
                var cell=queue.Dequeue();if(blocked[cell.x,cell.y])continue;count++;
                foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}) {
                    var next=cell+d;if(next.x<0 || next.x>=n || next.y<0 || next.y>=n || visited[next.x,next.y] || blocked[next.x,next.y])continue;
                    visited[next.x,next.y]=true;queue.Enqueue(next);
                }
            }
            return total==count;
        }
        IEnumerator Tests()
        {
            var player = PlayerHealth.Local;
            Check(player && HUDController.Instance && GameManager.Instance.IsPlaying, "SoloScene bootstraps player, HUD and run state");
            // ユーザーが選んだ短い準備時間で、検証中にシーンが切り替わることを防ぐ。設定保存は変更しない。
            GameManager.Instance.soloDuration=1800;
            var stats = player.GetComponent<PlayerStats>();
            foreach (var character in CharacterCatalog.All) {
                var build = CharacterCatalog.CreateBuild(character.Id); stats.Restore(build);
                Check(build.IsValid() && stats.CharacterId == character.Id && Mathf.Approximately(stats.MaxHealth, build.maxHealth), "Character starting build: " + character.Id);
            }
            foreach(var character in CharacterCatalog.All) {
                Check(ArsenalArt.Hero(character.Id) && ArsenalArt.Hero(character.Id,1) && ArsenalArt.Hero(character.Id,2),"Three painted character frames loaded: "+character.Id);
            }
            foreach(var weapon in WeaponCatalog.All) Check(ArsenalArt.Weapon(weapon.Id),"Painted weapon sprite loaded: "+weapon.Id);
            for(int art=0;art<10;art++) Check(ArsenalArt.Fusion(art),"Painted fusion sprite loaded: "+art);
            for(int role=0;role<5;role++)for(int frame=0;frame<3;frame++)
                Check(BattleArt.Enemy((EnemyRole)role,frame),"Painted enemy animation frame: "+role+"/"+frame);
            for(int art=0;art<12;art++)Check(BattleArt.Effect(art),"Painted attack texture loaded: "+art);
            for(int impact=0;impact<100;impact++)PaintedImpact.Show(0,new Vector2(1000,1000),2,.1f);
            Check(PaintedImpact.ActiveCount<=72,"Painted effects enforce the simultaneous visual limit");
            yield return new WaitForSeconds(.2f);
            Check(PaintedImpact.ActiveCount==0,"Painted effects release their active budget after expiry");
            var artArcher=Instantiate(Resources.Load<EnemyHealth>("RogueSurvivors/EnemyArcher"),new Vector2(1000,1000),Quaternion.identity);
            artArcher.GetComponent<EnemyAI>().enabled=false;
            yield return null;
            Check(artArcher.GetComponentInChildren<SpriteRenderer>().sprite==BattleArt.Enemy(EnemyRole.Archer),"Existing archer prefab uses painted idle sprite");
            Check(artArcher.GetComponent<CircleCollider2D>().radius==Resources.Load<EnemyHealth>("RogueSurvivors/EnemyArcher").GetComponent<CircleCollider2D>().radius,"Painted enemy keeps its original hitbox");
            var bow=artArcher.GetComponent<EnemyCombat>();bow.OverrideMovement(Vector2.right,6,out _);
            Check(bow.WindingUp && bow.AimDirection==Vector2.right,"Archer announces a fixed direction before firing");
            yield return new WaitForSeconds(.38f);bow.OverrideMovement(Vector2.left,6,out _);
            Check(!bow.WindingUp,"Archer releases the shot after the visible windup");
            bool paintedArrow=false;
            foreach(var arrow in FindObjectsByType<Bullet>(FindObjectsSortMode.None))if(arrow.transform.position.x>990) {
                var renderer=arrow.GetComponentInChildren<SpriteRenderer>();paintedArrow|=renderer.sprite==BattleArt.Effect(10) && renderer.sortingOrder>=160;
                Destroy(arrow.gameObject);
            }
            Check(paintedArrow,"Enemy arrow uses new artwork above allied effects");Destroy(artArcher.gameObject);
            var soloMap=SoloMap.Instance;Check(soloMap,"Solo map initializes before gameplay");
            for(int mapIndex=0;mapIndex<2;mapIndex++) {
                var kind=(SoloMapKind)mapIndex;Check(MapArt.Ground(kind),"Painted map floor: "+kind);soloMap.Initialize(kind);
                Check(soloMap.ChunkCount==9,"Solo map keeps nine active chunks: "+kind);
                Check(!Physics2D.OverlapCircle(Vector2.zero,.6f,LayerMask.GetMask("World")),"Map origin is safe: "+kind);
                bool corridor=true;for(int step=-70;step<=70;step++)corridor&=!Physics2D.OverlapCircle(new Vector2(step*.5f,0),.45f,LayerMask.GetMask("World")) && !Physics2D.OverlapCircle(new Vector2(0,step*.5f),.45f,LayerMask.GetMask("World"));
                Check(corridor,"Connected roads cross chunk seams without blocking: "+kind);
                for(int y=-3;y<=3;y++)for(int x=-3;x<=3;x++)Check(MapNavigable(kind,new Vector2Int(x,y)),"Walkable district has no isolated pockets: "+kind+"/"+x+","+y);
                var wanted=soloMap.ObstacleBounds[0].center;Vector2 open=soloMap.FindOpenSpot(wanted);
                Check(!Physics2D.OverlapCircle(open,.6f,LayerMask.GetMask("World")),"Enemy spawn can move out of terrain: "+kind);
                var mapBefore=SoloMapLayout.Generate(kind,Vector2Int.zero);soloMap.RefreshAround(new Vector2(-240,240));
                Check(soloMap.ChunkCount==9,"Distant movement unloads old map chunks: "+kind);soloMap.RefreshAround(Vector2.zero);
                var mapAfter=SoloMapLayout.Generate(kind,Vector2Int.zero);bool same=mapBefore.Count==mapAfter.Count;
                for(int i=0;i<mapBefore.Count && same;i++)same=mapBefore[i].Art==mapAfter[i].Art && mapBefore[i].Position==mapAfter[i].Position;
                Check(same,"Revisiting map reconstructs identical terrain: "+kind);
            }
            for(int prop=0;prop<8;prop++)Check(MapArt.Prop(prop),"Map prop texture: "+prop);
            Check(SoloMapLayout.Key(new Vector2(-12.1f,-12.1f))==new Vector2Int(-1,-1) && SoloMapLayout.Key(new Vector2(11.9f,11.9f))==Vector2Int.zero,"Map chunk boundaries work with negative coordinates");
            soloMap.Initialize(SoloMapCatalog.Selected);
            Check(FindFirstObjectByType<MapObstacles>() && FindFirstObjectByType<MapObstacles>().GetComponentsInChildren<BoxCollider2D>().Length > 0, "Ruin obstacle chunks populate SoloScene");
            var baseline = new PlayerDataData(); baseline.weapons.Add(new WeaponSaveData("bolt", 1)); stats.Restore(baseline);
            var penaltyBuild = baseline.NetworkCopy(); penaltyBuild.level = 4; stats.Restore(penaltyBuild);
            foreach (var kind in new[] { UpgradeKind.Power, UpgradeKind.Speed, UpgradeKind.Orbit }) {
                stats.RecordUpgrade(kind); new UpgradeOption(kind, "", "").Apply(stats);
            }
            var roundTrip = JsonUtility.FromJson<PlayerDataData>(JsonUtility.ToJson(stats.Capture())); stats.Restore(roundTrip);
            Check(stats.LoseRecentLevels(3) == 3 && stats.Level == 1 && stats.DamageMultiplier == 1 && stats.MoveSpeed == 5 && stats.GetComponent<OrbitWeapon>().Level == 0,
                "Saved history reverses the last three actual upgrades exactly");
            var scalingBuild = baseline.NetworkCopy(); scalingBuild.level = 25; stats.Restore(scalingBuild);
            var scalingObject = new GameObject("Difficulty verification"); var scaling = scalingObject.AddComponent<BossDifficulty>();
            scaling.Configure(new[] { stats }); float soloHP = scaling.Health, soloDamage = scaling.DamageScale, soloRate = scaling.AttackRate;
            Check(scaling.TeamLevel == 25, "Boss team level: level 25 x one player = 25");
            scaling.Configure(new[] { stats, stats, stats });
            Check(scaling.TeamLevel == 75 && scaling.Health > soloHP && scaling.DamageScale > soloDamage && scaling.AttackRate > soloRate, "Boss team level 75 increases HP, damage and attack frequency");
            scaling.Configure(new[] { stats, stats, stats, stats });
            Check(scaling.TeamLevel == 100, "Boss team level: level 25 x four players = 100"); Destroy(scalingObject);
            Check(EnemySpawner.HealthGrowth(180) > EnemySpawner.HealthGrowth(60) * 2, "Enemy health continues growing beyond weapon one-shot range");
            foreach (string variant in new[] { "EnemyRat", "EnemyArmored", "EnemyArcher", "EnemyCharger" })
                Check(Resources.Load<EnemyHealth>("RogueSurvivors/" + variant), "Enemy variant configured: " + variant);
            stats.Restore(baseline);
            FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var enemy in new List<EnemyHealth>(EnemyHealth.Active)) if (enemy) Destroy(enemy.gameObject);
            foreach (var weapon in player.GetComponents<WeaponBase>()) weapon.enabled = false;
            keyboard = InputSystem.AddDevice<Keyboard>();
            Vector3 initial = player.transform.position;
            float moveUntil = Time.time + .4f;
            while (Time.time < moveUntil)
            {
                keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Check(player.transform.position.y > initial.y + .5f, "W key moves Rigidbody2D upward");
            var prefab = Resources.Load<GameObject>("RogueSurvivors/Enemy");
            var chaser = Instantiate(prefab, player.transform.position + Vector3.right * 5, Quaternion.identity);
            float distance = Vector3.Distance(chaser.transform.position, player.transform.position);
            yield return new WaitForSeconds(.4f);
            Check(Vector3.Distance(chaser.transform.position, player.transform.position) < distance - .2f, "EnemyAI pursues the player");
            player.Damage(12); float after = player.Current; player.Damage(12);
            Check(Mathf.Approximately(after, player.Maximum - 12) && Mathf.Approximately(after, player.Current), "Damage and invulnerability prevent repeated hits");
            player.ResetHealth(); Destroy(chaser);
            yield return null;
            var victim = Instantiate(prefab, player.transform.position + Vector3.right * 2, Quaternion.identity).GetComponent<EnemyHealth>();
            victim.Scale(.01f); player.GetComponent<ProjectileWeapon>().enabled = true;
            yield return new WaitForSeconds(.9f);
            Check(!victim && GameManager.Instance.Kills > 0, "Automatic projectile kills enemy and increments kill count");
            foreach (var orb in ExpOrb.Active) if (orb) orb.Attract(player);
            yield return new WaitForSeconds(.7f);
            Check(stats.Experience > 0, "Dropped experience is attracted and collected");
            stats.AddExperience(stats.RequiredExperience - stats.Experience);
            yield return null; yield return null;
            Check(stats.Level == 2 && Time.timeScale == 0 && GameObject.Find("Card0") && GameObject.Find("Card1") && GameObject.Find("Card2"), "Level up pauses Solo and displays three cards");
            player.GetComponent<LevelUpManager>().Choose(0);
            yield return null;
            Check(Time.timeScale == 1 && !GameManager.Instance.ChoosingUpgrade, "Selecting a card resumes gameplay");
            stats.AddExperience(stats.RequiredExperience + (8 + stats.Level * 5));
            yield return null; yield return null;
            player.GetComponent<LevelUpManager>().Choose(0);
            yield return null; yield return null;
            Check(Time.timeScale == 0, "Multiple level gains queue a second choice");
            player.GetComponent<LevelUpManager>().Choose(0);
            yield return null;
            Check(Time.timeScale == 1, "Queued choices fully resume simulation");
            player.GetComponent<OrbitWeapon>().SetLevel(2); player.GetComponent<OrbitWeapon>().enabled = true;
            yield return null;
            Check(player.transform.childCount >= 4, "Orbit weapon creates rotating blades");
            player.Damage(20);
            var itemPrefab = Resources.Load<GameObject>("RogueSurvivors/DropItem");
            var item = Instantiate(itemPrefab).GetComponent<DropItem>(); item.SetKind(DropKind.Heal); item.Use(player);
            Check(Mathf.Approximately(player.Current, player.Maximum), "Healing drop restores maximum HP");
            var farOrb = Instantiate(Resources.Load<GameObject>("RogueSurvivors/ExpOrb"), player.transform.position + Vector3.right * 10, Quaternion.identity).GetComponent<ExpOrb>();
            Vector3 orbBefore = farOrb.transform.position;
            item = Instantiate(itemPrefab).GetComponent<DropItem>(); item.SetKind(DropKind.Magnet); item.Use(player);
            yield return new WaitForSeconds(.2f);
            Check(farOrb && farOrb.transform.position.x < orbBefore.x, "Magnet attracts distant experience");
            foreach (var weapon in player.GetComponents<WeaponBase>()) weapon.enabled = false;
            var novaVictim = Instantiate(prefab, player.transform.position + Vector3.right * 5, Quaternion.identity).GetComponent<EnemyHealth>();
            var offscreen = Instantiate(prefab, player.transform.position + Vector3.right * 80, Quaternion.identity).GetComponent<EnemyHealth>();
            item = Instantiate(itemPrefab).GetComponent<DropItem>(); item.SetKind(DropKind.Nova); item.Use(player);
            yield return null;
            Check(!novaVictim && offscreen && offscreen.Alive, "Nova kills visible enemies and preserves offscreen enemies");
            Destroy(offscreen.gameObject);
            var targetA = Instantiate(prefab, player.transform.position + Vector3.right * 2, Quaternion.identity).GetComponent<EnemyHealth>();
            var targetB = Instantiate(prefab, player.transform.position + new Vector3(2.2f, .5f, 0), Quaternion.identity).GetComponent<EnemyHealth>();
            targetA.Scale(500 / targetA.maximum); targetB.Scale(500 / targetB.maximum);
            targetA.GetComponent<EnemyAI>().enabled = false; targetB.GetComponent<EnemyAI>().enabled = false;
            foreach (var kind in new[] { UpgradeKind.Fireball, UpgradeKind.Lightning, UpgradeKind.Spear }) {
                var weapon = WeaponCatalog.Get(stats, kind); int oldLevel = weapon.Level;
                float healthA = targetA.Current, healthB = targetB.Current;
                weapon.SetLevel(3); weapon.enabled = true;
                yield return new WaitForSeconds(kind == UpgradeKind.Fireball ? .65f : .15f);
                weapon.enabled = false; weapon.SetLevel(oldLevel);
                Check(targetA.Current < healthA && targetB.Current < healthB, "Weapon hits multiple appropriate targets: " + kind);
            }
            Destroy(targetA.gameObject); Destroy(targetB.gameObject);
            foreach (var projectile in FindObjectsByType<AreaProjectile>(FindObjectsSortMode.None)) Destroy(projectile.gameObject);
            Check(JsonUtility.ToJson(stats.Capture().NetworkCopy()).Length <= 1536, "Build data fits Fusion network string capacity");
            var savedRoster = stats.Capture();
            int physicalCount = 0, elementalCount = 0;
            foreach (var definition in WeaponCatalog.All) { if (WeaponCatalog.IsPhysical(definition.Id)) physicalCount++; else elementalCount++; }
            Check(physicalCount == 12 && elementalCount == 9, "Roster: twelve physical weapons with nine elemental weapons");
            Check(WeaponCatalog.OfferWeight("knight", "sword") == 3 && WeaponCatalog.OfferWeight("knight", "water") == 1 && WeaponCatalog.OfferWeight("mage", "water") == 3 && WeaponCatalog.OfferWeight("ranger", "sword") == 1, "Melee and ranged classes bias weapon offers without excluding other builds");
            foreach (var definition in WeaponCatalog.All) {
                var weapon = WeaponCatalog.Get(stats, definition.Kind);
                Check(weapon != null, "Player contains weapon: " + definition.Id);
                foreach (var other in player.GetComponents<WeaponBase>()) { other.enabled = false; other.SetLevel(0); }
                var weaponVictim = Instantiate(prefab, player.transform.position + Vector3.right * 1.6f, Quaternion.identity).GetComponent<EnemyHealth>();
                weaponVictim.Scale(10000 / weaponVictim.maximum); weaponVictim.GetComponent<EnemyAI>().enabled = false;
                float initialWeaponHealth = weaponVictim.Current; weapon.SetLevel(4); weapon.enabled = true;
                yield return new WaitForSeconds(definition.Id == "orbit" ? 1.7f : definition.Id == "fireball" ? .6f : .25f);
                Check(weaponVictim.Current < initialWeaponHealth, "Weapon attack deals damage: " + definition.Id);
                weapon.enabled = false; Destroy(weaponVictim.gameObject);
            }
            foreach (var recipe in PhysicalEvolution.Recipes) {
                foreach (var weapon in player.GetComponents<WeaponBase>()) weapon.SetLevel(0);
                WeaponBase primary = null, partner = null;
                foreach (var weapon in player.GetComponents<WeaponBase>()) { if (weapon.Id == recipe.Weapon) primary = weapon; if (weapon.Id == recipe.Partner) partner = weapon; }
                primary.SetLevel(4); partner.SetLevel(4);
                Check(PhysicalEvolution.Name(primary)==recipe.Name && stats.WeaponCount==1 && PhysicalEvolution.IsPartner(partner) && !stats.CanAcquire(partner),"Two ingredients become one usable slot: "+recipe.Name);
                primary.enabled=true;partner.enabled=true;
                foreach(var shot in FindObjectsByType<PhysicalMissile>(FindObjectsSortMode.None))Destroy(shot.gameObject);
                var fusionVictim=Instantiate(prefab,player.transform.position+Vector3.right*2,Quaternion.identity).GetComponent<EnemyHealth>();
                fusionVictim.Scale(10000/fusionVictim.maximum);fusionVictim.GetComponent<EnemyAI>().enabled=false;
                float fusionHP=fusionVictim.Current;
                yield return new WaitForSeconds(1.2f);
                Check(fusionVictim.Current<fusionHP,"Combined weapon has its own damaging attack: "+recipe.Name);
                Check(WeaponCatalog.EstimateBossDps(partner)==0 && WeaponCatalog.EstimateBossDps(primary)>0,"Fusion is counted once in boss scaling: "+recipe.Name);
                primary.enabled=false;partner.enabled=false;Destroy(fusionVictim.gameObject);
                foreach(var pattern in FindObjectsByType<PhysicalAttackPattern>(FindObjectsSortMode.None))Destroy(pattern.gameObject);
                foreach(var shot in FindObjectsByType<PhysicalMissile>(FindObjectsSortMode.None))Destroy(shot.gameObject);
                foreach(var slash in FindObjectsByType<PhysicalSlash>(FindObjectsSortMode.None))Destroy(slash.gameObject);
                partner.SetLevel(3); Check(!PhysicalEvolution.Active(primary), "Losing ingredient level removes evolution: " + recipe.Name);
            }
            var mergeBuild=new PlayerDataData {level=10};
            foreach(var pair in new[]{new WeaponSaveData("sword",4),new WeaponSaveData("shield",3),new WeaponSaveData("bolt",1),new WeaponSaveData("fireball",1),new WeaponSaveData("water",1),new WeaponSaveData("ice",1)})mergeBuild.weapons.Add(pair);
            stats.Restore(mergeBuild);
            stats.RecordUpgrade(UpgradeKind.Shield);new UpgradeOption(UpgradeKind.Shield,"","").Apply(stats);
            Check(stats.WeaponCount==5 && stats.CanAcquire(stats.GetComponent<SpearWeapon>()),"Fusion at six slots frees one slot for a new weapon");
            stats.RecordUpgrade(UpgradeKind.Spear);new UpgradeOption(UpgradeKind.Spear,"","").Apply(stats);
            stats.RecordUpgrade(UpgradeKind.Sword);new UpgradeOption(UpgradeKind.Sword,"","").Apply(stats);
            Check(stats.WeaponCount==6 && PhysicalEvolution.DisplayLevel(stats.GetComponent<SwordWeapon>())==2,"Fusion card upgrades combined weapon after refilling slot");
            var fusionSave=JsonUtility.FromJson<PlayerDataData>(JsonUtility.ToJson(stats.Capture()));stats.Restore(fusionSave);
            Check(fusionSave.IsValid() && stats.WeaponCount==6 && stats.FusionIds.Contains("greatsword"),"Fusion identity and material levels survive save round trip");
            stats.LoseRecentLevels(3);
            Check(stats.WeaponCount==6 && stats.FusionIds.Count==0 && stats.GetComponent<SwordWeapon>().Level==4 && stats.GetComponent<ShieldWeapon>().Level==3 && stats.GetComponent<SpearWeapon>().Level==0,"Three-upgrade rollback restores both ingredients and the previously full loadout");
            foreach(var weapon in player.GetComponents<WeaponBase>())weapon.SetLevel(0);
            stats.GetComponent<SwordWeapon>().SetLevel(4);stats.GetComponent<ShieldWeapon>().SetLevel(4);PhysicalEvolution.Refresh(stats);
            stats.GetComponent<HammerWeapon>().SetLevel(4);PhysicalEvolution.Refresh(stats);
            Check(stats.FusionIds.Count==1 && stats.FusionIds[0]=="greatsword" && !PhysicalEvolution.Active(stats.GetComponent<HammerWeapon>()),"A consumed ingredient cannot participate in a second fusion");
            for(int i=0;i<7;i++)stats.GetComponent<SwordWeapon>().Upgrade();
            Check(PhysicalEvolution.DisplayLevel(stats.GetComponent<SwordWeapon>())==8,"Combined weapon upgrades to its own level eight cap");
            foreach(var definition in WeaponCatalog.All) if(WeaponCatalog.IsPhysical(definition.Id)) {
                bool hasRecipe=false;foreach(var recipe in PhysicalEvolution.Recipes)hasRecipe|=recipe.Weapon==definition.Id||recipe.Partner==definition.Id;
                Check(hasRecipe,"Every physical weapon has a fusion path: "+definition.Id);
            }
            var fiveBuild=new PlayerDataData {level=41};
            foreach(var recipeId in new[]{"greatsword","cycloneaxe","knifegloves","flail","chainscythe"}) {
                var recipe=PhysicalEvolution.ById(recipeId);fiveBuild.weapons.Add(new WeaponSaveData(recipe.Weapon,4));fiveBuild.weapons.Add(new WeaponSaveData(recipe.Partner,4));fiveBuild.physicalFusions.Add(recipe.Id);
            }
            fiveBuild.weapons.Add(new WeaponSaveData("crossbow",1));stats.Restore(fiveBuild);
            Check(stats.FusionIds.Count==5 && stats.WeaponCount==6 && !stats.CanAcquire(stats.GetComponent<BoomerangWeapon>()),"Five distinct fusions plus one normal weapon fill six slots");
            Check(stats.Capture().IsValid() && JsonUtility.ToJson(stats.Capture().NetworkCopy()).Length<=1536,"Five-fusion loadout survives validation and fits network capacity");
            foreach(var weapon in player.GetComponents<WeaponBase>())weapon.enabled=false;
            var noDamageVictim=Instantiate(prefab,player.transform.position+Vector3.right*2,Quaternion.identity).GetComponent<EnemyHealth>();noDamageVictim.GetComponent<EnemyAI>().enabled=false;float noDamageHP=noDamageVictim.Current;
            PhysicalAttackPattern.Create(2,player.transform.position,noDamageVictim.transform.position,4,0,null);
            yield return new WaitForSeconds(.7f);
            Check(noDamageVictim.Current==noDamageHP,"Replicated physical weapon visuals never apply damage");Destroy(noDamageVictim.gameObject);
            foreach (var weapon in player.GetComponents<WeaponBase>()) weapon.SetLevel(0);
            stats.GetComponent<ProjectileWeapon>().SetLevel(1); stats.GetComponent<FireballWeapon>().SetLevel(1); stats.GetComponent<WaterWeapon>().SetLevel(1); stats.GetComponent<IceWeapon>().SetLevel(1);
            Check(stats.ElementWeaponCount == 4 && !stats.CanAcquire(stats.GetComponent<DarkWeapon>()) && stats.CanAcquire(stats.GetComponent<SwordWeapon>()) && stats.CanAcquire(stats.GetComponent<WaterWeapon>()), "Four-element cap permits physical additions and owned-weapon upgrades");
            stats.GetComponent<SwordWeapon>().SetLevel(1); stats.GetComponent<ShieldWeapon>().SetLevel(1);
            Check(!stats.CanAcquire(stats.GetComponent<HammerWeapon>()), "Six total weapon slots cap both categories");
            stats.GetComponent<LightWeapon>().SetLevel(4); stats.GetComponent<DarkWeapon>().SetLevel(4);
            Check(ElementEvolution.Active(stats.GetComponent<LightWeapon>()) && ElementEvolution.Active(stats.GetComponent<DarkWeapon>()),"Light and dark evolve together at level four");
            stats.GetComponent<DarkWeapon>().SetLevel(3);
            Check(!ElementEvolution.Active(stats.GetComponent<LightWeapon>()),"Evolution is removed after ingredient rollback");
            foreach(var w in stats.GetComponents<WeaponBase>()) w.SetLevel(0);
            stats.GetComponent<DaggerWeapon>().SetLevel(1);
            Check(PhysicalEvolution.PartnerWeight(stats,"gauntlet")>PhysicalEvolution.PartnerWeight(stats,"axe"),"Dagger boosts gauntlet offer weight without suppressing unrelated choices");
            var rewards=player.GetComponent<MeleeRewards>(); if(!rewards) rewards=player.gameObject.AddComponent<MeleeRewards>();
            Check(rewards.Reward(2)+rewards.Reward(2)+rewards.Reward(2)+rewards.Reward(2)==10,"Melee experience reward adds exactly 25 percent across four kills");
            Check(StaffCast.Tier(1)==0 && StaffCast.Tier(3)==1 && StaffCast.Tier(5)==2 && StaffCast.Tier(8)==3,"Staff upgrades unlock four visibly different tiers");
            stats.Restore(savedRoster);
            foreach (var weapon in player.GetComponents<WeaponBase>()) weapon.enabled = false;
            var reactionPairs = new[] { new[] { CombatElement.Fire, CombatElement.Wind }, new[] { CombatElement.Fire, CombatElement.Lightning }, new[] { CombatElement.Fire, CombatElement.Ice }, new[] { CombatElement.Fire, CombatElement.Water }, new[] { CombatElement.Water, CombatElement.Lightning }, new[] { CombatElement.Water, CombatElement.Ice }, new[] { CombatElement.Light, CombatElement.Dark }, new[] { CombatElement.Wood, CombatElement.Water }, new[] { CombatElement.Wood, CombatElement.Fire }, new[] { CombatElement.Wood, CombatElement.Lightning }, new[] { CombatElement.Earth, CombatElement.Fire }, new[] { CombatElement.Earth, CombatElement.Water }, new[] { CombatElement.Earth, CombatElement.Ice }, new[] { CombatElement.Earth, CombatElement.Lightning }, new[] { CombatElement.Lightning, CombatElement.Ice }, new[] { CombatElement.Wind, CombatElement.Water }, new[] { CombatElement.Wind, CombatElement.Lightning }, new[] { CombatElement.Wind, CombatElement.Ice } };
            foreach (var pair in reactionPairs) Check(ElementReaction.Recipe(pair[0], pair[1]) > 0 && ElementReaction.Recipe(pair[0], pair[1]) == ElementReaction.Recipe(pair[1], pair[0]), "Reaction works in both orders: " + pair[0] + " + " + pair[1]);
            var statusVictim = Instantiate(prefab, player.transform.position + Vector3.right * 5, Quaternion.identity).GetComponent<EnemyHealth>();
            statusVictim.Scale(10000 / statusVictim.maximum); statusVictim.GetComponent<EnemyAI>().enabled = false;
            statusVictim.Damage(10, Vector2.zero, player, CombatElement.Water); statusVictim.Damage(10, Vector2.zero, player, CombatElement.Ice);
            Check(statusVictim.GetComponent<EnemyAilment>().Frozen, "Water and ice freeze normal enemies");
            yield return new WaitForSeconds(.45f);
            statusVictim.Damage(10, Vector2.zero, player, CombatElement.Fire); statusVictim.Damage(10, Vector2.zero, player, CombatElement.Earth);
            var crystal=FindFirstObjectByType<CrystalPickup>();
            Check(crystal && !player.HasBarrier,"Crystallize drops an item instead of instantly granting protection");
            Vector3 beforePickupPosition=player.transform.position; player.transform.position=crystal.transform.position;
            yield return new WaitForSeconds(.15f);
            Check(player.HasBarrier,"Collecting crystal grants one barrier");
            float barrierHealth=player.Current; player.GrantInvulnerability(0); player.Damage(999);
            Check(!player.HasBarrier && player.Current==barrierHealth,"Crystal consumes exactly one charge and blocks an entire hit");
            yield return new WaitForSeconds(.7f); player.Damage(5);
            Check(player.Current<barrierHealth,"Next hit deals damage after crystal is consumed");
            player.transform.position=beforePickupPosition;
            yield return new WaitForSeconds(.45f);
            statusVictim.Damage(10, Vector2.zero, player, CombatElement.Wood); statusVictim.Damage(10, Vector2.zero, player, CombatElement.Fire);
            float beforeBurn = statusVictim.Current; yield return new WaitForSeconds(2);
            Check(statusVictim.Current < beforeBurn, "Burning deals damage over time");
            statusVictim.Damage(10, Vector2.zero, player, CombatElement.Wood); statusVictim.Damage(10, Vector2.zero, player, CombatElement.Water);
            float beforeBloom = statusVictim.Current; yield return new WaitForSeconds(3.15f);
            Check(statusVictim.Current < beforeBloom, "Bloom detonates after a delay");
            var seedState=statusVictim.GetComponent<ReactionDamage>();
            foreach(var catalyst in new[]{CombatElement.Lightning,CombatElement.Fire}) {
                yield return new WaitForSeconds(.8f);
                statusVictim.GetComponent<ElementReaction>().Restore(0,0);
                statusVictim.Damage(10,Vector2.zero,player,CombatElement.Water);
                statusVictim.Damage(10,Vector2.zero,player,CombatElement.Wood);
                Check(seedState.BloomStage==1,"Water then wood creates a persistent seed: "+catalyst);
                statusVictim.Damage(10,Vector2.zero,player,catalyst);
                Check(seedState.BloomStage==(catalyst==CombatElement.Lightning?2:3),"Third element consumes seed exactly once: "+catalyst);
                Check(!seedState.TryCatalyze(catalyst,10,player),"Pending seed cannot be triggered again: "+catalyst);
                float hp=statusVictim.Current;yield return new WaitForSeconds(.4f);
                Check(Mathf.Abs(hp-statusVictim.Current-(catalyst==CombatElement.Lightning?22:15.5f))<.1f,"Chain reaction has bounded damage: "+catalyst);
                Check(seedState.BloomStage==0,"Seed clears after chain impact: "+catalyst);
            }
            seedState.Begin(10,player,true);seedState.Begin(999,player,true);
            Check(seedState.BloomDamage==10,"Seed stacking cannot overwrite damage or refresh the timer");
            var seedSnapshot=seedState.CaptureBloom();seedState.RestoreBloom(Vector3.zero,null);seedState.RestoreBloom(seedSnapshot,player);
            Check(seedState.BloomStage==1 && seedState.BloomOwner==player,"Seed state survives authority restoration");
            seedState.RestoreBloom(Vector3.zero,null);
            yield return new WaitForSeconds(.45f);
            statusVictim.Damage(10, Vector2.zero, player, CombatElement.Lightning); statusVictim.Damage(10, Vector2.zero, player, CombatElement.Ice);
            float beforePhysical = statusVictim.Current; statusVictim.Damage(20, Vector2.zero, player);
            Check(Mathf.Approximately(beforePhysical - statusVictim.Current, 25), "Superconduct increases subsequent physical damage by 25 percent");
            yield return new WaitForSeconds(.45f);
            var spreadVictim = Instantiate(prefab, statusVictim.transform.position + Vector3.up, Quaternion.identity).GetComponent<EnemyHealth>(); spreadVictim.GetComponent<EnemyAI>().enabled = false;
            float spreadHp=spreadVictim.Current;
            statusVictim.Damage(1, Vector2.zero, player, CombatElement.Water); statusVictim.Damage(1, Vector2.zero, player, CombatElement.Wind);
            var support=statusVictim.GetComponent<ElementReaction>();
            Check(spreadVictim.Current==spreadHp && support.Cooldown==0,"Swirl spreads aura without extra damage or normal reaction cooldown");
            Check(support.Resolve(10,CombatElement.Fire,player)==21,"Water wind fire immediately vaporizes without waiting for swirl");
            float normalWait=support.Cooldown;
            support.Resolve(1,CombatElement.Water,player);support.Resolve(1,CombatElement.Wind,player);
            Check(support.Cooldown==normalWait,"Wind does not extend an existing normal reaction cooldown");
            support.Restore((int)CombatElement.Water,4);
            Check(statusVictim.GetComponent<ElementReaction>().Aura==CombatElement.Water,"Swirl retains the original target aura even without nearby targets");
            statusVictim.Damage(1,Vector2.zero,player,CombatElement.Wind);
            Check(statusVictim.GetComponent<ElementReaction>().Aura==CombatElement.Water,"Wind during reaction cooldown does not overwrite retained water");
            Check(spreadVictim.GetComponent<ElementReaction>() && spreadVictim.GetComponent<ElementReaction>().Aura == CombatElement.Water, "Swirl spreads the original element without recursive reactions");
            Check(ElementReaction.Recipe(CombatElement.Wind, CombatElement.Wood) == 0 && ElementReaction.Recipe(CombatElement.Wind, CombatElement.Earth) == 0 && ElementReaction.Recipe(CombatElement.Wind, CombatElement.Light) == 0 && ElementReaction.Recipe(CombatElement.Wind, CombatElement.Dark) == 0, "Wood earth light and dark do not swirl");
            Destroy(spreadVictim.gameObject);
            Destroy(statusVictim.gameObject); player.ResetHealth();
            foreach(var starting in MageLoadout.Weapons) {
                var mageBuild=CharacterCatalog.CreateBuild("mage",starting);stats.Restore(mageBuild);
                Check(stats.StartingMageWeapon==starting && stats.Capture().weapons.Exists(w=>w.id==starting && w.level==2),"Mage starting element: "+starting);
                var roundtrip=JsonUtility.FromJson<PlayerDataData>(JsonUtility.ToJson(stats.Capture().NetworkCopy()));
                roundtrip.level=2;stats.Restore(roundtrip);stats.LoseRecentLevels(3);
                Check(stats.StartingMageWeapon==starting && stats.Capture().weapons.Exists(w=>w.id==starting && w.level==2),"Mage saved initial equipment survives death rollback: "+starting);
            }
            for(int kind=0;kind<4;kind++) {
                var a=BossAttackRoutes.Choose((BossKind)kind,2,0,1);
                var b=BossAttackRoutes.Choose((BossKind)kind,2,0,2);
                for(int part=0;part<4;part++)Check(BossAttackRoutes.Choose((BossKind)kind,2,0,part+1)!=BossAttackRoutes.Choose((BossKind)kind,3,0,part+1),"Third phase changes opening for each route: "+kind+"/"+part);
                Check(a!=b,"First destroyed part changes boss strategy: "+kind);
                Check(BossAttackRoutes.Choose((BossKind)kind,2,2,1|(2<<3))!=BossAttackRoutes.Choose((BossKind)kind,2,2,1|(3<<3)),"Later break order changes counterattack: "+kind);
            }
            var maximumBuild = stats.Capture().NetworkCopy(); maximumBuild.weapons.Clear(); maximumBuild.physicalFusions=new List<string>{"greatsword","cycloneaxe","knifegloves","flail"};
            foreach (var definition in WeaponCatalog.All) maximumBuild.weapons.Add(new WeaponSaveData(definition.Id, 8));
            Check(maximumBuild.IsValid() && JsonUtility.ToJson(maximumBuild).Length <= 1536, "Expanded weapon save fits split Fusion network storage");
            previousSave = File.Exists(BuildSaveService.PathName) ? File.ReadAllBytes(BuildSaveService.PathName) : null;
            previousBackup = File.Exists(BuildSaveService.PathName + ".bak") ? File.ReadAllBytes(BuildSaveService.PathName + ".bak") : null;
            saveTouched = true;
            int level = stats.Level;
            GameManager.Instance.soloDuration = GameManager.Instance.Elapsed + .1f;
            yield return new WaitForSeconds(.35f);
            Check(SceneManager.GetActiveScene().name == "LobbyScene" && BuildSaveService.Load()?.level == level, "Solo timer saves build to disk and transitions to LobbyScene");
            NetworkManager.Instance.Practice();
            yield return null; yield return null; yield return null;
            player = PlayerHealth.Local;
            Check(SceneManager.GetActiveScene().name == "MultiBossScene" && player && player.GetComponent<PlayerStats>().Level == level, "Boss scene restores the saved build");
            foreach (var weapon in player.GetComponents<WeaponBase>()) weapon.enabled = false;
            var boss = FindFirstObjectByType<BossAI>();
            Check(boss && boss.GetComponent<EnemyHealth>().IsBoss, "Boss prefab and AI spawn in arena");
            var armor = boss.GetComponent<BossParts>();
            var armoredHealth = boss.GetComponent<EnemyHealth>(); float coreBefore = armoredHealth.Current;
            Vector2 originalPosition = player.transform.position;
            player.GrantInvulnerability(60);
            var ping=player.GetComponent<PlayerPartPing>();
            Check(ping.SetPart(armor,2) && ping.VisiblePart(armor)==2,"Local part ping chooses a live armor plate");
            Check(!ping.SetPart(armor,7) && !ping.SetPart(armor,1),"Invalid and rapidly repeated pings are rejected");
            Check(ping.SetPart(armor,-1) && ping.VisiblePart(armor)==-1,"Player can clear their own ping");
            var opportunity=boss.GetComponent<BossBreakReward>();
            for (int part = 0; part < 4; part++) {
                float angle = part * Mathf.PI / 2;
                player.transform.position = boss.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 3;
                armoredHealth.Damage(armor.Maximum + 1, Vector2.zero, player);
                Check(armor.Health[part] == 0 && armoredHealth.Current == coreBefore, "Directional armor protects core and breaks part " + part);
                if(part==0) {
                    yield return new WaitForSeconds(.12f);
                    Check(boss.State==BossState.Stagger && boss.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude<.001f,"Armor break interrupts movement for a short stagger");
                    Check(!opportunity.Open,"Break reward cannot damage core during stagger");
                    yield return new WaitForSeconds(1.15f);
                    Check(boss.Phase==2 && boss.IsTransforming && armoredHealth.Current==coreBefore,"First broken part starts phase two while core is full");
                    Check(Mathf.Approximately(opportunity.Remaining,4),"Transformation preserves the full four-second reward");
                    yield return new WaitForSeconds(2.5f);
                    Check(opportunity.Open,"Weak point opens after first transformation");
                    var opportunityState=opportunity.Capture();bool bypass;
                    Vector2 center=boss.transform.position;
                    Check(opportunity.Resolve(10,center+Vector2.left*3,true,false,out bypass)==10 && !bypass,"Opposite side cannot bypass armor");
                    Check(opportunity.Resolve(10,center+Vector2.right*6,true,false,out bypass)==10 && !bypass,"Distant attacks cannot claim close weak point reward");
                    Check(Mathf.Approximately(opportunity.Resolve(10,center+Vector2.right*3,true,false,out bypass),16) && bypass,"Close physical hit earns 1.6x and bypasses remaining armor");
                    Check(Mathf.Approximately(opportunity.Resolve(10,center+Vector2.right*3,false,false,out bypass),13),"Close elemental hit earns 1.3x");
                    Check(Mathf.Approximately(opportunity.Resolve(10,center+Vector2.right*3,false,true,out bypass),11.5f),"Reaction follow-up has a smaller bounded bonus");
                    opportunity.Restore(opportunityState);
                    player.transform.position=center+Vector2.right*3;
                    armoredHealth.Damage(10,Vector2.zero,player);
                    Check(Mathf.Approximately(armoredHealth.Current,coreBefore-16),"Real combat path applies weak point damage while other armor remains");
                    armoredHealth.SetSynchronizedHealth(coreBefore,armoredHealth.maximum);
                    opportunity.Restore(opportunityState);
                    float capped=opportunity.Resolve(armoredHealth.maximum,center+Vector2.right*3,true,false,out bypass);
                    Check(Mathf.Approximately(capped,armoredHealth.maximum*.035f) && !opportunity.Open,"Early core damage is capped at 3.5 percent per break");
                    opportunity.Restore(opportunityState);opportunity.Begin(0);
                    Check(opportunity.Capture()==opportunityState,"Same broken part cannot refresh reward or budget");
                    opportunity.Tick(5);Check(!opportunity.Open,"Reward expires instead of leaving a permanent armor bypass");
                    opportunity.Restore(opportunityState);Check(opportunity.Open && opportunity.Part==0,"Reward snapshot restores remaining time and budget");
                    Check(!ping.SetPart(armor,0),"Destroyed armor cannot be pinged");
                }
            }
            opportunity.Clear();
            Check(armor.Exposed, "All four armor parts expose boss core");
            Check(armor.BreakOrder==(1|(2<<3)|(3<<6)|(4<<9)),"Armor records exact break order without losing earlier parts");
            Check(boss.Phase==2,"Breaking remaining parts retains phase two until half HP");
            armoredHealth.Damage(1, Vector2.zero, player);
            Check(armoredHealth.Current == coreBefore - 1, "Exposed boss core takes damage");
            player.transform.position = originalPosition;
            var reactionObject = new GameObject("Element verification");
            var reactions = reactionObject.AddComponent<ElementReaction>();
            Check(reactions.Resolve(10, CombatElement.Fire, player) == 10 && reactions.Resolve(10, CombatElement.Wind, player) == 10, "Fire plus wind swirls and retains fire");
            yield return new WaitForSeconds(.45f);
            reactions.Resolve(10, CombatElement.Fire, player);
            Check(reactions.Resolve(10, CombatElement.Lightning, player) == 20, "Fire plus lightning triggers overload");
            yield return new WaitForSeconds(.45f);
            reactions.Resolve(10, CombatElement.Ice, player);
            Check(reactions.Resolve(10, CombatElement.Fire, player) == 24, "Ice plus fire triggers melt in reverse order");
            Destroy(reactionObject);
            var firstElements=new[]{CombatElement.Fire,CombatElement.Ice,CombatElement.Water,CombatElement.Lightning};
            var nextElements=new[]{CombatElement.Water,CombatElement.Fire,CombatElement.Ice,CombatElement.Fire};
            var expectedDamage=new[]{21f,24f,10f,20f};
            for(int i=0;i<firstElements.Length;i++) {
                var testObject=new GameObject("Support reaction regression");
                var test=testObject.AddComponent<ElementReaction>();
                test.Restore((int)firstElements[i],2);
                for(int wind=0;wind<20;wind++)test.Resolve(10,CombatElement.Wind,player);
                Check(test.Aura==firstElements[i] && test.Remaining==2 && test.Cooldown==0,"Repeated wind preserves aura lifetime and normal reaction readiness: "+firstElements[i]);
                Check(test.Resolve(10,nextElements[i],player)==expectedDamage[i] && test.Aura==CombatElement.None,"Normal reaction follows repeated wind immediately: "+firstElements[i]);
                Destroy(testObject);
            }
            var barrierObject=new GameObject("Barrier reaction regression");
            var barrierReaction=barrierObject.AddComponent<ElementReaction>();
            barrierReaction.Resolve(1,CombatElement.Wind,player);
            Check(barrierReaction.Aura==CombatElement.None,"Wind does not leave a persistent wind aura");
            foreach(var unspreadable in new[]{CombatElement.Wood,CombatElement.Earth,CombatElement.Light,CombatElement.Dark}) {
                barrierReaction.Restore((int)unspreadable,2);barrierReaction.Resolve(1,CombatElement.Wind,player);
                Check(barrierReaction.Aura==unspreadable && barrierReaction.Remaining==2,"Wind preserves non-spreadable aura: "+unspreadable);
            }
            player.GrantBarrier();
            int crystalCount=FindObjectsByType<CrystalPickup>(FindObjectsSortMode.None).Length;
            barrierReaction.Restore((int)CombatElement.Water,2);
            barrierReaction.Resolve(10,CombatElement.Earth,player);
            Check(player.HasBarrier && barrierReaction.Aura==CombatElement.Water && barrierReaction.Remaining==2 && barrierReaction.Cooldown==0,"Protected player earth hit preserves aura timer and reaction readiness");
            Check(FindObjectsByType<CrystalPickup>(FindObjectsSortMode.None).Length==crystalCount,"Protected player does not create another crystal");
            Check(barrierReaction.Resolve(10,CombatElement.Fire,player)==21,"Suppressed crystallize allows immediate vaporize");
            yield return new WaitForSeconds(.45f);
            barrierReaction.Restore((int)CombatElement.Earth,2);barrierReaction.Resolve(10,CombatElement.Water,player);
            Check(barrierReaction.Aura==CombatElement.Water && barrierReaction.Cooldown==0,"Protected player replaces stored earth with incoming reactive aura without crystallize");
            player.SetRemoteBarrier(false);
            barrierReaction.Restore((int)CombatElement.Water,2);barrierReaction.Resolve(10,CombatElement.Earth,player,true);
            Check(barrierReaction.Aura==CombatElement.Water && barrierReaction.Cooldown==0,"Guest barrier snapshot suppresses crystallize before replicated shield arrives");
            barrierReaction.Resolve(10,CombatElement.Earth,player,false);
            Check(barrierReaction.Aura==CombatElement.None && barrierReaction.Cooldown>0,"Crystallize resumes when the attacking player has no barrier");
            barrierReaction.Restore((int)CombatElement.Fire,2);
            Check(!barrierReaction.ReceiveSpread(CombatElement.Water) && barrierReaction.Aura==CombatElement.Fire && barrierReaction.Remaining==2,"Spread does not overwrite a different pending aura");
            Destroy(barrierObject);

            boss.RestoreEncounter(0, 2, (int)BossAttack.Charge, Vector2.right*8, 1);
            boss.RestoreState((int)BossState.Windup, .05f, Vector2.right, 1);
            yield return new WaitForSeconds(.12f);
            Check(boss.State == BossState.Dash, "Boss telegraph transitions into dash");
            Check(boss.warning.localScale.x >= 8.6f && boss.warning.localScale.y >= 2.8f, "Dash telegraph covers travel distance and collision width after leg break");
            armoredHealth.Damage(armoredHealth.maximum*5, Vector2.zero, player);
            Check(Mathf.Approximately(armoredHealth.Current,armoredHealth.maximum*.5f), "Huge hit cannot skip boss transformation");
            yield return new WaitForSeconds(.12f);
            Check(boss.Phase==3 && boss.IsTransforming, "Half HP triggers third phase transformation");
            float transformedHealth=armoredHealth.Current;
            armoredHealth.Damage(100,Vector2.zero,player); armoredHealth.ReceiveSecondary(100,player);
            Check(armoredHealth.Current==transformedHealth, "Transformation blocks normal and reaction damage");
            yield return new WaitForSeconds(2.5f);
            Check(boss.PendingAttack==BossAttackRoutes.Choose(boss.Kind,3,0,armor.BreakOrder) && boss.State==BossState.Windup, "Third phase preserves the chosen break route");
            Check(GameObject.Find("East").transform.position.x==28 && GameObject.Find("North").transform.position.y==18, "Arena expands to 56 by 36 world units");
            player.GrantInvulnerability(50);
            for(int kind=0;kind<4;kind++) {
                Check(BossAppearance.GetSprite((BossKind)kind,false)!=BossAppearance.GetSprite((BossKind)kind,true), "Distinct before and after graphics: "+(BossKind)kind);
                Check(BossAppearance.GetSprite((BossKind)kind,2)!=BossAppearance.GetSprite((BossKind)kind,3),"Distinct third-phase graphics: "+(BossKind)kind);
                Check(BossCatalog.Attack((BossKind)kind,false,0)!=BossCatalog.Attack((BossKind)kind,true,0), "Different phase attack plans: "+(BossKind)kind);
                for(int phase=0;phase<3;phase++) {
                    boss.GetComponent<BossAttackDirector>().StopAllCoroutines();
                    foreach(var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None)) Destroy(bullet.gameObject);
                    foreach(var hazard in FindObjectsByType<BossHazard>(FindObjectsSortMode.None)) Destroy(hazard.gameObject);
                    foreach(var mechanic in FindObjectsByType<BossMechanic>(FindObjectsSortMode.None)) Destroy(mechanic.gameObject);
                    armoredHealth.SetSynchronizedHealth(armoredHealth.maximum,armoredHealth.maximum);
                    armor.Restore(phase==0?Vector4.one*armor.Maximum:phase==1?new Vector4(0,armor.Maximum,armor.Maximum,armor.Maximum):Vector4.zero,armor.Maximum);
                    if(phase==2) armoredHealth.SetSynchronizedHealth(armoredHealth.maximum*.45f,armoredHealth.maximum);
                    boss.RestoreEncounter(kind,phase+1,0,Vector2.zero,0);
                    boss.RestoreState((int)BossState.Pursue,.01f,Vector2.down,0);
                    yield return new WaitForSeconds(1.6f);
                    bool fired=boss.State==BossState.Dash || FindObjectsByType<BossHazard>(FindObjectsSortMode.None).Length>0 || FindObjectsByType<BossMechanic>(FindObjectsSortMode.None).Length>0 || FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length>0;
                    Check(fired, "Playable attack executed: "+(BossKind)kind+" phase "+(phase+1));
                }
            }
            boss.enabled=false; boss.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            boss.GetComponent<BossAttackDirector>().StopAllCoroutines();
            foreach(var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None)) Destroy(bullet.gameObject);
            foreach(var hazard in FindObjectsByType<BossHazard>(FindObjectsSortMode.None)) Destroy(hazard.gameObject);
                    foreach(var mechanic in FindObjectsByType<BossMechanic>(FindObjectsSortMode.None)) Destroy(mechanic.gameObject);
            yield return null;
            var testHazard=BossHazard.Create(boss,new Vector2(20,10),new Vector2(20,10),2,1,1,10,Color.red);
            Check(!testHazard.Active && testHazard.Contains(new Vector2(20,10)) && !testHazard.Contains(Vector2.zero), "Hazard warning has no early damage and uses bounded hit area");
            Destroy(testHazard.gameObject);
            player.transform.position=new Vector3(-20,-12,0); player.ResetHealth();
            player.GetComponent<PlayerPassiveAbilities>().enabled=false;
            player.Damage(player.Maximum*.7f/(1-player.GetComponent<PlayerStats>().DamageReduction));
            var recovery=player.GetComponent<BossRecovery>(); float wounded=player.Current;
            yield return new WaitForSeconds(1);
            Check(player.Current==wounded && recovery.Charges==3, "Emergency heal waits after damage");
            yield return new WaitForSeconds(5.2f);
            Check(recovery.Charges==2 && Mathf.Approximately(player.Current,wounded+player.Maximum*.18f), "Emergency heal restores 18 percent after six safe seconds");
            player.ResetHealth();
            Check(recovery.Charges==2, "Emergency heal charges do not reset on revival health reset");
            stats=player.GetComponent<PlayerStats>(); var previousBuild=stats.Capture();
            var regenBuild=CharacterCatalog.CreateBuild("ranger"); regenBuild.level=4; stats.Restore(regenBuild);
            stats.RecordUpgrade(UpgradeKind.Regeneration); new UpgradeOption(UpgradeKind.Regeneration,"","").Apply(stats);
            Check(Mathf.Approximately(stats.Regeneration,.35f), "Regeneration upgrade grants 0.35 HP per second");
            stats.LoseRecentLevels(1); Check(stats.Regeneration==0, "Death rollback removes regeneration upgrade");
            for(int i=0;i<10;i++) stats.UpgradeRegeneration(); Check(stats.Regeneration==2,"Regeneration caps at two HP per second");
            stats.Restore(previousBuild);
            boss.GetComponent<EnemyHealth>().ApplyDamage(boss.GetComponent<EnemyHealth>().maximum + 1, Vector2.zero);
            yield return null;
            Check(GameManager.Instance.Won && !GameManager.Instance.IsPlaying && GameObject.Find("Result"), "Boss defeat displays victory result");
            GameManager.Instance.Retry();
            yield return null; yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == "SoloScene" && PlayerHealth.Local.GetComponent<PlayerStats>().Level == 1 && Time.timeScale == 1, "Retry starts a fresh level one Solo run");
            GameManager.Instance.soloDuration = 180;
            player = PlayerHealth.Local;
            var deathBuild = CharacterCatalog.CreateBuild("ranger"); deathBuild.level = 4; player.GetComponent<PlayerStats>().Restore(deathBuild);
            PlayerHealth.Local.Damage(100000);
            yield return null;
            Check(GameManager.Instance.IsPlaying && !player.Alive && player.GetComponent<PlayerStats>().Level == 1, "Death removes three levels while the run continues");
            yield return new WaitForSeconds(3.2f);
            Check(player.Alive && Mathf.Approximately(player.Current, player.Maximum), "Player revives with full health after three seconds");
#if ROGUE_FUSION
            Time.timeScale = 1;
            var connect = NetworkManager.Instance.JoinAsync("local-smoke", true);
            float timeout = Time.realtimeSinceStartup + 30;
            while (!connect.IsCompleted && Time.realtimeSinceStartup < timeout) yield return null;
            Check(connect.IsCompleted && connect.Result, "Fusion Single mode starts a local simulation");
            NetworkManager.Instance.StartArena();
            timeout = Time.realtimeSinceStartup + 20;
            while ((SceneManager.GetActiveScene().name != "MultiBossScene" || !PlayerHealth.Local) && Time.realtimeSinceStartup < timeout) yield return null;
            yield return null; yield return null; yield return null;
            player = PlayerHealth.Local; boss = FindFirstObjectByType<BossAI>();
            Check(player && player.GetComponent<Fusion.NetworkObject>().HasStateAuthority && boss && boss.GetComponent<NetworkEnemySync>().IsNetworked,
                "Fusion spawns player and master-owned boss from registered prefabs");
            foreach (var weapon in player.GetComponents<WeaponBase>()) weapon.enabled = false;
            boss.enabled = false;
            var bossHealth = boss.GetComponent<EnemyHealth>(); float before = bossHealth.Current;
            bossHealth.Damage(20, Vector2.zero, player);
            yield return new WaitForSeconds(.1f);
            Check(Mathf.Approximately(bossHealth.Current, before) && boss.GetComponent<BossParts>().Health != Vector4.one * boss.GetComponent<BossParts>().Maximum, "Fusion damage RPC damages armor while protecting core");
            int bulletsBefore = FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length;
            boss.GetComponent<NetworkEnemySync>().BroadcastVolley(0, 14, 3, 14);
            yield return null;
            Check(FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length >= bulletsBefore + 14, "Fusion volley RPC creates hostile projectiles");
            bossHealth.ApplyDamage(bossHealth.maximum + 1, Vector2.zero);
            yield return null;
            Check(GameManager.Instance.Won, "Fusion replicated result completes the battle");
            var leave = NetworkManager.Instance.LeaveAsync("HomeScene");
            while (!leave.IsCompleted) yield return null;
            yield return null; yield return null;
            Check(FindFirstObjectByType<HomeUI>() && GameObject.Find("出撃"), "Japanese home screen returns after Fusion shutdown");
#endif
        }
        void RestoreSave()
        {
            if (!saveTouched) return;
            if (previousSave != null) File.WriteAllBytes(BuildSaveService.PathName, previousSave);
            else if (File.Exists(BuildSaveService.PathName)) File.Delete(BuildSaveService.PathName);
            if (previousBackup != null) File.WriteAllBytes(BuildSaveService.PathName + ".bak", previousBackup);
            else if (File.Exists(BuildSaveService.PathName + ".bak")) File.Delete(BuildSaveService.PathName + ".bak");
            saveTouched = false;
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Log; RestoreSave();
            if (previousInputSettings)
            {
                var temporary = InputSystem.settings; InputSystem.settings = previousInputSettings;
                Destroy(temporary);
            }
        }
    }
}
#endif
