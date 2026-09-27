using UnityEngine;
namespace RogueSurvivors
{
    // 通常命中より短く鋭い反応演出。音とラベルは連発時にも上限を設ける。
    public static class ReactionFeedback
    {
        static float nextSound,nextLabel,nextSupportLabel;
        static AudioClip impact;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){nextSound=nextLabel=nextSupportLabel=0;}
        public static void Show(Vector2 position,int reaction)
        {
            if(reaction==11||reaction==13) {
                if(Time.unscaledTime>=nextSupportLabel) {nextSupportLabel=Time.unscaledTime+.35f;EffectsService.Instance?.Popup((Vector3)position+Vector3.up*.55f,ReactionBalance.Name(reaction),ReactionBalance.Color(reaction));}
                return;
            }
            if(Time.unscaledTime>=nextLabel) {
                nextLabel=Time.unscaledTime+.065f;
                EffectsService.Instance?.Popup((Vector3)position+Vector3.up*.55f,ReactionBalance.Name(reaction),ReactionBalance.Color(reaction),true);
            }
            if(reaction==11||reaction==13||reaction==8||Time.unscaledTime<nextSound)return;
            nextSound=Time.unscaledTime+.12f;
            if(!impact) {
                const int rate=22050;var samples=new float[3307];
                for(int i=0;i<samples.Length;i++) {float t=i/(float)rate;float envelope=Mathf.Exp(-t*28);samples[i]=(Mathf.Sin(2*Mathf.PI*(180*t-350*t*t))*.55f+Mathf.Sin(i*2.39996f)*Mathf.Exp(-t*90)*.2f)*envelope;}
                impact=AudioClip.Create("属性反応の着弾",samples.Length,1,rate,false);impact.SetData(samples,0);
            }
            EffectsService.Instance?.PlayImpact(impact);
        }
        public static void Damage(Vector2 position,float damage,int reaction)
        {
            if(damage<=0)return;
            EffectsService.Instance?.Popup(position,Mathf.CeilToInt(damage).ToString("N0"),ReactionBalance.Color(reaction),true);
        }
    }
}
