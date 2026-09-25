#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    // Opt-in, offscreen review of real scenes. Never runs in ordinary gameplay.
    public sealed class BossPreviewCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"--rogue-boss-gallery");
            if(index<0 || index+1>=args.Length) return;
            var go=new GameObject("ボス描画検証"); DontDestroyOnLoad(go);
            go.AddComponent<BossPreviewCapture>().StartCoroutine(Capture(args[index+1]));
        }
        static IEnumerator Capture(string folder)
        {
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);
            SaveFrame(Path.Combine(folder,"ホーム.png"));
            FindFirstObjectByType<BossGuideUI>().Open(); yield return new WaitForSecondsRealtime(.2f);
            SaveFrame(Path.Combine(folder,"ボス攻略.png"));
            FindFirstObjectByType<BuildGuideUI>().Open(); yield return new WaitForSecondsRealtime(.2f);
            SaveFrame(Path.Combine(folder,"合体と属性ガイド.png"));
            SceneManager.LoadScene("MultiBossScene");
            yield return null; yield return null; yield return null;
            var player=PlayerHealth.Local; var boss=FindFirstObjectByType<BossAI>();
            player.GrantInvulnerability(100); player.GetComponent<PlayerMovement>().enabled=false;
            foreach(var weapon in player.GetComponents<WeaponBase>()) weapon.enabled=false;
            player.GetComponent<Rigidbody2D>().position=new Vector2(0,-5);
            boss.enabled=false; boss.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            boss.GetComponent<Rigidbody2D>().position=Vector2.zero;
            yield return new WaitForSecondsRealtime(4.2f);
            var director=boss.GetComponent<BossAttackDirector>(); var health=boss.GetComponent<EnemyHealth>(); var parts=boss.GetComponent<BossParts>();
            for(int kind=0;kind<4;kind++) for(int phase=0;phase<3;phase++) {
                director.StopAllCoroutines();
                foreach(var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None)) Destroy(bullet.gameObject);
                foreach(var hazard in FindObjectsByType<BossHazard>(FindObjectsSortMode.None)) Destroy(hazard.gameObject);
                foreach(var mechanic in FindObjectsByType<BossMechanic>(FindObjectsSortMode.None)) Destroy(mechanic.gameObject);
                var attack=BossCatalog.Attack((BossKind)kind,phase+1,0);
                if(kind==0 && phase==0) attack=BossAttack.Shockwave;
                boss.RestoreEncounter(kind,phase+1,(int)attack,player.transform.position,0);
                boss.RestoreState((int)BossState.Recover,50,Vector2.down,0);
                health.SetSynchronizedHealth(health.maximum*(phase==0?1:.45f),health.maximum);
                parts.Restore(phase==0?Vector4.one*parts.Maximum:Vector4.zero,parts.Maximum);
                yield return new WaitForSecondsRealtime(.1f);
                director.Execute(attack,Vector2.zero,player.transform.position,phase+1,1,boss.BrokenMask);
                yield return new WaitForSecondsRealtime(phase==0?.3f:1.9f);
                HUDController.Instance.Toast("");
                SaveFrame(Path.Combine(folder,((BossKind)kind)+"_"+phase+".png"));
            }
            director.StopAllCoroutines();
            foreach(var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None)) Destroy(bullet.gameObject);
            foreach(var hazard in FindObjectsByType<BossHazard>(FindObjectsSortMode.None)) Destroy(hazard.gameObject);
            foreach(var mechanic in FindObjectsByType<BossMechanic>(FindObjectsSortMode.None)) Destroy(mechanic.gameObject);
            boss.RestoreEncounter(0,1,0,Vector2.zero,0);
            foreach(var element in new[]{CombatElement.Ice,CombatElement.Water,CombatElement.Wood,CombatElement.Earth,CombatElement.Light,CombatElement.Dark}) {
                foreach(int level in new[]{1,4,8}) {
                    foreach(var cast in FindObjectsByType<StaffCast>(FindObjectsSortMode.None)) Destroy(cast.gameObject);
                    foreach(var fx in FindObjectsByType<CombatFx>(FindObjectsSortMode.None)) Destroy(fx.gameObject);
                    foreach(var mote in FindObjectsByType<CombatMote>(FindObjectsSortMode.None)) Destroy(mote.gameObject);
                    StaffCast.Create(element,player.transform.position,Vector2.zero,level,0,null,level>=4 && (element==CombatElement.Light || element==CombatElement.Dark));
                    HUDController.Instance.Toast((element==CombatElement.Ice?"氷の槍":element==CombatElement.Water?"水のオーブ":element==CombatElement.Wood?"木の種":element==CombatElement.Earth?"土のハンマー":element==CombatElement.Light?"光の槍":"闇のオーブ")+" Lv."+level);
                    yield return new WaitForSecondsRealtime(.28f);
                    SaveFrame(Path.Combine(folder,"杖_"+element+"_"+level+".png"));
                }
            }
            foreach(var cast in FindObjectsByType<StaffCast>(FindObjectsSortMode.None)) Destroy(cast.gameObject);
            foreach(var fx in FindObjectsByType<CombatFx>(FindObjectsSortMode.None)) Destroy(fx.gameObject);
            foreach(var mote in FindObjectsByType<CombatMote>(FindObjectsSortMode.None)) Destroy(mote.gameObject);
            for(int weapon=0;weapon<10;weapon++) foreach(int level in new[]{1,8}) {
                foreach(var shot in FindObjectsByType<PhysicalMissile>(FindObjectsSortMode.None))Destroy(shot.gameObject);
                foreach(var slash in FindObjectsByType<PhysicalSlash>(FindObjectsSortMode.None))Destroy(slash.gameObject);
                PhysicalAttackPattern.Create(weapon,player.transform.position,(Vector2)player.transform.position+Vector2.right*4,level,0,player);
                HUDController.Instance.Toast(PhysicalEvolution.Recipes[weapon].Name+" Lv."+level);
                yield return new WaitForSecondsRealtime(.2f);
                SaveFrame(Path.Combine(folder,"合体_"+weapon+"_"+level+".png"));
                yield return new WaitForSecondsRealtime(.3f);
                SaveFrame(Path.Combine(folder,"合体_"+weapon+"_"+level+"_後半.png"));
                yield return new WaitForSecondsRealtime(1.5f);
            }
            foreach(var shot in FindObjectsByType<PhysicalMissile>(FindObjectsSortMode.None))Destroy(shot.gameObject);
            for(int reaction=2;reaction<=13;reaction++) {
                Vector2 point=new Vector2((reaction-2)%4*3-4.5f,(reaction-2)/4*3-6);
                CombatFx.Reaction(reaction,CombatElement.Fire,point);
            }
            player.GrantBarrier(); HUDController.Instance.Toast("属性反応と一撃防御の結晶バリア");
            yield return new WaitForSecondsRealtime(.1f);
            SaveFrame(Path.Combine(folder,"属性反応とバリア.png"));
            var options=new System.Collections.Generic.List<UpgradeOption>();
            foreach(var id in new[]{"hammer","wood","crossbow"}) {
                var weapon=PhysicalEvolution.Weapon(player.GetComponent<PlayerStats>(),id);weapon.SetLevel(0);
                options.Add(new UpgradeOption(WeaponCatalog.Find(id).Kind,WeaponCatalog.Find(id).Name+" Lv.0 → 1",WeaponCatalog.UpgradeDescription(weapon)+PhysicalEvolution.Hint(id)));
            }
            HUDController.Instance.LevelUI.Show(options,_=>{},20);
            yield return new WaitForSecondsRealtime(.1f);
            SaveFrame(Path.Combine(folder,"強化カード.png"));
            File.WriteAllText(Path.Combine(folder,"描画完了.txt"),"ホーム・攻略・合体ガイド・4ボス3形態・属性攻撃・10合体2レベル2時点・反応・結晶・強化カードを1280×720で描画。");
            Application.Quit();
        }
        static void SaveFrame(string path)
        {
            var camera=Camera.main; var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32); target.Create();
            var previous=camera.targetTexture; var previousActive=RenderTexture.active; float aspect=camera.aspect;
            camera.targetTexture=target; camera.aspect=1280f/720;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; canvas.sortingLayerName="UI"; canvas.sortingOrder=1000; }
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            RenderTexture.active=target;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());
            camera.targetTexture=previous; camera.aspect=aspect; RenderTexture.active=previousActive;
            Destroy(texture); target.Release(); Destroy(target);
        }
    }
}
#endif
