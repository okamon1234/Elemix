using UnityEngine;
namespace RogueSurvivors
{
    // Four directional armor plates; all must break before the core can be damaged.
    public sealed class BossParts : MonoBehaviour
    {
        public Vector4 Health { get; private set; }
        public float Maximum { get; private set; }
        public bool Exposed => BrokenCount == 4;
        public int BrokenCount { get { int count = 0; for (int i = 0; i < 4; i++) if (Health[i] <= 0) count++; return count; } }
        public static readonly string[] Effects = { "攻撃威力低下", "弾数減少", "設置時間短縮", "移動・突進減速" };
        public static readonly string[] Names = { "右腕", "頭部", "左腕", "脚部" };
        readonly SpriteRenderer[] plates = new SpriteRenderer[4];
        bool initialized;
        string PartName(int index) { var boss=GetComponent<BossAI>(); return boss ? BossCatalog.PartName(boss.Kind,index) : Names[index]; }
        public void Configure(float coreHealth)
        {
            if (initialized) return;
            initialized = true; Maximum = Mathf.Max(120, coreHealth * .12f); Health = Vector4.one * Maximum;
        }
        void Start()
        {
            Configure(GetComponent<EnemyHealth>().maximum);
            var main = GetComponentInChildren<SpriteRenderer>();
            for (int i = 0; i < 4; i++) {
                var go = new GameObject(Names[i]); go.transform.SetParent(transform, false);
                float a = i * Mathf.PI / 2;
                go.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 1.45f;
                go.transform.localScale = new Vector3(1.1f, 1.1f, 1);
                plates[i] = go.AddComponent<SpriteRenderer>(); plates[i].sprite = CreatePlate(i);
                plates[i].sortingLayerName = "Characters"; plates[i].sortingOrder = 5;
                if (main) plates[i].sharedMaterial = main.sharedMaterial;
            }
        }
        static readonly Sprite[] armorSprites = new Sprite[4];
        static Sprite CreatePlate(int part)
        {
            if (armorSprites[part]) return armorSprites[part];
            var texture = new Texture2D(24, 24, TextureFormat.RGBA32, false); texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 24; y++) for (int x = 0; x < 24; x++) {
                bool shape = part == 1 ? y > 3 && y < 21 && Mathf.Abs(x - 11) < 9 - Mathf.Abs(y - 13) / 3 :
                    part == 3 ? y > 2 && y < 21 && x > 3 && x < 21 && (x < 10 || x > 13 || y > 13) :
                    x > 3 && x < 21 && y > 3 && y < 21 && Mathf.Abs(x - 12) + Mathf.Abs(y - 12) < 15;
                Color color = !shape ? Color.clear : (x < 6 || x > 18 || y < 5 || y > 19) ? new Color(.13f,.16f,.23f) :
                    (y % 5 == 0) ? new Color(.22f,.3f,.4f) : x < 12 ? new Color(.65f,.73f,.8f) : new Color(.35f,.44f,.55f);
                if (shape && Mathf.Abs(x - 12) < 2 && Mathf.Abs(y - 12) < 3) color = new Color(1,.45f,.2f);
                texture.SetPixel(x, y, color);
            }
            texture.Apply(); armorSprites[part] = Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(.5f,.5f), 24); return armorSprites[part];
        }
        public void Restore(Vector4 health, float maximum)
        {
            if (maximum <= 0) return;
            initialized = true; Maximum = maximum; Health = health;
        }
        public int PartFrom(Vector2 origin)
        {
            Vector2 delta = origin - (Vector2)transform.position;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) return delta.x >= 0 ? 0 : 2;
            return delta.y >= 0 ? 1 : 3;
        }
        public bool Absorb(float damage, Vector2 origin)
        {
            Configure(GetComponent<EnemyHealth>().maximum);
            if (Exposed) return false;
            int index = PartFrom(origin);
            if (Health[index] <= 0) return true;
            Vector4 next = Health; next[index] = Mathf.Max(0, next[index] - damage); Health = next;
            EffectsService.Instance?.Popup(transform.position, PartName(index) + " −" + Mathf.CeilToInt(damage), new Color(1, .8f, .3f));
            return true;
        }
        int shownMask;
        void Update()
        {
            for (int i = 0; i < 4; i++) {
                if (plates[i]) { plates[i].enabled = Health[i] > 0; plates[i].color = Color.Lerp(new Color(1, .25f, .15f), Color.white, Maximum > 0 ? Health[i] / Maximum : 1); }
                if (initialized && Health[i] <= 0 && (shownMask & (1 << i)) == 0) {
                    shownMask |= 1 << i;
                    EffectsService.Instance?.Burst(transform.position, new Color(1, .7f, .2f));
                    HUDController.Instance?.Toast(Exposed ? "全装甲破壊！　本体を攻撃！" : PartName(i) + "を破壊！　" + Effects[i] + "・残りの部位を狙おう");
                }
            }
        }
        public string Status()
        {
            if (Exposed) return "本体露出！　攻撃可能";
            string text = "本体無敵　";
            for (int i = 0; i < 4; i++) text += PartName(i) + (Health[i] <= 0 ? "：破壊済 " : "：" + Mathf.CeilToInt(100 * Health[i] / Mathf.Max(1, Maximum)) + "% ");
            return text;
        }
    }
}
