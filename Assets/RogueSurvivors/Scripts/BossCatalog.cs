using UnityEngine;
namespace RogueSurvivors
{
    public enum BossKind { Golem, Spider, Dragon, Guardian }
    public enum BossAttack { Charge, Shockwave, TripleCharge, CrossSlam, PoisonPools, WebFan, WebGrid, NeedleStorm, FireFan, WingRing, Spiral, LightningLanes, Roots, SeedRing, RootMaze, TwinRings, SeismicRing, HammerSweep, WebCage, VenomTrail, SweepingBreath, StormHunt, ThornSpiral, Harvest }
    public static class BossCatalog
    {
        public static readonly string[] Names = { "アイアンゴーレム", "大グモ", "ストームドラゴン", "森の守護者" };
        public static readonly string[] PhaseNames = { "炉心開放", "狩猟形態", "嵐の翼", "古木の怒り" };
        static readonly string[][] PartNames = { new[] { "右腕", "頭部", "左腕", "脚部" }, new[] { "右脚", "頭部", "左脚", "腹部" }, new[] { "右翼", "頭部", "左翼", "尾部" }, new[] { "右枝", "樹冠", "左枝", "根元" } };
        public static string PartName(BossKind kind,int part) => PartNames[(int)kind][part];
        public static readonly Color[] Colors = { new Color(1,.48f,.16f), new Color(.65f,.35f,1), new Color(.2f,.8f,1), new Color(.35f,1,.4f) };
        static readonly BossAttack[][] Plans = {
            new[] { BossAttack.Charge, BossAttack.Shockwave, BossAttack.Charge },
            new[] { BossAttack.TripleCharge, BossAttack.CrossSlam, BossAttack.Shockwave },
            new[] { BossAttack.PoisonPools, BossAttack.WebFan, BossAttack.PoisonPools },
            new[] { BossAttack.WebGrid, BossAttack.NeedleStorm, BossAttack.PoisonPools },
            new[] { BossAttack.FireFan, BossAttack.WingRing, BossAttack.FireFan },
            new[] { BossAttack.Spiral, BossAttack.LightningLanes, BossAttack.FireFan },
            new[] { BossAttack.Roots, BossAttack.SeedRing, BossAttack.Roots },
            new[] { BossAttack.RootMaze, BossAttack.TwinRings, BossAttack.Roots }
        };
        static int? selection;
        public static int Selection {
            get { if(!selection.HasValue) selection=Mathf.Clamp(PlayerPrefs.GetInt("Rogue.BossSelection",-1),-1,3); return selection.Value; }
            set { selection=Mathf.Clamp(value,-1,3); PlayerPrefs.SetInt("Rogue.BossSelection",selection.Value); PlayerPrefs.Save(); }
        }
        internal static BossKind? VerificationSelection;
        public static BossKind Choose() => VerificationSelection ?? (BossKind)(Selection < 0 ? Random.Range(0,4) : Selection);
        public static BossAttack Attack(BossKind kind, bool phaseTwo, int index) { var plan = Plans[(int)kind * 2 + (phaseTwo ? 1 : 0)]; return plan[Mathf.Abs(index) % plan.Length]; }
        public static BossAttack Attack(BossKind kind,int phase,int index)
        {
            if(phase==1) return Attack(kind,false,index);
            BossAttack[][] plans={
                new[]{BossAttack.HammerSweep,BossAttack.TripleCharge,BossAttack.SeismicRing,BossAttack.CrossSlam},
                new[]{BossAttack.WebCage,BossAttack.VenomTrail,BossAttack.NeedleStorm,BossAttack.PoisonPools},
                new[]{BossAttack.SweepingBreath,BossAttack.StormHunt,BossAttack.FireFan,BossAttack.Spiral},
                new[]{BossAttack.ThornSpiral,BossAttack.RootMaze,BossAttack.Harvest,BossAttack.Roots}
            };
            BossAttack[][] finals={
                new[]{BossAttack.SeismicRing,BossAttack.HammerSweep,BossAttack.TripleCharge,BossAttack.CrossSlam,BossAttack.HammerSweep},
                new[]{BossAttack.VenomTrail,BossAttack.WebCage,BossAttack.NeedleStorm,BossAttack.WebGrid,BossAttack.WebCage},
                new[]{BossAttack.StormHunt,BossAttack.SweepingBreath,BossAttack.Spiral,BossAttack.LightningLanes,BossAttack.SweepingBreath},
                new[]{BossAttack.Harvest,BossAttack.ThornSpiral,BossAttack.Roots,BossAttack.RootMaze,BossAttack.Harvest}
            };
            var plan=(phase>=3?finals:plans)[(int)kind]; return plan[Mathf.Abs(index)%plan.Length];
        }
        public static string AttackName(BossAttack attack)
        {
            switch (attack) {
                case BossAttack.SeismicRing:return "地震の衝撃環・切れ目へ"; case BossAttack.HammerSweep:return "ハンマーの大なぎ払い";
                case BossAttack.WebCage:return "縮む蜘蛛の巣・出口へ"; case BossAttack.VenomTrail:return "連続毒針・その場から離れる";
                case BossAttack.SweepingBreath:return "旋回ブレス・背後へ"; case BossAttack.StormHunt:return "連続落雷・立ち止まらない";
                case BossAttack.ThornSpiral:return "回る根・根と同じ方向へ"; case BossAttack.Harvest:return "収穫の輪・中央へ";
                case BossAttack.Charge: return "突進"; case BossAttack.TripleCharge: return "三連突進";
                case BossAttack.Shockwave: return "衝撃波"; case BossAttack.CrossSlam: return "十字粉砕";
                case BossAttack.PoisonPools: return "毒だまり"; case BossAttack.WebFan: return "糸の扇射";
                case BossAttack.WebGrid: return "蜘蛛の巣"; case BossAttack.NeedleStorm: return "連続針射";
                case BossAttack.FireFan: return "ブレス"; case BossAttack.WingRing: return "翼の弾幕";
                case BossAttack.Spiral: return "旋回弾幕"; case BossAttack.LightningLanes: return "落雷の列";
                case BossAttack.Roots: return "根の追撃"; case BossAttack.SeedRing: return "種の輪";
                case BossAttack.RootMaze: return "根の迷路"; default: return "交差する種の輪";
            }
        }
    }
}
