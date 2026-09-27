using UnityEngine;
namespace RogueSurvivors
{
    public sealed class DamagePopup : MonoBehaviour
    {
        TextMesh label;
        float remaining = .65f; bool emphasis; float age;
        public void Initialize(string value, Color color, bool emphasized=false)
        {
            emphasis=emphasized;remaining=emphasis?.85f:.65f;
            label = gameObject.AddComponent<TextMesh>(); label.font=UIFactory.JapaneseFont;GetComponent<MeshRenderer>().sharedMaterial=label.font.material;label.text = value; label.color = color;
            label.fontSize = 48; label.characterSize = emphasis?.085f:.055f; label.anchor = TextAnchor.MiddleCenter;
            GetComponent<MeshRenderer>().sortingLayerName = "UI";
        }
        void Update()
        {
            age+=Time.deltaTime;if(emphasis)transform.localScale=Vector3.one*Mathf.Lerp(1.45f,1,Mathf.Clamp01(age/.14f));
            remaining -= Time.deltaTime; transform.position += Vector3.up * Time.deltaTime * 1.1f;
            if (label) { var color = label.color; color.a = Mathf.Clamp01(remaining * 2); label.color = color; }
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
