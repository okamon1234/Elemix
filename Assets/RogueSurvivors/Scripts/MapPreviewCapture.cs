#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    public sealed class MapPreviewCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--rogue-map-gallery");if(index<0 || index+1>=args.Length)return;
            var go=new GameObject("マップ描画確認");DontDestroyOnLoad(go);go.AddComponent<MapPreviewCapture>().StartCoroutine(Capture(args[index+1]));
        }
        static IEnumerator Capture(string folder)
        {
            Directory.CreateDirectory(folder);yield return new WaitForSecondsRealtime(.7f);
            BossPreviewCapture.SaveFrame(Path.Combine(folder,"ホームのマップ選択.png"));FindFirstObjectByType<MapSelectionUI>().Open();yield return new WaitForSecondsRealtime(.1f);
            BossPreviewCapture.SaveFrame(Path.Combine(folder,"マップ選択ガイド.png"));
            SceneManager.LoadScene("SoloScene");yield return null;yield return null;yield return null;
            GameManager.Instance.soloDuration=1800;FindFirstObjectByType<EnemySpawner>().enabled=false;
            foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active))Destroy(enemy.gameObject);
            var player=PlayerHealth.Local;player.GetComponent<PlayerMovement>().enabled=false;player.GetComponent<LevelUpManager>().enabled=false;player.GrantInvulnerability(300);
            foreach(var weapon in player.GetComponents<WeaponBase>())weapon.enabled=false;
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=9;
            for(int kind=0;kind<2;kind++) {
                var mapKind=(SoloMapKind)kind;var map=SoloMap.Instance;map.Initialize(mapKind);
                for(int district=0;district<3;district++) {
                    var key=Vector2Int.zero;bool found=false;
                    for(int y=-2;y<=2 && !found;y++)for(int x=-2;x<=2;x++)if(SoloMapLayout.District(mapKind,new Vector2Int(x,y))==district){key=new Vector2Int(x,y);found=true;break;}
                    Vector2 position=(Vector2)key*24;player.GetComponent<Rigidbody2D>().position=position;map.RefreshAround(position);camera.transform.position=new Vector3(position.x,position.y,-10);
                    HUDController.Instance.Toast(SoloMapCatalog.Name(mapKind)+"・"+SoloMapCatalog.District(mapKind,district));yield return new WaitForSecondsRealtime(.25f);
                    BossPreviewCapture.SaveFrame(Path.Combine(folder,SoloMapCatalog.Name(mapKind)+"_"+district+".png"));
                }
                var names=new[]{"Enemy","EnemyRat","EnemyArmored","EnemyArcher","EnemyCharger"};
                Vector2 center=player.transform.position;camera.orthographicSize=7;
                for(int enemy=0;enemy<28;enemy++) {
                    float a=enemy*2.39996f;Vector2 point=map.FindOpenSpot(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(5+enemy%5));
                    Instantiate(Resources.Load<EnemyHealth>("RogueSurvivors/"+names[enemy%5]),point,Quaternion.identity).Scale(20);
                }
                StaffCast.Create(CombatElement.Water,center,center+Vector2.right*3,8,0,null);
                StaffCast.Create(CombatElement.Wood,center,center+Vector2.up*3,8,0,null);
                yield return new WaitForSecondsRealtime(.3f);BossPreviewCapture.SaveFrame(Path.Combine(folder,SoloMapCatalog.Name(mapKind)+"_戦闘.png"));
                foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active))Destroy(enemy.gameObject);
                foreach(var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))Destroy(bullet.gameObject);
                yield return new WaitForSecondsRealtime(1);camera.orthographicSize=9;
            }
            File.WriteAllText(Path.Combine(folder,"描画完了.txt"),"ホーム・マップ選択・草原と遺跡の3区画・各マップの戦闘を描画。");Application.Quit();
        }
    }
}
#endif
