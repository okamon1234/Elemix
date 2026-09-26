#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    public sealed class MobPreviewCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--rogue-mob-gallery");if(i<0 || i+1>=args.Length)return;
            var go=new GameObject("雑魚と攻撃の描画確認");DontDestroyOnLoad(go);go.AddComponent<MobPreviewCapture>().StartCoroutine(Capture(args[i+1]));
        }
        static IEnumerator Capture(string folder)
        {
            Directory.CreateDirectory(folder);yield return null;SceneManager.LoadScene("SoloScene");yield return null;yield return null;yield return null;
            GameManager.Instance.soloDuration=1800;FindFirstObjectByType<EnemySpawner>().enabled=false;
            foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active))Destroy(enemy.gameObject);
            var player=PlayerHealth.Local;player.GrantInvulnerability(300);player.GetComponent<PlayerMovement>().enabled=false;
            foreach(var weapon in player.GetComponents<WeaponBase>())weapon.enabled=false;
            foreach(var obstacle in GameObject.FindGameObjectsWithTag("Untagged")) {
                if(obstacle.layer==LayerMask.NameToLayer("World"))Destroy(obstacle);
            }
            player.GetComponent<Rigidbody2D>().position=Vector2.zero;
            yield return new WaitForSecondsRealtime(.5f);
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=new Vector3(0,1,-10);camera.orthographicSize=4;
            var names=new[]{"Enemy","EnemyRat","EnemyArmored","EnemyArcher","EnemyCharger"};
            var labels=new[]{"骸骨兵","大ネズミ","重装兵","弓兵","突進獣"};var samples=new List<EnemyHealth>();
            for(int i=0;i<5;i++) {
                var enemy=Instantiate(Resources.Load<EnemyHealth>("RogueSurvivors/"+names[i]),new Vector3((i-2)*2.35f,2),Quaternion.identity);
                enemy.GetComponent<EnemyAI>().enabled=false;enemy.GetComponent<Rigidbody2D>().simulated=false;samples.Add(enemy);
                EffectsService.Instance.Popup(enemy.transform.position+Vector3.down*1.05f,labels[i],Color.white);
            }
            HUDController.Instance.Toast("");
            yield return new WaitForSecondsRealtime(.1f);BossPreviewCapture.SaveFrame(Path.Combine(folder,"雑魚5種類.png"));
            foreach(var enemy in samples)enemy.GetComponent<Rigidbody2D>().linearVelocity=Vector2.right;
            yield return new WaitForSecondsRealtime(.2f);BossPreviewCapture.SaveFrame(Path.Combine(folder,"雑魚の歩行.png"));
            foreach(var enemy in samples)Destroy(enemy.gameObject);yield return null;
            for(int i=0;i<12;i++)PaintedImpact.Show(i,new Vector2((i%4-1.5f)*2.7f,2.7f-i/4*2.1f),2,.7f);
            HUDController.Instance.Toast("");yield return new WaitForSecondsRealtime(.12f);BossPreviewCapture.SaveFrame(Path.Combine(folder,"攻撃エフェクト.png"));
            yield return new WaitForSecondsRealtime(.8f);
            var archer=Instantiate(Resources.Load<EnemyHealth>("RogueSurvivors/EnemyArcher"),new Vector3(-4,2),Quaternion.identity);
            var charger=Instantiate(Resources.Load<EnemyHealth>("RogueSurvivors/EnemyCharger"),new Vector3(4,2),Quaternion.identity);
            HUDController.Instance.Toast("弓兵は射撃前に照準、突進獣は進行範囲を予告");yield return new WaitForSecondsRealtime(.15f);BossPreviewCapture.SaveFrame(Path.Combine(folder,"雑魚の攻撃予告.png"));
            yield return new WaitForSecondsRealtime(.3f);BossPreviewCapture.SaveFrame(Path.Combine(folder,"敵の矢.png"));
            camera.orthographicSize=7;
            for(int i=0;i<45;i++) {
                float a=i*2.39996f,r=3+Mathf.Sqrt(i)*.65f;Vector2 point=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                Instantiate(Resources.Load<EnemyHealth>("RogueSurvivors/"+names[i%5]),point,Quaternion.identity).Scale(10);
            }
            var build=CharacterCatalog.CreateBuild("mage","ice");build.weapons.Add(new WeaponSaveData("wood",8));build.weapons.Add(new WeaponSaveData("water",8));build.weapons.Add(new WeaponSaveData("earth",8));player.GetComponent<PlayerStats>().Restore(build);player.GrantInvulnerability(300);player.GetComponent<LevelUpManager>().enabled=false;
            foreach(var weapon in player.GetComponents<WeaponBase>())weapon.enabled=true;
            HUDController.Instance.Toast("");
            yield return new WaitForSecondsRealtime(2);BossPreviewCapture.SaveFrame(Path.Combine(folder,"ソロ混戦.png"));
            yield return new WaitForSecondsRealtime(10);BossPreviewCapture.SaveFrame(Path.Combine(folder,"ソロ混戦_後半.png"));
            File.WriteAllText(Path.Combine(folder,"描画完了.txt"),"雑魚5種類・歩行・攻撃素材・予告・矢・混戦2時点を実際のURP描画で確認。");Application.Quit();
        }
    }
}
#endif
