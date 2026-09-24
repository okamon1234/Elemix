using UnityEngine;
namespace RogueSurvivors
{
    public sealed class HitFeedback : MonoBehaviour
    {
        SpriteRenderer sprite;
        Color original;
        float until;
        void Awake() { sprite = GetComponentInChildren<SpriteRenderer>(); if (sprite) original = sprite.color; }
        public void Flash() { until = Time.time + .09f; if (sprite) sprite.color = new Color(1, .35f, .35f); }
        public void RefreshColor() { if (sprite) original = sprite.color; }
        void Update() { if (sprite && Time.time >= until) sprite.color = original; }
    }
}
