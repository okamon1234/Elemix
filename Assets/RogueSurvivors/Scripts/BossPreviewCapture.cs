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
            for(int kind=0;kind<4;kind++) for(int phase=0;phase<2;phase++) {
                director.StopAllCoroutines();
                foreach(var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None)) Destroy(bullet.gameObject);
                foreach(var hazard in FindObjectsByType<BossHazard>(FindObjectsSortMode.None)) Destroy(hazard.gameObject);
                var attack=BossCatalog.Attack((BossKind)kind,phase==1,0);
                if(kind==0) attack=phase==0?BossAttack.Shockwave:BossAttack.CrossSlam;
                boss.RestoreEncounter(kind,phase==1,(int)attack,player.transform.position,0);
                boss.RestoreState((int)BossState.Recover,50,Vector2.down,0);
                health.SetSynchronizedHealth(health.maximum*(phase==0?1:.45f),health.maximum);
                parts.Restore(phase==0?Vector4.one*parts.Maximum:Vector4.zero,parts.Maximum);
                yield return new WaitForSecondsRealtime(.1f);
                director.Execute(attack,Vector2.zero,player.transform.position,phase==1,1,boss.BrokenMask);
                yield return new WaitForSecondsRealtime(.3f);
                HUDController.Instance.Toast("");
                SaveFrame(Path.Combine(folder,((BossKind)kind)+"_"+phase+".png"));
            }
            File.WriteAllText(Path.Combine(folder,"描画完了.txt"),"Home, guide, four bosses and both phases rendered at 1280x720.");
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
