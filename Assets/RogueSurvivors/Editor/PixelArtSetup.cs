using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    // Original pixel drawings. No third-party game artwork is used.
    public static class PixelArtSetup
    {
        const string Root = "Assets/RogueSurvivors/Resources/RogueSurvivors/Art";
        static Texture2D canvas;
        static readonly Color32 Ink = new Color32(20, 24, 38, 255);
        static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);
        static void New(int size = 32) { canvas = new Texture2D(size, size, TextureFormat.RGBA32, false); canvas.SetPixels32(new Color32[size * size]); }
        static void Rect(int x, int y, int w, int h, Color32 color)
        { for (int py = y; py < y + h; py++) for (int px = x; px < x + w; px++) if (px >= 0 && py >= 0 && px < canvas.width && py < canvas.height) canvas.SetPixel(px, canvas.height - 1 - py, color); }
        static void Line(int x, int y, int tx, int ty, Color32 color)
        {
            int count = Mathf.Max(Mathf.Abs(tx - x), Mathf.Abs(ty - y));
            for (int i = 0; i <= count; i++) Rect(Mathf.RoundToInt(Mathf.Lerp(x, tx, count == 0 ? 0 : (float)i / count)), Mathf.RoundToInt(Mathf.Lerp(y, ty, count == 0 ? 0 : (float)i / count)), 1, 1, color);
        }
        static Sprite Save(string name, float pixelsPerUnit = 32)
        {
            canvas.Apply(); string path = Root + "/" + name + ".png";
            File.WriteAllBytes(path, canvas.EncodeToPNG()); Object.DestroyImmediate(canvas);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Person(string id, Color32 cloak, Color32 light, bool helmet, bool staff)
        {
            New();
            Rect(8, 28, 17, 2, C(27, 31, 39));
            Rect(10, 18, 13, 9, Ink); Rect(8, 19, 17, 6, Ink);
            Rect(9, 19, 15, 5, cloak); Rect(11, 17, 11, 10, cloak);
            Rect(12, 18, 3, 8, light); Rect(20, 19, 2, 7, C(35, 50, 65));
            Rect(11, 26, 4, 3, Ink); Rect(19, 26, 4, 3, Ink);
            Rect(10, 28, 6, 2, C(91, 66, 52)); Rect(18, 28, 6, 2, C(91, 66, 52));
            Rect(10, 6, 13, 12, Ink); Rect(8, 9, 17, 7, Ink);
            Rect(10, 7, 13, 9, helmet ? C(121, 135, 151) : cloak);
            Rect(11, 11, 11, 7, C(232, 186, 136)); Rect(12, 17, 8, 2, C(177, 117, 87));
            Rect(12, 11, 9, 2, helmet ? C(78, 85, 101) : C(74, 49, 45));
            Rect(13, 13, 2, 2, Ink); Rect(19, 13, 2, 2, Ink); Rect(17, 16, 2, 1, C(151, 78, 71));
            Rect(11, 7, 2, 4, light); Rect(13, 6, 9, 2, light);
            Rect(12, 22, 10, 2, C(86, 57, 46)); Rect(16, 22, 2, 2, C(239, 190, 96));
            Rect(7, 19, 3, 4, C(232, 186, 136)); Rect(23, 19, 3, 4, C(232, 186, 136));
            if (staff)
            {
                Line(26, 10, 26, 28, C(133, 90, 64)); Rect(24, 7, 5, 5, Ink);
                Rect(25, 7, 3, 4, C(173, 130, 246)); Rect(26, 6, 1, 6, C(224, 211, 255));
            }
            else if (helmet)
            {
                Rect(14, 3, 5, 4, C(196, 89, 64)); Rect(16, 2, 5, 2, C(237, 142, 90));
                Rect(25, 12, 2, 13, C(221, 233, 233)); Rect(24, 14, 1, 10, C(112, 149, 173));
                Rect(22, 24, 7, 2, C(234, 177, 74)); Rect(25, 26, 2, 3, C(111, 68, 45));
                if (id == "lancer") { Rect(26, 7, 2, 22, C(181, 134, 78)); Rect(25, 3, 4, 7, C(192, 225, 232)); Rect(26, 2, 2, 8, C(244, 251, 237)); }
            }
            else
            {
                Line(26, 10, 29, 15, C(196, 137, 70)); Line(29, 15, 29, 23, C(196, 137, 70));
                Line(29, 23, 26, 28, C(196, 137, 70)); Line(26, 11, 26, 27, C(224, 222, 192));
                Line(11, 6, 7, 3, C(174, 226, 187)); Rect(7, 3, 2, 3, C(107, 179, 137));
            }
            Save(id);
        }
        public static void Apply()
        {
            Directory.CreateDirectory(Root);
            Person("ranger", C(40, 105, 117), C(89, 182, 173), false, false);
            Person("warden", C(89, 61, 126), C(159, 114, 183), false, true);
            Person("knight", C(102, 69, 67), C(160, 148, 133), true, false);
            Person("mage", C(137, 66, 52), C(218, 137, 71), false, true);
            Person("lancer", C(49, 83, 116), C(129, 179, 201), true, false);
            Person("armored", C(76, 52, 69), C(120, 125, 132), true, false);
            Person("enemy_archer", C(124, 54, 54), C(177, 87, 63), false, false);
            New(); Rect(8, 16, 17, 11, Ink); Rect(10, 15, 14, 11, C(128, 121, 137));
            Rect(8, 11, 6, 8, Ink); Rect(9, 12, 4, 6, C(188, 126, 143));
            Rect(18, 11, 6, 8, Ink); Rect(19, 12, 4, 6, C(188, 126, 143));
            Rect(7, 19, 18, 6, C(146, 137, 149)); Rect(12, 20, 2, 2, C(229, 84, 97)); Rect(20, 20, 2, 2, C(229, 84, 97));
            Rect(15, 24, 3, 2, C(209, 158, 170)); Line(4, 23, 12, 24, C(177, 170, 181)); Line(21, 24, 29, 22, C(177, 170, 181));
            Line(24, 26, 29, 28, C(163, 112, 132)); Rect(10, 27, 4, 2, C(184, 128, 143)); Rect(20, 27, 4, 2, C(184, 128, 143)); Save("rat");
            New(); Rect(6, 13, 21, 14, Ink); Rect(7, 14, 19, 12, C(120, 77, 56));
            Rect(9, 10, 5, 9, C(151, 103, 67)); Rect(20, 10, 5, 9, C(151, 103, 67));
            Rect(8, 18, 17, 7, C(168, 115, 73)); Rect(10, 17, 3, 2, C(242, 145, 72)); Rect(21, 17, 3, 2, C(242, 145, 72));
            Rect(12, 23, 10, 5, C(194, 141, 102)); Rect(14, 24, 2, 2, Ink); Rect(19, 24, 2, 2, Ink);
            Rect(7, 23, 3, 5, C(240, 221, 171)); Rect(24, 23, 3, 5, C(240, 221, 171));
            Line(10, 13, 5, 7, C(238, 209, 144)); Line(22, 13, 27, 7, C(238, 209, 144)); Save("charger");
            New();
            Rect(10, 17, 13, 10, Ink); Rect(8, 20, 17, 5, C(104, 73, 90));
            Rect(12, 17, 9, 9, C(174, 167, 145)); Rect(16, 18, 1, 7, Ink);
            for (int y = 19; y < 25; y += 2) Rect(12, y, 9, 1, C(91, 92, 90));
            Rect(11, 7, 12, 11, Ink); Rect(10, 8, 14, 7, Ink); Rect(11, 8, 12, 8, C(211, 208, 176));
            Rect(13, 10, 3, 3, Ink); Rect(19, 10, 3, 3, Ink); Rect(14, 11, 1, 1, C(238, 104, 138)); Rect(20, 11, 1, 1, C(238, 104, 138));
            Rect(17, 13, 1, 2, Ink); for (int x = 13; x < 22; x += 2) Rect(x, 16, 1, 2, C(198, 191, 156));
            Line(9, 18, 7, 25, C(211, 208, 176)); Line(24, 18, 26, 25, C(211, 208, 176));
            Rect(12, 26, 3, 4, C(211, 208, 176)); Rect(20, 26, 3, 4, C(211, 208, 176));
            Rect(5, 21, 2, 8, C(101, 68, 46)); Rect(4, 15, 3, 8, C(160, 177, 174)); Save("skeleton");
            New();
            for (int y = 13; y < 29; y++) { int spread = (y - 13) / 3; Rect(9 - spread, y, 14 + 2 * spread, 1, Ink); Rect(10 - spread, y, 12 + 2 * spread, 1, C(68, 43, 83)); }
            Rect(9, 5, 14, 13, Ink); Rect(10, 6, 12, 10, C(113, 103, 134));
            Line(10, 8, 5, 2, C(217, 187, 133)); Line(21, 8, 26, 2, C(217, 187, 133));
            Rect(5, 1, 2, 5, C(235, 220, 173)); Rect(25, 1, 2, 5, C(235, 220, 173));
            Rect(11, 10, 4, 2, C(248, 82, 141)); Rect(18, 10, 4, 2, C(248, 82, 141));
            Rect(15, 12, 3, 3, C(44, 28, 55)); Rect(12, 16, 9, 3, C(175, 131, 139));
            Rect(12, 20, 9, 1, C(183, 136, 99)); Rect(16, 19, 2, 7, C(244, 131, 183));
            Rect(3, 18, 5, 6, C(151, 124, 142)); Rect(25, 18, 5, 6, C(151, 124, 142)); Save("riftwarden");
            New(); Line(4, 16, 27, 16, Ink); Rect(5, 15, 19, 3, C(144, 215, 207));
            Line(22, 12, 28, 16, C(230, 255, 241)); Line(28, 16, 22, 20, C(230, 255, 241));
            Line(6, 13, 10, 16, C(80, 139, 137)); Line(6, 19, 10, 16, C(80, 139, 137)); Save("arrow");
            New(); Rect(4, 15, 8, 3, C(126, 82, 48)); Rect(10, 11, 3, 11, C(237, 189, 97));
            Rect(13, 14, 13, 5, C(120, 176, 187)); Rect(13, 14, 13, 2, C(228, 242, 225));
            Line(26, 14, 30, 16, C(228, 242, 225)); Line(30, 16, 26, 18, C(120, 176, 187)); Save("blade");
            New(); Rect(1, 15, 23, 3, C(134, 91, 58)); Rect(22, 12, 4, 8, Ink);
            Rect(23, 13, 5, 6, C(139, 186, 194)); Line(23, 13, 31, 16, C(235, 246, 229)); Line(31, 16, 23, 19, C(139, 186, 194)); Save("spear");
            New(); Rect(9, 9, 16, 15, C(163, 49, 38)); Rect(13, 7, 8, 20, C(211, 71, 33));
            Line(4, 4, 18, 16, C(249, 120, 40)); Line(3, 27, 18, 16, C(249, 120, 40));
            Rect(12, 11, 11, 12, C(248, 134, 42)); Rect(17, 12, 8, 8, C(255, 202, 85)); Rect(19, 13, 4, 5, C(255, 242, 169)); Save("fireball");
            New(); Rect(12, 6, 8, 4, C(173, 127, 73)); Rect(11, 10, 10, 4, C(155, 200, 198)); Rect(8, 14, 16, 14, Ink);
            Rect(9, 15, 14, 12, C(177, 213, 203)); Rect(10, 19, 12, 7, C(67, 167, 102)); Rect(11, 16, 2, 7, C(233, 250, 224)); Save("potion");
            New(); Rect(7, 8, 6, 16, C(158, 60, 67)); Rect(20, 8, 6, 16, C(60, 99, 177)); Rect(11, 21, 12, 7, C(150, 162, 172));
            Rect(7, 7, 6, 5, C(229, 232, 216)); Rect(20, 7, 6, 5, C(229, 232, 216)); Save("magnet");
            New(); Rect(8, 13, 17, 14, Ink); Rect(10, 12, 13, 15, C(66, 71, 87)); Rect(12, 14, 3, 6, C(128, 142, 160));
            Line(18, 12, 22, 5, C(184, 139, 76)); Rect(20, 3, 5, 4, C(247, 160, 58)); Rect(22, 2, 1, 6, C(255, 229, 131)); Save("bomb");
            New(64); Rect(0, 0, 64, 64, C(29, 42, 44));
            var rng = new System.Random(297);
            for (int i = 0; i < 140; i++) { int x = rng.Next(64), y = rng.Next(64); Rect(x, y, 1, 2, C(39, 57, 52)); }
            for (int y = 0; y < 64; y += 16) for (int x = -8; x < 64; x += 24) {
                int px = x + ((y / 16) % 2) * 12; Rect(px, y, 21, 13, C(43, 52, 56));
                Rect(px + 1, y + 1, 19, 1, C(57, 65, 64)); Rect(px + 1, y + 12, 19, 1, C(24, 37, 40));
                Line(px + 8, y + 1, px + 10, y + 5, C(30, 43, 44));
            }
            var floor = Save("ruin_floor", 32);
            SetPrefab("Player", "ranger"); SetPrefab("FusionPlayer", "ranger");
            SetPrefab("Enemy", "skeleton"); SetPrefab("Boss", "riftwarden"); SetPrefab("FusionBoss", "riftwarden");
            SetPrefab("PlayerBullet", "arrow"); SetPrefab("OrbitBlade", "blade");
            foreach (string name in new[] { "HomeScene", "SoloScene", "LobbyScene", "MultiBossScene" }) {
                string path = "Assets/RogueSurvivors/Scenes/" + name + ".unity";
                if (!File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path);
                var grid = GameObject.Find("Grid Floor"); if (grid) grid.GetComponent<SpriteRenderer>().sprite = floor;
                EditorSceneManager.SaveScene(scene);
            }
        }
        static void SetPrefab(string name, string art)
        {
            string path = "Assets/RogueSurvivors/Resources/RogueSurvivors/" + name + ".prefab";
            if (!File.Exists(path)) return;
            var go = PrefabUtility.LoadPrefabContents(path);
            var sprite = go.GetComponentInChildren<SpriteRenderer>(); sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + art + ".png"); sprite.color = Color.white;
            if (name == "PlayerBullet") sprite.transform.localScale = new Vector3(.7f, .7f, 1);
            if (name == "OrbitBlade") go.transform.localScale = Vector3.one * .9f;
            PrefabUtility.SaveAsPrefabAsset(go, path); PrefabUtility.UnloadPrefabContents(go);
        }
    }
}
