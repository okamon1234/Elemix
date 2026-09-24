using UnityEngine;
namespace RogueSurvivors
{
    public sealed class EffectsService : MonoBehaviour
    {
        public static EffectsService Instance { get; private set; }
        public ParticleSystem burstPrefab;
        public AudioClip shoot, pickup, hurt, level;
        AudioSource audioSource;
        float nextSound;
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
        public void Popup(Vector3 position, string value, Color color)
        {
            var go = new GameObject("Damage"); go.transform.position = position + Vector3.up * .6f;
            go.AddComponent<DamagePopup>().Initialize(value, color);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
