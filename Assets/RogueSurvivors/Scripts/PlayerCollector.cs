using UnityEngine;
namespace RogueSurvivors
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PlayerCollector : MonoBehaviour
    {
        PlayerHealth player;
        CircleCollider2D area;
        void Awake() { player = GetComponentInParent<PlayerHealth>(); area = GetComponent<CircleCollider2D>(); }
        void Update() { if (player) area.radius = player.GetComponent<PlayerStats>().PickupRadius; }
        void OnTriggerEnter2D(Collider2D other) => Attract(other);
        void OnTriggerStay2D(Collider2D other) => Attract(other);
        void Attract(Collider2D other)
        {
            if (!player || !player.Alive || !player.IsLocal) return;
            other.GetComponent<ExpOrb>()?.Attract(player);
            other.GetComponent<DropItem>()?.Attract(player);
        }
    }
}
