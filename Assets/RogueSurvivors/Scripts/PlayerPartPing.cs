using UnityEngine;
namespace RogueSurvivors
{
    public sealed class PlayerPartPing : MonoBehaviour
    {
        public const float Lifetime=8;
        int localPart=-1;float until,nextSend;
        void Update(){if(!GetComponent<PlayerHealth>().Alive){localPart=-1;until=0;}}
        public int Part {
            get {var sync=GetComponent<NetworkPlayerSync>();return sync && sync.IsNetworked?sync.PartPing:Time.time<until?localPart:-1;}
        }
        public int Number {get{var sync=GetComponent<NetworkPlayerSync>();return sync && sync.IsNetworked?sync.PingNumber:1;}}
        public bool SetPart(BossParts boss,int part)
        {
            var player=GetComponent<PlayerHealth>();
            if(!player || !player.IsLocal || !player.Alive || !GameManager.Instance || !GameManager.Instance.IsPlaying || GameManager.Instance.Mode!=RunMode.Boss)return false;
            if(!boss || part< -1 || part>3 || (part>=0 && (boss.Health[part]<=0 || Time.unscaledTime<nextSend)))return false;
            var sync=GetComponent<NetworkPlayerSync>();
            if(sync && sync.IsNetworked)sync.PublishPartPing(part);
            else{localPart=part;until=Time.time+Lifetime;}
            nextSend=Time.unscaledTime+.4f;return true;
        }
        public int VisiblePart(BossParts boss)
        {
            int part=Part;return GetComponent<PlayerHealth>().Alive && boss && part>=0 && part<4 && boss.Health[part]>0?part:-1;
        }
        public void Cycle(BossParts boss)
        {
            if(!boss)return;for(int i=1;i<=4;i++){int part=(Part+i+4)%4;if(boss.Health[part]>0){SetPart(boss,part);return;}}
        }
    }
}
