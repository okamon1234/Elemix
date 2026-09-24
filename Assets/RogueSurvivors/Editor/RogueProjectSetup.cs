using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ROGUE_FUSION
using Fusion;
using Fusion.Photon.Realtime;
#endif
namespace RogueSurvivors.Editor
{
    public static class RogueProjectSetup
    {
        const string Root = "Assets/RogueSurvivors";
        const string Res = Root + "/Resources/RogueSurvivors";
        static Material spriteMaterial;
        static Sprite diamond, circle, square, grid;
        static RuntimeAnimatorController controller;
        [MenuItem("Tools/Rogue Survivors/Setup")]
        public static void Setup()
        {
            if (!Application.isBatchMode && Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save your current scene before running Rogue Survivors Setup. No scenes were changed.");
            Directory.CreateDirectory(Res); Directory.CreateDirectory(Root + "/Art");
            Directory.CreateDirectory(Root + "/Audio"); Directory.CreateDirectory(Root + "/Scenes");
            ConfigureTagsAndLayers();
            PlayerSettings.runInBackground = true;
            AssetDatabase.Refresh();
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Res + "/Player.prefab"))
            {
                MakeArt(); MakeAnimator();
                var orb = MakeOrb(); var item = MakeItem();
                var bullet = MakeBullet("PlayerBullet", new Color(.3f, 1, .95f), false);
                var hostile = MakeBullet("EnemyBullet", new Color(1, .32f, .48f), true);
                var blade = MakeBlade();
                var player = MakePlayer(bullet, blade);
                var enemy = MakeEnemy(orb, item);
                var boss = MakeBoss(hostile);
                var effects = MakeEffects();
                MakeScene("SoloScene", RunMode.Solo, player, enemy, boss, effects);
                MakeScene("LobbyScene", RunMode.Lobby, player, enemy, boss, effects);
                MakeScene("MultiBossScene", RunMode.Boss, player, enemy, boss, effects);
            }
            MakeNetworkPrefabs();
            UpgradeJapaneseEdition();
            PixelArtSetup.Apply();
            EnemyVariantSetup.Apply();
            WeaponPrefabSetup.Apply();
#if ROGUE_FUSION
            Fusion.Editor.FusionGlobalScriptableObjectUtils.EnsureAssetExists<PhotonAppSettings>();
            Fusion.Editor.FusionGlobalScriptableObjectUtils.EnsureAssetExists<NetworkProjectConfigAsset>();
            PhotonAppSettings.Global.AppSettings.AppIdFusion = NetworkManager.FusionAppId;
            EditorUtility.SetDirty(PhotonAppSettings.Global);
            Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable();
#endif
            var scenePaths = new[] { "HomeScene", "SoloScene", "LobbyScene", "MultiBossScene" }.Select(n => Root + "/Scenes/" + n + ".unity").ToArray();
            var scenes = scenePaths.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            scenes.AddRange(EditorBuildSettings.scenes.Where(s => !scenePaths.Contains(s.path)));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(Root + "/Scenes/HomeScene.unity");
            Validate();
            Debug.Log("ROGUE_SETUP_OK: SoloScene, LobbyScene and MultiBossScene are ready.");
        }
        static void ConfigureTagsAndLayers()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var settings = new SerializedObject(asset);
            var tags = settings.FindProperty("tags");
            foreach (string name in new[] { "Player", "Enemy", "ExpOrb", "Item", "Bullet", "Boss" })
            {
                if (name == "Player" || Enumerable.Range(0, tags.arraySize).Any(i => tags.GetArrayElementAtIndex(i).stringValue == name)) continue;
                tags.InsertArrayElementAtIndex(tags.arraySize); tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = name;
            }
            var layers = settings.FindProperty("layers");
            string[] names = { "Player", "Enemy", "Boss", "PlayerBullet", "EnemyBullet", "Pickup", "Collector", "World" };
            foreach (string name in names)
            {
                if (Enumerable.Range(0, layers.arraySize).Any(i => layers.GetArrayElementAtIndex(i).stringValue == name)) continue;
                int free = Enumerable.Range(8, 24).FirstOrDefault(i => string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
                if (free < 8) throw new InvalidOperationException("No free layer available for " + name);
                layers.GetArrayElementAtIndex(free).stringValue = name;
            }
            var sorting = settings.FindProperty("m_SortingLayers");
            string[] orders = { "Background", "Items", "Characters", "Projectiles", "UI" };
            for (int n = 0; n < orders.Length; n++)
            {
                string name = orders[n];
                if (Enumerable.Range(0, sorting.arraySize).Any(i => sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)) continue;
                int index = name == "Background" ? 0 : sorting.arraySize;
                sorting.InsertArrayElementAtIndex(index);
                var entry = sorting.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("name").stringValue = name;
                entry.FindPropertyRelative("uniqueID").intValue = 18270001 + n;
                entry.FindPropertyRelative("locked").boolValue = false;
            }
            settings.ApplyModifiedProperties();
            foreach (string a in names)
                foreach (string b in names) Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(a), LayerMask.NameToLayer(b), true);
            foreach (string pair in new[] { "Player:Enemy", "Player:Boss", "Player:EnemyBullet", "Player:World", "Enemy:PlayerBullet", "Boss:PlayerBullet", "Collector:Pickup", "Enemy:World", "Boss:World", "PlayerBullet:World", "EnemyBullet:World" })
            {
                var split = pair.Split(':'); Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(split[0]), LayerMask.NameToLayer(split[1]), false);
            }
        }
        static void MakeArt()
        {
            diamond = CreateSprite("Diamond", (x, y) =>
            {
                float d = Mathf.Abs(x) + Mathf.Abs(y);
                if (d > .92f) return Color.clear;
                return d > .68f ? Color.white : new Color(.45f, .62f, .72f, 1);
            });
            circle = CreateSprite("Circle", (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                if (d > .92f) return Color.clear;
                return d > .68f ? Color.white : new Color(.42f, .48f, .61f, 1);
            });
            square = CreateSprite("Square", (x, y) => Color.white);
            grid = CreateSprite("Grid", (x, y) => Mathf.Abs(x) > .97f || Mathf.Abs(y) > .97f ? new Color(.09f, .13f, .20f) : new Color(.035f, .05f, .085f));
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (!shader) throw new InvalidOperationException("URP 2D unlit sprite shader missing.");
            spriteMaterial = new Material(shader);
            AssetDatabase.CreateAsset(spriteMaterial, Root + "/Art/NeonSprite.mat");
        }
        static Sprite CreateSprite(string name, Func<float, float, Color> pixel)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) texture.SetPixel(x, y, pixel((x + .5f) / 32 - 1, (y + .5f) / 32 - 1));
            texture.Apply(); string path = Root + "/Art/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 64;
            importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(textureSettings);
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static SpriteRenderer Visual(GameObject go, Sprite sprite, Color color, string sorting = "Characters", bool child = false)
        {
            var target = child ? new GameObject("Visual") : go;
            if (child) target.transform.SetParent(go.transform, false);
            var renderer = target.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            renderer.sharedMaterial = spriteMaterial; renderer.sortingLayerName = sorting; return renderer;
        }
        static GameObject RootObject(string name, string tag, string layer)
        {
            var go = new GameObject(name); go.tag = tag; go.layer = LayerMask.NameToLayer(layer); return go;
        }
        static Rigidbody2D Body(GameObject go, float radius, bool trigger = false)
        {
            var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = go.AddComponent<CircleCollider2D>(); collider.radius = radius; collider.isTrigger = trigger;
            return body;
        }
        static GameObject Save(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Res + "/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go); return prefab;
        }
        static ExpOrb MakeOrb()
        {
            var go = RootObject("ExpOrb", "ExpOrb", "Pickup");
            var renderer = Visual(go, diamond, new Color(.2f, 1, .85f), "Items"); renderer.transform.localScale = Vector3.one * .28f;
            go.AddComponent<CircleCollider2D>().isTrigger = true; go.AddComponent<ExpOrb>();
            return Save(go, "ExpOrb").GetComponent<ExpOrb>();
        }
        static DropItem MakeItem()
        {
            var go = RootObject("DropItem", "Item", "Pickup"); Visual(go, square, Color.white, "Items"); go.transform.localScale = Vector3.one * .38f;
            go.AddComponent<CircleCollider2D>().isTrigger = true; go.AddComponent<DropItem>(); return Save(go, "DropItem").GetComponent<DropItem>();
        }
        static Bullet MakeBullet(string name, Color color, bool hostile)
        {
            var go = RootObject(name, "Bullet", name); Body(go, hostile ? .18f : .12f, true);
            var renderer = Visual(go, hostile ? circle : diamond, color, "Projectiles", true);
            renderer.transform.localScale = hostile ? Vector3.one * .38f : new Vector3(.5f, .20f, 1);
            go.AddComponent<Bullet>(); return Save(go, name).GetComponent<Bullet>();
        }
        static GameObject MakeBlade()
        {
            var go = new GameObject("OrbitBlade"); Visual(go, diamond, new Color(.9f, .8f, .3f), "Projectiles");
            go.transform.localScale = new Vector3(.8f, .36f, 1); return Save(go, "OrbitBlade");
        }
        static GameObject MakePlayer(Bullet bullet, GameObject blade)
        {
            var go = RootObject("Player", "Player", "Player"); var body = Body(go, .32f); body.mass = 10;
            var visual = Visual(go, diamond, new Color(.25f, .95f, 1), "Characters", true);
            visual.gameObject.AddComponent<Animator>().runtimeAnimatorController = controller;
            go.AddComponent<PlayerStats>(); go.AddComponent<PlayerHealth>(); go.AddComponent<PlayerMovement>(); go.AddComponent<AutoTargeting>();
            go.AddComponent<HitFeedback>();
            var bolt = go.AddComponent<ProjectileWeapon>(); bolt.bulletPrefab = bullet; bolt.SetLevel(1);
            var orbit = go.AddComponent<OrbitWeapon>(); orbit.bladePrefab = blade;
            var collector = RootObject("Collector", "Untagged", "Collector"); collector.transform.SetParent(go.transform, false);
            var area = collector.AddComponent<CircleCollider2D>(); area.radius = 2.4f; area.isTrigger = true;
            collector.AddComponent<PlayerCollector>();
            return Save(go, "Player");
        }
        static EnemyHealth MakeEnemy(ExpOrb orb, DropItem item)
        {
            var go = RootObject("Enemy", "Enemy", "Enemy"); Body(go, .35f);
            Visual(go, circle, new Color(.78f, .4f, 1), "Characters", true).transform.localScale = Vector3.one * .8f;
            var health = go.AddComponent<EnemyHealth>(); health.orbPrefab = orb; health.itemPrefab = item;
            go.AddComponent<EnemyAI>(); go.AddComponent<HitFeedback>();
            return Save(go, "Enemy").GetComponent<EnemyHealth>();
        }
        static GameObject MakeBoss(Bullet bullet)
        {
            var go = RootObject("RiftWarden", "Boss", "Boss"); var body = Body(go, 1.1f); body.mass = 100;
            Visual(go, circle, new Color(1, .28f, .48f), "Characters", true).transform.localScale = Vector3.one * 2.6f;
            var health = go.AddComponent<EnemyHealth>(); health.maximum = 2400; health.IsBoss = true;
            go.AddComponent<HitFeedback>(); go.AddComponent<NetworkEnemySync>();
            var ai = go.AddComponent<BossAI>(); ai.hostileBullet = bullet;
            var warning = new GameObject("Dash Telegraph"); warning.transform.SetParent(go.transform, false);
            Visual(warning, square, new Color(1, .25f, .38f, .3f), "Items");
            warning.transform.localScale = new Vector3(8, 1.8f, 1); ai.warning = warning.transform; warning.SetActive(false);
            return Save(go, "Boss");
        }
        static void MakeAnimator()
        {
            var idle = new AnimationClip { name = "Idle" };
            idle.SetCurve("", typeof(Transform), "localScale.y", AnimationCurve.Constant(0, 1, 1));
            idle.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Constant(0, 1, 1));
            AssetDatabase.CreateAsset(idle, Root + "/Art/Idle.anim");
            var walk = new AnimationClip { name = "Walk" };
            walk.SetCurve("", typeof(Transform), "localScale.y", new AnimationCurve(new Keyframe(0, 1), new Keyframe(.13f, 1.12f), new Keyframe(.26f, 1)));
            walk.SetCurve("", typeof(Transform), "localScale.x", new AnimationCurve(new Keyframe(0, 1), new Keyframe(.13f, .88f), new Keyframe(.26f, 1)));
            var settings = AnimationUtility.GetAnimationClipSettings(walk); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(walk, settings);
            AssetDatabase.CreateAsset(walk, Root + "/Art/Walk.anim");
            var animator = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Art/Player.controller");
            animator.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            var machine = animator.layers[0].stateMachine; var a = machine.AddState("Idle"); a.motion = idle;
            var b = machine.AddState("Walk"); b.motion = walk; machine.defaultState = a;
            var toWalk = a.AddTransition(b); toWalk.hasExitTime = false; toWalk.duration = .07f; toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            var toIdle = b.AddTransition(a); toIdle.hasExitTime = false; toIdle.duration = .07f; toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            controller = animator;
        }
        static EffectsService MakeEffects()
        {
            var burstGO = new GameObject("DeathBurst"); var particles = burstGO.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.loop = false; main.duration = .4f; main.startLifetime = .45f; main.startSpeed = 3;
            main.startSize = .13f; main.maxParticles = 32; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .1f;
            var render = particles.GetComponent<ParticleSystemRenderer>();
            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var particleMaterial = new Material(particleShader ? particleShader : spriteMaterial.shader);
            if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(particleMaterial, Root + "/Art/Particles.mat");
            render.sharedMaterial = particleMaterial; render.sortingLayerName = "Projectiles";
            var burst = Save(burstGO, "DeathBurst").GetComponent<ParticleSystem>();
            var go = new GameObject("EffectsService"); var fx = go.AddComponent<EffectsService>(); fx.burstPrefab = burst;
            fx.shoot = MakeTone("Shoot", 640, .07f); fx.pickup = MakeTone("Pickup", 1050, .08f);
            fx.hurt = MakeTone("Hurt", 120, .16f); fx.level = MakeTone("Level", 820, .35f);
            return Save(go, "EffectsService").GetComponent<EffectsService>();
        }
        static AudioClip MakeTone(string name, float frequency, float length)
        {
            string path = Root + "/Audio/" + name + ".wav"; int count = (int)(22050 * length);
            using (var stream = File.Create(path)) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(22050); writer.Write(44100); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / 22050, envelope = Mathf.Pow(1 - (float)i / count, 2);
                    float wave = Mathf.Sin(2 * Mathf.PI * (frequency * t + frequency * .3f * t * t / length));
                    writer.Write((short)(wave * envelope * 11000));
                }
            }
            AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static void MakeScene(string name, RunMode mode, GameObject player, EnemyHealth enemy, GameObject boss, EffectsService effects)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow)); cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0, 0, -10);
            var camera = cameraGO.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = mode == RunMode.Boss ? 10 : 8;
            camera.backgroundColor = new Color(.025f, .04f, .075f); camera.clearFlags = CameraClearFlags.SolidColor;
            var floor = new GameObject("Grid Floor"); var renderer = Visual(floor, grid, Color.white, "Background");
            renderer.drawMode = SpriteDrawMode.Tiled; renderer.size = new Vector2(60, 40);
            var follow = cameraGO.GetComponent<CameraFollow>(); follow.grid = floor.transform; follow.arena = mode != RunMode.Solo;
            if (mode == RunMode.Boss)
            {
                Wall("North", new Vector2(0, 8.6f), new Vector2(33, .4f)); Wall("South", new Vector2(0, -8.6f), new Vector2(33, .4f));
                Wall("West", new Vector2(-16, 0), new Vector2(.4f, 17)); Wall("East", new Vector2(16, 0), new Vector2(.4f, 17));
            }
            var root = new GameObject("SceneBootstrap"); var setup = root.AddComponent<SceneBootstrap>(); setup.mode = mode;
            setup.playerPrefab = player; setup.enemyPrefab = enemy; setup.bossPrefab = boss; setup.effectsPrefab = effects;
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/" + name + ".unity");
        }
        static void Wall(string name, Vector2 position, Vector2 size)
        {
            var go = RootObject(name, "Untagged", "World"); go.transform.position = position;
            Visual(go, square, new Color(.16f, .4f, .5f), "Background"); go.transform.localScale = new Vector3(size.x, size.y, 1);
            go.AddComponent<BoxCollider2D>();
        }
        static void MakeNetworkPrefabs()
        {
#if ROGUE_FUSION
            foreach (string kind in new[] { "Player", "Boss" })
            {
                string path = Res + "/Fusion" + kind + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) continue;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Res + "/" + kind + ".prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                if (kind == "Player") go.AddComponent<NetworkPlayerSync>();
                go.AddComponent<NetworkTransform>();
                var networkObject = go.AddComponent<NetworkObject>();
                networkObject.IsSpawnable = true;
                networkObject.NetworkedBehaviours = go.GetComponents<NetworkBehaviour>();
                networkObject.NestedObjects = Array.Empty<NetworkObject>();
                if (kind == "Boss")
                {
                    networkObject.Flags |= NetworkObjectFlags.MasterClientObject;
                    networkObject.Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
                }
                new Fusion.Editor.NetworkObjectBakerEditTime().Bake(go);
                PrefabUtility.SaveAsPrefabAsset(go, path); UnityEngine.Object.DestroyImmediate(go);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
#endif
        }
        static void UpgradeJapaneseEdition()
        {
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/NeonSprite.mat");
            grid = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Grid.png");
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Square.png");
            foreach (string name in new[] { "Player", "FusionPlayer" })
            {
                string path = Res + "/" + name + ".prefab";
                if (!File.Exists(path)) continue;
                var instance = PrefabUtility.LoadPrefabContents(path);
                if (!instance.GetComponent<PlayerPassiveAbilities>()) instance.AddComponent<PlayerPassiveAbilities>();
                if (!instance.GetComponent<PlayerRespawn>()) instance.AddComponent<PlayerRespawn>();
                if (!instance.GetComponent<FireballWeapon>()) instance.AddComponent<FireballWeapon>();
                if (!instance.GetComponent<LightningWeapon>()) instance.AddComponent<LightningWeapon>();
                if (!instance.GetComponent<SpearWeapon>()) instance.AddComponent<SpearWeapon>();
                PrefabUtility.SaveAsPrefabAsset(instance, path); PrefabUtility.UnloadPrefabContents(instance);
            }
            foreach (string name in new[] { "Boss", "FusionBoss" }) {
                string path = Res + "/" + name + ".prefab";
                if (!File.Exists(path)) continue;
                var instance = PrefabUtility.LoadPrefabContents(path);
                if (!instance.GetComponent<BossDifficulty>()) instance.AddComponent<BossDifficulty>();
                PrefabUtility.SaveAsPrefabAsset(instance, path); PrefabUtility.UnloadPrefabContents(instance);
            }
            if (!File.Exists(Res + "/RuinPillar.prefab"))
            {
                var stone = CreateSprite("RuinStone", (x, y) => {
                    if (Mathf.Abs(x) > .87f || Mathf.Abs(y) > .92f) return Color.clear;
                    float horizontal = Mathf.Repeat((y + 1) * 3, 1);
                    bool seam = horizontal < .07f || Mathf.Abs(x + (((int)((y + 1) * 3) % 2 == 0) ? .25f : -.25f)) < .025f;
                    if (seam) return new Color(.10f, .13f, .17f);
                    return x + y > .1f ? new Color(.39f, .44f, .50f) : new Color(.26f, .31f, .38f);
                });
                var obstacle = RootObject("崩れた石柱", "Untagged", "World");
                Visual(obstacle, stone, Color.white, "Characters"); obstacle.transform.localScale = new Vector3(2.3f, 2.6f, 1);
                var collider = obstacle.AddComponent<BoxCollider2D>(); collider.size = new Vector2(1.6f, 1.7f) / 2.3f;
                Save(obstacle, "RuinPillar");
            }
            if (!File.Exists(Root + "/Scenes/HomeScene.unity"))
                MakeScene("HomeScene", RunMode.Home,
                    AssetDatabase.LoadAssetAtPath<GameObject>(Res + "/Player.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>(Res + "/Enemy.prefab").GetComponent<EnemyHealth>(),
                    AssetDatabase.LoadAssetAtPath<GameObject>(Res + "/Boss.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>(Res + "/EffectsService.prefab").GetComponent<EffectsService>());
        }
        [MenuItem("Tools/Rogue Survivors/Validate")]
        public static void Validate()
        {
            var errors = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Res }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var child in go.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0) errors.Add(path + ": missing script");
            }
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(Res + "/Player.prefab");
            if (!player || !player.GetComponent<ProjectileWeapon>().bulletPrefab || !player.GetComponent<OrbitWeapon>().bladePrefab) errors.Add("Player weapon references missing.");
            if (Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("Collector"), LayerMask.NameToLayer("Pickup"))) errors.Add("Collector cannot collide with pickups.");
            if (!Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("PlayerBullet"), LayerMask.NameToLayer("Player"))) errors.Add("Friendly bullet layer collision enabled.");
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log("ROGUE_VALIDATION_OK: prefab scripts, weapon references and collision layers checked.");
        }
    }
}

