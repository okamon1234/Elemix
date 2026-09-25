using UnityEngine;
namespace RogueSurvivors
{
    // First break establishes the route; later breaks add counterattacks.
    public static class BossAttackRoutes
    {
        static readonly BossAttack[][] Routes = {
            new[]{BossAttack.TripleCharge,BossAttack.SeismicRing,BossAttack.CrossSlam},
            new[]{BossAttack.HammerSweep,BossAttack.CrossSlam,BossAttack.SeismicRing},
            new[]{BossAttack.SeismicRing,BossAttack.TripleCharge,BossAttack.HammerSweep},
            new[]{BossAttack.HammerSweep,BossAttack.SeismicRing,BossAttack.CrossSlam},
            new[]{BossAttack.VenomTrail,BossAttack.PoisonPools,BossAttack.WebCage},
            new[]{BossAttack.WebCage,BossAttack.WebGrid,BossAttack.PoisonPools},
            new[]{BossAttack.NeedleStorm,BossAttack.VenomTrail,BossAttack.WebGrid},
            new[]{BossAttack.NeedleStorm,BossAttack.WebCage,BossAttack.VenomTrail},
            new[]{BossAttack.SweepingBreath,BossAttack.FireFan,BossAttack.LightningLanes},
            new[]{BossAttack.StormHunt,BossAttack.Spiral,BossAttack.WingRing},
            new[]{BossAttack.LightningLanes,BossAttack.StormHunt,BossAttack.FireFan},
            new[]{BossAttack.Spiral,BossAttack.SweepingBreath,BossAttack.StormHunt},
            new[]{BossAttack.RootMaze,BossAttack.Roots,BossAttack.Harvest},
            new[]{BossAttack.ThornSpiral,BossAttack.RootMaze,BossAttack.Roots},
            new[]{BossAttack.Harvest,BossAttack.Roots,BossAttack.ThornSpiral},
            new[]{BossAttack.ThornSpiral,BossAttack.Harvest,BossAttack.SeedRing}
        };
        static readonly string[] Labels = {
            "突進・地震型","なぎ払い型","衝撃環型","定点粉砕型",
            "毒の追跡型","巣の包囲型","連続針射型","毒針・捕縛型",
            "旋回ブレス型","雷嵐型","落雷の列型","旋回弾幕型",
            "根の迷路型","回転する根型","収穫の輪型","回転・収穫型"
        };
        public static int PartAt(int order,int index) => ((order >> (index*3)) & 7)-1;
        public static int Count(int order) { int count=0; while(count<4 && PartAt(order,count)>=0)count++; return count; }
        public static string Description(BossKind kind,int order) {
            int first=PartAt(order,0), count=Count(order);
            if(first<0 || first>3) return "最初に壊す部位で攻撃が分岐";
            string text=Labels[(int)kind*4+first];
            if(count>1)text+="／追加："+BossCatalog.PartName(kind,PartAt(order,count-1));
            return text;
        }
        public static BossAttack Choose(BossKind kind,int phase,int index,int order)
        {
            int first=PartAt(order,0),count=Count(order);
            if(phase==1 || first<0 || first>3) return BossCatalog.Attack(kind,phase,index);
            int step=Mathf.Abs(index)%4;
            if(step==0 && phase>=3) {
                for(int offset=0;offset<5;offset++) {
                    var opening=BossCatalog.Attack(kind,3,first+offset);
                    if(opening!=Routes[(int)kind*4+first][0])return opening;
                }
            }
            if(step==3 && phase>=3) return BossCatalog.Attack(kind,3,index/4);
            if(step==2 && count>1) return Routes[(int)kind*4+PartAt(order,count-1)][0];
            return Routes[(int)kind*4+first][step%3];
        }
    }
}
