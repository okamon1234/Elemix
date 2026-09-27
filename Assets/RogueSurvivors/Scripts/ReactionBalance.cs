using UnityEngine;
namespace RogueSurvivors
{
    // 武器の多段ヒットが反応の一撃を極端に弱くしないための、共有する火力基準。
    public static class ReactionBalance
    {
        public const float PhysicalPower=1.35f;
        public const float AuraSeconds=6;
        public static float Power(float hit,PlayerHealth source)
        {
            var stats=source?source.GetComponent<PlayerStats>():null;
            return Mathf.Max(hit,stats?(12+Mathf.Min(50,stats.Level)*1.1f)*stats.DamageMultiplier:hit);
        }
        public static float Multiplier(int reaction)
        {
            switch(reaction) {
                case 2:return 3.2f;
                case 3:return 3.6f;
                case 4:return 3.2f;
                case 5:return 2.2f;
                case 6:return 1.5f;
                case 7:return 3.6f;
                case 8:case 11:return 1;
                case 9:return 1.2f;
                case 10:return 3.0f;
                case 12:return 1.5f;
                default:return 1;
            }
        }
        public static float Hit(float hit,int reaction,PlayerHealth source)=>hit+Power(hit,source)*(Multiplier(reaction)-1);
        public static string Name(int reaction)
        {
            string[] names={"","","過負荷","融解","蒸発","感電","凍結","対消滅","開花","燃焼","激化","結晶","超電導","拡散","超開花","烈開花","氷砕き"};
            return reaction>=0&&reaction<names.Length?names[reaction]:"";
        }
        public static Color Color(int reaction)=>reaction==6||reaction==16?new Color(.55f,1,1):reaction==5||reaction==12?new Color(.8f,.65f,1):reaction==7?new Color(1,.65f,1):reaction==8||reaction==10||reaction==14?new Color(.65f,1,.25f):new Color(1,.65f,.2f);
    }
}
