using UnityEngine;
namespace RogueSurvivors
{
    public sealed class EnemyAttackCue : MonoBehaviour
    {
        static Material material;
        EnemyCombat combat;EnemyHealth health;LineRenderer path,tip;
        LineRenderer Create(string name,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();
            if(!material)material=new Material(Shader.Find("Sprites/Default"));
            line.sharedMaterial=material;line.useWorldSpace=true;line.sortingLayerName="Projectiles";line.sortingOrder=125;
            line.startWidth=line.endWidth=width;line.startColor=line.endColor=new Color(1,.35f,.12f,.9f);line.enabled=false;return line;
        }
        void Start(){combat=GetComponent<EnemyCombat>();health=GetComponent<EnemyHealth>();path=Create("攻撃方向の予告",.055f);tip=Create("予告の矢先",.09f);}
        void LateUpdate()
        {
            if(!combat || !path)return;
            var ailment=GetComponent<EnemyAilment>();bool show=health && health.Alive && combat.WindingUp && (!ailment || !ailment.Frozen);
            path.enabled=tip.enabled=show;if(!show)return;
            Vector2 start=transform.position,d=combat.AimDirection,side=new Vector2(-d.y,d.x);
            float length=combat.role==EnemyRole.Charger?4.55f:6;
            Vector2 end=start+d*length;
            path.positionCount=4;float half=combat.role==EnemyRole.Charger?.34f:.12f;
            path.SetPositions(new Vector3[]{start+side*half,end+side*half,end-side*half,start-side*half});
            tip.positionCount=3;tip.SetPositions(new Vector3[]{end-d*.42f+side*.3f,end,end-d*.42f-side*.3f});
        }
    }
}
