using UnityEngine;
namespace RogueSurvivors
{
    // Local presentation and owner-local damage. The boss authority broadcasts the same attack to all peers.
    public sealed class BossHazard : MonoBehaviour
    {
        BossAI owner;
        Vector2 start, end;
        float radius, delay, duration, damage, elapsed, nextHit;
        Color tint;
        LineRenderer outline, fill;
        static Material material;
        public bool Active => elapsed >= delay && elapsed < delay + duration;
        public static BossHazard Create(BossAI boss, Vector2 from, Vector2 to, float width, float warningTime, float activeTime, float hit, Color color)
        {
            var hazard = new GameObject("ボス攻撃予告").AddComponent<BossHazard>();
            hazard.owner = boss; hazard.start = from; hazard.end = to; hazard.radius = width;
            hazard.delay = warningTime; hazard.duration = activeTime; hazard.damage = hit; hazard.tint = color;
            hazard.BuildVisual(); return hazard;
        }
        LineRenderer Line(string name, float width, int order)
        {
            if (!material) material = new Material(Shader.Find("Sprites/Default"));
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(transform,false);
            line.sharedMaterial = material; line.useWorldSpace = true; line.startWidth = line.endWidth = width;
            line.sortingLayerName = "Items"; line.sortingOrder = order; return line;
        }
        void BuildVisual()
        {
            outline = Line("危険範囲の縁",.10f,2);
            bool circle = (end-start).sqrMagnitude < .01f;
            if (circle) {
                outline.loop = true; outline.positionCount = 48;
                for(int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; outline.SetPosition(i,start+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius); }
                fill = Line("危険範囲", radius*1.7f,1); fill.positionCount=2;
                fill.SetPosition(0,start+Vector2.left*radius*.16f); fill.SetPosition(1,start+Vector2.right*radius*.16f); fill.numCapVertices=12;
            } else {
                Vector2 normal = new Vector2(-(end-start).y,(end-start).x).normalized*radius;
                outline.loop=true; outline.positionCount=4;
                outline.SetPositions(new Vector3[] { start+normal,end+normal,end-normal,start-normal });
                fill=Line("危険範囲",radius*2,1); fill.positionCount=2; fill.SetPosition(0,start); fill.SetPosition(1,end);
            }
        }
        public bool Contains(Vector2 point)
        {
            Vector2 delta=end-start;
            float t=delta.sqrMagnitude<.01f?0:Mathf.Clamp01(Vector2.Dot(point-start,delta)/delta.sqrMagnitude);
            return Vector2.Distance(point,start+delta*t)<=radius+.3f;
        }
        void Update()
        {
            if (!owner || owner.IsTransforming || !owner.GetComponent<EnemyHealth>().Alive || !GameManager.Instance || !GameManager.Instance.IsPlaying) { Destroy(gameObject); return; }
            elapsed+=Time.deltaTime;
            if(elapsed>=delay+duration) { Destroy(gameObject); return; }
            Color edge=Active?tint:Color.Lerp(tint,Color.white,.5f+.35f*Mathf.Sin(elapsed*12)); edge.a=1;
            outline.startColor=outline.endColor=edge;
            Color inside=tint; inside.a=Active?.42f:.10f; fill.startColor=fill.endColor=inside;
            if(!Active || Time.time<nextHit) return;
            var player=PlayerHealth.Local;
            if(player && player.IsLocal && player.Alive && Contains(player.transform.position)) { player.Damage(damage); nextHit=Time.time+.9f; }
        }
    }
}
