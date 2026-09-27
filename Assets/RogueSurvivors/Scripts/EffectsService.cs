using UnityEngine;
namespace RogueSurvivors
{
    public sealed class EffectsService : MonoBehaviour
    {
        public static EffectsService Instance { get; private set; }
        public ParticleSystem burstPrefab;
        public AudioClip shoot, pickup, hurt, level;
        AudioSource audioSource;
        float nextSound;int defeatFrame,defeatCount;
        void OnEnable() => Instance = this;
        void Awake() { Instance = this; audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .35f; }
        public void Play(string id, float volume = .6f)
        {
            if ((id == "Shoot" || id == "Pickup") && Time.unscaledTime < nextSound) return;
            nextSound = Time.unscaledTime + .045f;
            var clip = id == "Shoot" ? shoot : id == "Pickup" ? pickup : id == "Hurt" ? hurt : level;
            if (clip) audioSource.PlayOneShot(clip, volume);
        }
        public void Burst(Vector3 position, Color color)
        {
            if (!burstPrefab) return;
            var fx = Instantiate(burstPrefab, position, Quaternion.identity);
            var main = fx.main; main.startColor = color; fx.Play(); Destroy(fx.gameObject, 1.5f);
        }
        public void Defeat(Vector2 point)
        {
            if(defeatFrame!=Time.frameCount){defeatFrame=Time.frameCount;defeatCount=0;}
            if(defeatCount++>=8)return;
            for(int i=0;i<4;i++) {
                float angle=(i*90+Time.frameCount%90)*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                CombatMote.Create(point+direction*.15f,direction*2.4f,new Color(.8f,.7f,.48f),new Vector2(.075f,.17f),.24f,2,120);
            }
        }
        public void PlayImpact(AudioClip clip) { if(audioSource && clip)audioSource.PlayOneShot(clip,.27f); }
        public void Popup(Vector3 position, string value, Color color, bool emphasized=false)
        {
            var go = new GameObject("Damage"); bool number=!string.IsNullOrEmpty(value) && char.IsDigit(value[0]);
            go.transform.position = position + Vector3.up * .6f + (number?Vector3.right*(emphasized?.4f:-.2f):Vector3.zero);
            go.AddComponent<DamagePopup>().Initialize(value, color, emphasized);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
