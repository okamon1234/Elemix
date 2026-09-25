using UnityEngine;
namespace RogueSurvivors
{
    // Frame changes are cosmetic and work with both local movement and replicated transforms.
    public sealed class CharacterAppearance : MonoBehaviour
    {
        SpriteRenderer body;PlayerStats stats;Vector3 lastPosition;float walkClock;float movingUntil;
        void Start() {body=GetComponentInChildren<SpriteRenderer>();stats=GetComponent<PlayerStats>();lastPosition=transform.position;}
        void LateUpdate()
        {
            if(!body || !stats)return;
            if((transform.position-lastPosition).sqrMagnitude>.00001f)movingUntil=Time.time+.08f;
            lastPosition=transform.position;
            bool moving=Time.time<movingUntil && GetComponent<PlayerHealth>().Alive;
            walkClock=moving?walkClock+Time.deltaTime:0;
            int frame=moving?1+(Mathf.FloorToInt(walkClock*7)%2):0;
            var art=ArsenalArt.Hero(stats.CharacterId,frame);if(art) {body.sprite=art;body.transform.localScale=Vector3.one*(moving?1+.012f*Mathf.Sin(walkClock*14):1);}
        }
    }
}
