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
        IEnumerator Tests()
        {
            var player = PlayerHealth.Local;
            Check(player && HUDController.Instance && GameManager.Instance.IsPlaying, "SoloScene bootstraps player, HUD and run state");
            var stats = player.GetComponent<PlayerStats>();
            foreach (var character in CharacterCatalog.All) {
                var build = CharacterCatalog.CreateBuild(character.Id); stats.Restore(build);
                Check(build.IsValid() && stats.CharacterId == character.Id && Mathf.Approximately(stats.MaxHealth, build.maxHealth), "Character starting build: " + character.Id);
            }
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
            Check(JsonUtility.ToJson(stats.Capture().NetworkCopy()).Length <= 1024, "Build data fits Fusion network string capacity");
            var savedRoster = stats.Capture();
            int physicalCount = 0, elementalCount = 0;
            foreach (var definition in WeaponCatalog.All) { if (WeaponCatalog.IsPhysical(definition.Id)) physicalCount++; else elementalCount++; }
            Check(physicalCount == 9 && elementalCount == 9, "Balanced roster: nine physical and nine elemental weapons");
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
                Check(PhysicalEvolution.Name(primary) == recipe.Name && PhysicalEvolution.Power(primary) == 1.5f, "Physical combination evolves: " + recipe.Name);
                partner.SetLevel(3); Check(!PhysicalEvolution.Active(primary), "Losing ingredient level removes evolution: " + recipe.Name);
            }
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
            float beforeBloom = statusVictim.Current; yield return new WaitForSeconds(.9f);
            Check(statusVictim.Current < beforeBloom, "Bloom detonates after a delay");
            yield return new WaitForSeconds(.45f);
            statusVictim.Damage(10, Vector2.zero, player, CombatElement.Lightning); statusVictim.Damage(10, Vector2.zero, player, CombatElement.Ice);
            float beforePhysical = statusVictim.Current; statusVictim.Damage(20, Vector2.zero, player);
            Check(Mathf.Approximately(beforePhysical - statusVictim.Current, 25), "Superconduct increases subsequent physical damage by 25 percent");
            yield return new WaitForSeconds(.45f);
            var spreadVictim = Instantiate(prefab, statusVictim.transform.position + Vector3.up, Quaternion.identity).GetComponent<EnemyHealth>(); spreadVictim.GetComponent<EnemyAI>().enabled = false;
            statusVictim.Damage(1, Vector2.zero, player, CombatElement.Water); statusVictim.Damage(1, Vector2.zero, player, CombatElement.Wind);
            Check(statusVictim.GetComponent<ElementReaction>().Aura==CombatElement.Water,"Swirl retains the original target aura even without nearby targets");
            statusVictim.Damage(1,Vector2.zero,player,CombatElement.Wind);
            Check(statusVictim.GetComponent<ElementReaction>().Aura==CombatElement.Water,"Wind during reaction cooldown does not overwrite retained water");
            Check(spreadVictim.GetComponent<ElementReaction>() && spreadVictim.GetComponent<ElementReaction>().Aura == CombatElement.Water, "Swirl spreads the original element without recursive reactions");
            Check(ElementReaction.Recipe(CombatElement.Wind, CombatElement.Wood) == 0 && ElementReaction.Recipe(CombatElement.Wind, CombatElement.Earth) == 0 && ElementReaction.Recipe(CombatElement.Wind, CombatElement.Light) == 0 && ElementReaction.Recipe(CombatElement.Wind, CombatElement.Dark) == 0, "Wood earth light and dark do not swirl");
            Destroy(spreadVictim.gameObject);
            Destroy(statusVictim.gameObject); player.ResetHealth();
            var maximumBuild = stats.Capture().NetworkCopy(); maximumBuild.weapons.Clear();
            foreach (var definition in WeaponCatalog.All) maximumBuild.weapons.Add(new WeaponSaveData(definition.Id, 8));
            Check(maximumBuild.IsValid() && JsonUtility.ToJson(maximumBuild).Length <= 1024, "Expanded weapon save fits split Fusion network storage");
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
            for (int part = 0; part < 4; part++) {
                float angle = part * Mathf.PI / 2;
                player.transform.position = boss.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 3;
                armoredHealth.Damage(armor.Maximum + 1, Vector2.zero, player);
                Check(armor.Health[part] == 0 && armoredHealth.Current == coreBefore, "Directional armor protects core and breaks part " + part);
                if(part==0) {
                    yield return new WaitForSeconds(.12f);
                    Check(boss.Phase==2 && boss.IsTransforming && armoredHealth.Current==coreBefore,"First broken part starts phase two while core is full");
                    yield return new WaitForSeconds(2.5f);
                }
            }
            Check(armor.Exposed, "All four armor parts expose boss core");
            Check(boss.Phase==2,"Breaking remaining parts retains phase two until half HP");
            armoredHealth.Damage(1, Vector2.zero, player);
            Check(armoredHealth.Current == coreBefore - 1, "Exposed boss core takes damage");
            player.transform.position = originalPosition;
            var reactionObject = new GameObject("Element verification");
            var reactions = reactionObject.AddComponent<ElementReaction>();
            Check(reactions.Resolve(10, CombatElement.Fire, player) == 10 && reactions.Resolve(10, CombatElement.Wind, player) == 13, "Fire plus wind swirls and retains fire");
            yield return new WaitForSeconds(.45f);
            reactions.Resolve(10, CombatElement.Fire, player);
            Check(reactions.Resolve(10, CombatElement.Lightning, player) == 20, "Fire plus lightning triggers overload");
            yield return new WaitForSeconds(.45f);
            reactions.Resolve(10, CombatElement.Ice, player);
            Check(reactions.Resolve(10, CombatElement.Fire, player) == 24, "Ice plus fire triggers melt in reverse order");
            Destroy(reactionObject);
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
            Check(boss.PendingAttack==BossAttack.SeismicRing && boss.State==BossState.Windup, "Third phase starts the moving seismic ring pattern");
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

