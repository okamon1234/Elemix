using UnityEngine;
namespace RogueSurvivors
{
    public sealed class PlayerPassiveAbilities : MonoBehaviour
    {
        PlayerHealth health;
        PlayerStats stats;
        void Awake() { health = GetComponent<PlayerHealth>(); stats = GetComponent<PlayerStats>(); }
        void Update()
        {
            if (health && health.IsLocal && health.Alive && GameManager.Instance && GameManager.Instance.IsPlaying && stats.Regeneration > 0 && Time.time - health.LastDamageTime >= 5)
                health.Heal(stats.Regeneration * Time.deltaTime);
        }
    }
}
