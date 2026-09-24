using UnityEngine;
namespace RogueSurvivors
{
    public sealed class DamagePopup : MonoBehaviour
    {
        TextMesh label;
        float remaining = .65f;
        public void Initialize(string value, Color color)
        {
            label = gameObject.AddComponent<TextMesh>(); label.text = value; label.color = color;
            label.fontSize = 48; label.characterSize = .055f; label.anchor = TextAnchor.MiddleCenter;
            GetComponent<MeshRenderer>().sortingLayerName = "UI";
        }
        void Update()
        {
            remaining -= Time.deltaTime; transform.position += Vector3.up * Time.deltaTime * 1.1f;
            if (label) { var color = label.color; color.a = Mathf.Clamp01(remaining * 2); label.color = color; }
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
