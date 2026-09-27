#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    public sealed class ReactionPreviewCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--rogue-reaction-gallery");if(at<0||at+1>=args.Length)return;
            var root=new GameObject("属性反応の画面確認");DontDestroyOnLoad(root);root.AddComponent<ReactionPreviewCapture>().StartCoroutine(Capture(args[at+1]));
        }
        static IEnumerator Capture(string folder)
        {
            Directory.CreateDirectory(folder);yield return null;SceneManager.LoadScene("SoloScene");yield return null;yield return null;yield return null;
            FindFirstObjectByType<EnemySpawner>().enabled=false;GameManager.Instance.soloDuration=1800;
            var player=PlayerHealth.Local;player.GrantInvulnerability(300);player.GetComponent<PlayerMovement>().enabled=false;player.GetComponent<LevelUpManager>().enabled=false;
            foreach(var weapon in player.GetComponents<WeaponBase>())weapon.enabled=false;
            player.GetComponent<Rigidbody2D>().position=Vector2.left*3;
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=5;
            int[] reactions={2,3,4,5,6,8,9,10,14,15,16};
            foreach(int reaction in reactions) {
                foreach(var e in new List<EnemyHealth>(EnemyHealth.Active))if(e)Destroy(e.gameObject);yield return new WaitForSecondsRealtime(1);
                var targets=new List<EnemyHealth>();
                for(int i=0;i<7;i++) {
                    Vector2 point=i==0?Vector2.zero:new Vector2(Mathf.Cos(i*Mathf.PI/3),Mathf.Sin(i*Mathf.PI/3))*2;
                    var enemy=Instantiate(Resources.Load<EnemyHealth>("RogueSurvivors/Enemy"),point,Quaternion.identity);enemy.GetComponent<EnemyAI>().enabled=false;enemy.GetComponent<Rigidbody2D>().simulated=false;enemy.SetSynchronizedHealth(i==0?1200:170,i==0?1200:170);targets.Add(enemy);
                }
                var target=targets[0];
                CombatElement first=reaction==2||reaction==3?CombatElement.Fire:reaction==9||reaction==10?CombatElement.Wood:CombatElement.Water;
                CombatElement second=reaction==2||reaction==5||reaction==10?CombatElement.Lightning:reaction==3||reaction==6||reaction==16?CombatElement.Ice:reaction==8||reaction==14||reaction==15?CombatElement.Wood:CombatElement.Fire;
                target.Damage(100,Vector2.zero,player,first);target.Damage(100,Vector2.zero,player,second);
                if(reaction==14||reaction==15)target.Damage(100,Vector2.zero,player,reaction==14?CombatElement.Lightning:CombatElement.Fire);
                if(reaction==16)target.Damage(100,Vector2.zero,player);
                HUDController.Instance.Toast(ReactionBalance.Name(reaction));
                yield return new WaitForSecondsRealtime(reaction==8?3.06f:reaction==9?.55f:reaction==14||reaction==15?.37f:.09f);
                BossPreviewCapture.SaveFrame(Path.Combine(folder,ReactionBalance.Name(reaction)+".png"));
            }
            File.WriteAllText(Path.Combine(folder,"描画完了.txt"),"11種類の属性反応・追撃を実際のダメージ処理とともに描画。");Application.Quit();
        }
    }
}
#endif
