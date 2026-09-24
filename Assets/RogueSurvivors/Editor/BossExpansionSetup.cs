using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    public static class BossExpansionSetup
    {
        [MenuItem("Tools/Rogue Survivors/Update Boss Arena and Art")]
        public static void Apply()
        {
            const string folder="Assets/RogueSurvivors/Resources/RogueSurvivors/Art/Bosses";
            Directory.CreateDirectory(folder);
            for(int kind=0;kind<4;kind++) for(int phase=0;phase<3;phase++) {
                string path=folder+"/"+(BossKind)kind+"_v4_"+phase+".png";
                if(!File.Exists(path)) File.WriteAllBytes(path,BossAppearance.GetSprite((BossKind)kind,phase+1).texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.spritePixelsPerUnit=32;
                importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=true; importer.SaveAndReimport();
            }
            const string scenePath="Assets/RogueSurvivors/Scenes/MultiBossScene.unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool opened=!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            if(scene.isDirty) throw new System.InvalidOperationException("ボスシーンを保存してから更新してください。");
            foreach(var root in scene.GetRootGameObjects()) {
                switch(root.name) {
                    case "North": root.transform.position=new Vector3(0,18,0); root.transform.localScale=new Vector3(56.4f,.4f,1); break;
                    case "South": root.transform.position=new Vector3(0,-18,0); root.transform.localScale=new Vector3(56.4f,.4f,1); break;
                    case "West": root.transform.position=new Vector3(-28,0,0); root.transform.localScale=new Vector3(.4f,36,1); break;
                    case "East": root.transform.position=new Vector3(28,0,0); root.transform.localScale=new Vector3(.4f,36,1); break;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if(opened) EditorSceneManager.CloseScene(scene,true);
            AssetDatabase.SaveAssets();
            Debug.Log("BOSS_EXPANSION_SETUP_OK: four bosses, twelve sprites, 56 x 36 arena.");
        }
    }
}
