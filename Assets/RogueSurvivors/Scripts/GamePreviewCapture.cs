#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RogueSurvivors
{
    public sealed class GamePreviewCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--rogue-preview");
            if (index < 0 || index + 2 >= args.Length) return;
            if (args[index + 1] != "HomeScene" && args[index + 1] != "SoloScene" && args[index + 1] != "MultiBossScene") return;
            var go = new GameObject("Preview capture"); DontDestroyOnLoad(go);
            go.AddComponent<GamePreviewCapture>().StartCoroutine(Capture(args[index + 1], args[index + 2]));
        }
        static IEnumerator Capture(string scene, string output)
        {
            yield return null; SceneManager.LoadScene(scene);
            yield return new WaitForSecondsRealtime(scene == "SoloScene" ? 8 : 2);
            ScreenCapture.CaptureScreenshot(output);
            yield return new WaitForSecondsRealtime(2);
            Application.Quit();
        }
    }
}
#endif
