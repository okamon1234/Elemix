using System.Collections.Generic;
namespace RogueSurvivors
{
    // 散開する矢が同じ大型ボスへ全弾集中したときだけ追加分を抑える。雑魚への貫通・散開は維持。
    public sealed class BulletVolley
    {
        readonly HashSet<int> bosses=new HashSet<int>();
        public float Multiplier(EnemyHealth boss)=>bosses.Add(boss.GetInstanceID())?1:.15f;
    }
}
